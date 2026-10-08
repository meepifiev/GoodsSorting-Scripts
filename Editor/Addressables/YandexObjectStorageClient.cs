using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _Project.Editor.Addressables
{
    public class YandexObjectStorageClient : IDisposable
    {
        public const string Endpoint = "storage.yandexcloud.net";

        private const string Region = "ru-central1";
        private const string Service = "s3";
        private const string Algorithm = "AWS4-HMAC-SHA256";
        private const string SignedHeaders = "host;x-amz-content-sha256;x-amz-date";
        private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        private readonly string _accessKeyId;
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;

        public YandexObjectStorageClient(string accessKeyId, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(accessKeyId))
                throw new ArgumentException("Access key id is empty.", nameof(accessKeyId));

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException("Secret key is empty.", nameof(secretKey));

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            _accessKeyId = accessKeyId.Trim();
            _secretKey = secretKey.Trim();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        public async Task<HttpStatusCode> ProbeAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = CreateRequest(HttpMethod.Head, bucket, key, EmptyPayloadHash))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                return response.StatusCode;
        }

        public async Task<string> GetETagAsync(string bucket, string key, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = CreateRequest(HttpMethod.Head, bucket, key, EmptyPayloadHash))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                await EnsureSuccessAsync(response, key).ConfigureAwait(false);

                return response.Headers.ETag != null ? response.Headers.ETag.Tag.Trim('"') : null;
            }
        }

        public async Task PutAsync(
            string bucket,
            string key,
            byte[] content,
            string contentType,
            string cacheControl,
            CancellationToken cancellationToken)
        {
            string payloadHash = ToHex(ComputeSha256(content));

            using (HttpRequestMessage request = CreateRequest(HttpMethod.Put, bucket, key, payloadHash))
            {
                request.Content = new ByteArrayContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                request.Headers.TryAddWithoutValidation("Cache-Control", cacheControl);

                using (HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    await EnsureSuccessAsync(response, key).ConfigureAwait(false);
            }
        }

        public static string ComputeMd5Hex(byte[] content)
        {
            using (MD5 md5 = MD5.Create())
                return ToHex(md5.ComputeHash(content));
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string bucket, string key, string payloadHash)
        {
            DateTime utcNow = DateTime.UtcNow;
            string amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            string dateStamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string canonicalUri = "/" + UriEncode(bucket) + "/" + UriEncode(key);

            string canonicalHeaders =
                "host:" + Endpoint + "\n" +
                "x-amz-content-sha256:" + payloadHash + "\n" +
                "x-amz-date:" + amzDate + "\n";

            string canonicalRequest =
                method.Method + "\n" +
                canonicalUri + "\n" +
                string.Empty + "\n" +
                canonicalHeaders + "\n" +
                SignedHeaders + "\n" +
                payloadHash;

            string scope = dateStamp + "/" + Region + "/" + Service + "/aws4_request";

            string stringToSign =
                Algorithm + "\n" +
                amzDate + "\n" +
                scope + "\n" +
                ToHex(ComputeSha256(Encoding.UTF8.GetBytes(canonicalRequest)));

            byte[] dateKey = ComputeHmac(Encoding.UTF8.GetBytes("AWS4" + _secretKey), dateStamp);
            byte[] regionKey = ComputeHmac(dateKey, Region);
            byte[] serviceKey = ComputeHmac(regionKey, Service);
            byte[] signingKey = ComputeHmac(serviceKey, "aws4_request");
            string signature = ToHex(ComputeHmac(signingKey, stringToSign));

            HttpRequestMessage request = new HttpRequestMessage(method, "https://" + Endpoint + canonicalUri);
            request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
            request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                Algorithm + " Credential=" + _accessKeyId + "/" + scope + ", SignedHeaders=" + SignedHeaders + ", Signature=" + signature);

            return request;
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string key)
        {
            if (response.IsSuccessStatusCode)
                return;

            string body = response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;

            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase} for '{key}': {body}");
        }

        private static string UriEncode(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length * 2);

            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                char c = (char)b;

                bool unreserved =
                    (c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '-' || c == '_' || c == '.' || c == '~' || c == '/';

                if (unreserved)
                    builder.Append(c);
                else
                    builder.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static byte[] ComputeSha256(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
                return sha256.ComputeHash(data);
        }

        private static byte[] ComputeHmac(byte[] key, string data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));

            return builder.ToString();
        }
    }
}
