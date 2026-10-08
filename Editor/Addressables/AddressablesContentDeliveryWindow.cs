using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Addressables
{
    public class AddressablesContentDeliveryWindow : EditorWindow
    {
        private const string AccessKeyIdPreference = "GoodsSorting.YandexStorage.AccessKeyId";
        private const string SecretKeyPreference = "GoodsSorting.YandexStorage.SecretKey";

        private string _accessKeyId = string.Empty;
        private string _secretKey = string.Empty;
        private string _keyPrefix = string.Empty;
        private string _contentFolder = string.Empty;
        private string _localContent = string.Empty;
        private string _status = "Idle.";
        private bool _isBusy;
        private CancellationTokenSource _cancellation;
        private volatile string _progressFile = string.Empty;
        private volatile float _progressFraction;

        [MenuItem("Tools/Addressables/Content Delivery (Yandex Cloud)")]
        public static void Open()
        {
            AddressablesContentDeliveryWindow window = GetWindow<AddressablesContentDeliveryWindow>();
            window.titleContent = new GUIContent("Content Delivery");
            window.minSize = new Vector2(560f, 460f);
        }

        private void OnEnable()
        {
            _accessKeyId = EditorPrefs.GetString(AccessKeyIdPreference, string.Empty);
            _secretKey = EditorPrefs.GetString(SecretKeyPreference, string.Empty);
            RefreshInfo();
        }

        private void OnDestroy()
        {
            _cancellation?.Cancel();
            EditorApplication.update -= RepaintWhileBusy;
        }

        private void OnFocus()
        {
            RefreshInfo();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Yandex Object Storage — Addressables content delivery", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Bucket", AddressablesContentDelivery.Bucket);
                EditorGUILayout.TextField("Key prefix", _keyPrefix);
                EditorGUILayout.TextField("Content folder", _contentFolder);
                EditorGUILayout.TextField("Local content", _localContent);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Static access key of a service account with the storage.uploader role. Stored in EditorPrefs on this machine only.", MessageType.Info);

            using (new EditorGUI.DisabledScope(_isBusy))
            {
                EditorGUI.BeginChangeCheck();
                string accessKey = EditorGUILayout.TextField("Access key id", _accessKeyId);
                string secretKey = EditorGUILayout.PasswordField("Secret key", _secretKey);

                if (EditorGUI.EndChangeCheck())
                {
                    _accessKeyId = accessKey ?? string.Empty;
                    _secretKey = secretKey ?? string.Empty;
                    EditorPrefs.SetString(AccessKeyIdPreference, _accessKeyId);
                    EditorPrefs.SetString(SecretKeyPreference, _secretKey);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_isBusy))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Test Connection", GUILayout.Height(30f)))
                    RunAsync("Testing connection", TestConnectionAsync);

                if (GUILayout.Button("Build Content", GUILayout.Height(30f)))
                    Run("Building content", AddressablesContentDelivery.BuildContent);

                if (GUILayout.Button("Upload", GUILayout.Height(30f)))
                    RunAsync("Uploading", UploadAsync);

                if (GUILayout.Button("Build + Upload", GUILayout.Height(30f)))
                    RunAsync("Building and uploading", BuildAndUploadAsync);
            }

            if (_isBusy)
            {
                EditorGUILayout.Space();
                Rect rect = EditorGUILayout.GetControlRect(false, 18f);
                EditorGUI.ProgressBar(rect, _progressFraction, _progressFile);

                if (GUILayout.Button("Cancel"))
                    _cancellation?.Cancel();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_status, MessageType.None);
        }

        private void RefreshInfo()
        {
            _keyPrefix = Describe(AddressablesContentDelivery.GetKeyPrefix);
            _contentFolder = Describe(AddressablesContentDelivery.GetContentFolder);
            _localContent = Describe(AddressablesContentDelivery.DescribeLocalContent);
        }

        private static string Describe(Func<string> getter)
        {
            try
            {
                return getter();
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private void Run(string title, Action action)
        {
            _isBusy = true;
            SetStatus(title + "...");

            try
            {
                action();
                SetStatus(title + ": done.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(title + " failed: " + exception.Message);
            }
            finally
            {
                _isBusy = false;
                RefreshInfo();
                Repaint();
            }
        }

        private async void RunAsync(string title, Func<CancellationToken, Task<string>> action)
        {
            _isBusy = true;
            _cancellation = new CancellationTokenSource();
            _progressFile = string.Empty;
            _progressFraction = 0f;
            EditorApplication.update += RepaintWhileBusy;
            SetStatus(title + "...");

            try
            {
                string result = await action(_cancellation.Token);
                SetStatus(result);
            }
            catch (OperationCanceledException)
            {
                SetStatus(title + " cancelled.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(title + " failed: " + exception.Message);
            }
            finally
            {
                EditorApplication.update -= RepaintWhileBusy;
                _cancellation.Dispose();
                _cancellation = null;
                _isBusy = false;
                RefreshInfo();
                Repaint();
            }
        }

        private async Task<string> TestConnectionAsync(CancellationToken cancellationToken)
        {
            HttpStatusCode status = await AddressablesContentDelivery.TestConnectionAsync(_accessKeyId, _secretKey, cancellationToken);

            switch (status)
            {
                case HttpStatusCode.NotFound:
                    return "Connection OK: credentials accepted by bucket " + AddressablesContentDelivery.Bucket + ".";
                case HttpStatusCode.Forbidden:
                    return "Forbidden: check access key, secret key and the storage.uploader role.";
                default:
                    return "Unexpected response: " + (int)status + " " + status;
            }
        }

        private async Task<string> UploadAsync(CancellationToken cancellationToken)
        {
            AddressablesContentDelivery.UploadSummary summary = await AddressablesContentDelivery.UploadAsync(
                _accessKeyId,
                _secretKey,
                ReportProgress,
                cancellationToken);

            string publicUrl = AddressablesContentDelivery.GetPublicUrl(AddressablesContentDelivery.GetKeyPrefix() + "/");

            string message =
                $"Upload done: {summary.Uploaded} uploaded ({summary.UploadedBytes / 1048576f:F1} MB), {summary.Skipped} unchanged. " +
                (summary.PublicAccessVerified
                    ? "Public read verified: " + publicUrl
                    : "WARNING: catalog is not publicly readable at " + publicUrl);

            Debug.Log("[Delivery] " + message);

            return message;
        }

        private async Task<string> BuildAndUploadAsync(CancellationToken cancellationToken)
        {
            AddressablesContentDelivery.BuildContent();
            RefreshInfo();

            return await UploadAsync(cancellationToken);
        }

        private void ReportProgress(string file, float fraction)
        {
            _progressFile = file;
            _progressFraction = fraction;
        }

        private void RepaintWhileBusy()
        {
            Repaint();
        }

        private void SetStatus(string status)
        {
            _status = status;
            Repaint();
        }
    }
}
