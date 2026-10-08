using UnityEditor;
using UnityEngine;

namespace _Project.Editor
{
    public static class ClearPrimeSdkDataMenu
    {
        private const string PrimeSdkDataKey = "PrimeGames.SDK.Data";

        [MenuItem("Tools/Goods Sorting/Development/Clear PrimeSDK Data")]
        public static void Clear()
        {
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Clear PrimeSDK Data",
                "Delete local PrimeSDK progress for this project?",
                "Delete",
                "Cancel");

            if (isConfirmed == false)
            {
                return;
            }

            PlayerPrefs.DeleteKey(PrimeSdkDataKey);
            PlayerPrefs.Save();

            Debug.Log("[Development] PrimeSDK data cleared.");
        }
    }
}
