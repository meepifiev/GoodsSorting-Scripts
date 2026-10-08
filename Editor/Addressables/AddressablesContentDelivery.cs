using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace _Project.Editor.Addressables
{
    public static class AddressablesContentDelivery
    {
        public readonly struct UploadSummary
        {
            public UploadSummary(int uploaded, int skipped, long uploadedBytes, bool publicAccessVerified)
            {
                Uploaded = uploaded;
                Skipped = skipped;
                UploadedBytes = uploadedBytes;
                PublicAccessVerified = publicAccessVerified;
            }

            public int Uploaded { get; }
            public int Skipped { get; }
            public long UploadedBytes { get; }
            public bool PublicAccessVerified { get; }
        }

        private const string CatalogFilePrefix = "catalog";
        private const string BundleCacheControl = "public, max-age=31536000, immutable";
        private const string CatalogCacheControl = "no-cache";

        public static string Bucket => AddressablesRemoteSetup.Bucket;

        public static string GetContentFolder()
        {
            string buildPath = EvaluateProfileVariable(AddressablesRemoteSetup.RemoteBuildPathVariable);
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), buildPath));
        }

        public static string GetKeyPrefix()
        {
            string loadPath = EvaluateProfileVariable(AddressablesRemoteSetup.RemoteLoadPathVariable);
            string bucketRoot = GetBucketRoot();

            if (loadPath.StartsWith(bucketRoot, StringComparison.OrdinalIgnoreCase) == false)
                throw new InvalidOperationException($"Remote.LoadPath '{loadPath}' does not point to bucket '{Bucket}'. Run Tools/Addressables/Full Setup.");

            return loadPath.Substring(bucketRoot.Length).Trim('/');
        }

        public static string GetPublicUrl(string key)
        {
            return GetBucketRoot() + key;
        }

        public static void BuildContent()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new InvalidOperationException("Active build target must be WebGL to build remote content.");

            string folder = GetContentFolder();

            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            if (string.IsNullOrEmpty(result.Error) == false)
                throw new InvalidOperationException("Addressables content build failed: " + result.Error);

            Debug.Log($"[Delivery] Content built in {result.Duration:F1}s: {folder}");
        }

        public static async Task<HttpStatusCode> TestConnectionAsync(string accessKeyId, string secretKey, CancellationToken cancellationToken)
        {
            using (YandexObjectStorageClient client = new YandexObjectStorageClient(accessKeyId, secretKey))
                return await client.ProbeAsync(Bucket, GetKeyPrefix() + "/.connection-probe", cancellationToken).ConfigureAwait(false);
        }

        public static async Task<UploadSummary> UploadAsync(
            string accessKeyId,
            string secretKey,
            Action<string, float> progress,
            CancellationToken cancellationToken)
        {
            string folder = GetContentFolder();

            if (Directory.Exists(folder) == false)
                throw new DirectoryNotFoundException($"Content folder not found: {folder}. Build content first.");

            List<string> files = CollectFilesInUploadOrder(folder);

            if (files.Count == 0)
                throw new InvalidOperationException("Content folder is empty: " + folder);

            string prefix = GetKeyPrefix();

            int uploaded = 0;
            int skipped = 0;
            long uploadedBytes = 0;
            string hashKey = null;
            byte[] hashContent = null;

            using (YandexObjectStorageClient client = new YandexObjectStorageClient(accessKeyId, secretKey))
            {
                for (int i = 0; i < files.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string file = files[i];
                    string relativePath = GetRelativePath(folder, file);
                    string key = prefix + "/" + relativePath;

                    progress?.Invoke(relativePath, (float)i / files.Count);

                    byte[] content = File.ReadAllBytes(file);

                    if (IsCatalogHash(file))
                    {
                        hashKey = key;
                        hashContent = content;
                    }

                    string localTag = YandexObjectStorageClient.ComputeMd5Hex(content);
                    string remoteTag = await client.GetETagAsync(Bucket, key, cancellationToken).ConfigureAwait(false);

                    if (string.Equals(localTag, remoteTag, StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    await client.PutAsync(Bucket, key, content, GetContentType(file), GetCacheControl(file), cancellationToken).ConfigureAwait(false);

                    uploaded++;
                    uploadedBytes += content.Length;
                }
            }

            bool publicAccessVerified = hashKey != null && await VerifyPublicAccessAsync(hashKey, hashContent, cancellationToken).ConfigureAwait(false);

            return new UploadSummary(uploaded, skipped, uploadedBytes, publicAccessVerified);
        }

        public static string DescribeLocalContent()
        {
            string folder = GetContentFolder();

            if (Directory.Exists(folder) == false)
                return "not built";

            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            long bytes = 0;
            string catalog = null;

            foreach (string file in files)
            {
                bytes += new FileInfo(file).Length;

                if (IsCatalogHash(file))
                    catalog = Path.GetFileNameWithoutExtension(file);
            }

            return $"{files.Length} files, {bytes / 1048576f:F1} MB" + (catalog != null ? $", {catalog}" : ", no catalog");
        }

        private static async Task<bool> VerifyPublicAccessAsync(string key, byte[] expected, CancellationToken cancellationToken)
        {
            using (HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GetPublicUrl(key)))
            {
                request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");

                using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode == false)
                        return false;

                    byte[] actual = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    return actual.Length == expected.Length && string.Equals(
                        YandexObjectStorageClient.ComputeMd5Hex(actual),
                        YandexObjectStorageClient.ComputeMd5Hex(expected),
                        StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private static List<string> CollectFilesInUploadOrder(string folder)
        {
            List<string> bundles = new List<string>();
            List<string> catalogs = new List<string>();
            List<string> hashes = new List<string>();

            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);

                if (name.StartsWith(".", StringComparison.Ordinal))
                    continue;

                if (IsCatalogHash(file))
                    hashes.Add(file);
                else if (IsCatalog(file))
                    catalogs.Add(file);
                else
                    bundles.Add(file);
            }

            bundles.Sort(StringComparer.Ordinal);
            catalogs.Sort(StringComparer.Ordinal);
            hashes.Sort(StringComparer.Ordinal);

            List<string> ordered = new List<string>(bundles.Count + catalogs.Count + hashes.Count);
            ordered.AddRange(bundles);
            ordered.AddRange(catalogs);
            ordered.AddRange(hashes);

            return ordered;
        }

        private static bool IsCatalog(string file)
        {
            return Path.GetFileName(file).StartsWith(CatalogFilePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCatalogHash(string file)
        {
            return IsCatalog(file) && string.Equals(Path.GetExtension(file), ".hash", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetContentType(string file)
        {
            switch (Path.GetExtension(file).ToLowerInvariant())
            {
                case ".json":
                    return "application/json";
                case ".hash":
                    return "text/plain";
                default:
                    return "application/octet-stream";
            }
        }

        private static string GetCacheControl(string file)
        {
            return IsCatalog(file) ? CatalogCacheControl : BundleCacheControl;
        }

        private static string GetRelativePath(string folder, string file)
        {
            return file
                .Substring(folder.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');
        }

        private static string GetBucketRoot()
        {
            return "https://" + YandexObjectStorageClient.Endpoint + "/" + Bucket + "/";
        }

        private static string EvaluateProfileVariable(string variableName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetProfileSettings profiles = settings.profileSettings;
            string rawValue = profiles.GetValueByName(settings.activeProfileId, variableName);

            if (string.IsNullOrEmpty(rawValue))
                throw new InvalidOperationException($"Profile variable '{variableName}' is not set. Run Tools/Addressables/Full Setup.");

            return profiles.EvaluateString(settings.activeProfileId, rawValue);
        }
    }
}
