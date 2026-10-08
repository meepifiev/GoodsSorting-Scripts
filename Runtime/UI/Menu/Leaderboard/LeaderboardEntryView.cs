using System.Collections;
using _Project.Core.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace _Project.UI.Menu.Leaderboard
{
    public class LeaderboardEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _positionText;
        [SerializeField] private RawImage _avatarImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;

        public void Bind(LeaderboardEntry entry)
        {
            if (_positionText != null)
            {
                _positionText.text = entry.Position.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = entry.DisplayName;
            }

            if (_scoreText != null)
            {
                _scoreText.text = entry.Score.ToString();
            }

            LoadAvatar(entry.AvatarUrl);
        }

        private void LoadAvatar(string url)
        {
            if (_avatarImage == null)
            {
                return;
            }

            _avatarImage.texture = null;
            _avatarImage.enabled = false;

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            StartCoroutine(LoadTexture(url));
        }

        private IEnumerator LoadTexture(string url)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    yield break;
                }

                Texture texture = DownloadHandlerTexture.GetContent(request);

                if (_avatarImage != null)
                {
                    _avatarImage.texture = texture;
                    _avatarImage.enabled = texture != null;
                }
            }
        }
    }
}
