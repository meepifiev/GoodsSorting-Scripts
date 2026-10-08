using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Gameplay
{
    public class FrostTimerFlipbook : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Texture2D _sheet;
        [SerializeField] private int _columns = 6;
        [SerializeField] private int _rows = 5;
        [SerializeField] private int _frameCount = 29;
        [SerializeField] private float _pixelsPerUnit = 100f;
        [SerializeField] private float _framesPerSecond = 30f;

        private Sprite[] _frames;
        private Coroutine _routine;

        private void Awake()
        {
            BuildFrames();

            if (_image != null)
            {
                _image.enabled = false;
            }
        }

        public void PlayFreeze()
        {
            Play(true);
        }

        public void PlayThaw()
        {
            Play(false);
        }

        private void Play(bool freeze)
        {
            if (_frames == null || _frames.Length == 0 || _image == null)
            {
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(Animate(freeze));
        }

        private IEnumerator Animate(bool freeze)
        {
            _image.enabled = true;
            int last = _frames.Length - 1;
            float step = _framesPerSecond > 0f ? 1f / _framesPerSecond : 0f;

            if (freeze)
            {
                for (int i = 0; i <= last; i++)
                {
                    _image.sprite = _frames[i];
                    yield return new WaitForSecondsRealtime(step);
                }

                _image.sprite = _frames[last];
            }
            else
            {
                for (int i = last; i >= 0; i--)
                {
                    _image.sprite = _frames[i];
                    yield return new WaitForSecondsRealtime(step);
                }

                _image.enabled = false;
            }

            _routine = null;
        }

        private void BuildFrames()
        {
            if (_sheet == null || _columns <= 0 || _rows <= 0 || _frameCount <= 0)
            {
                return;
            }

            float frameWidth = (float)_sheet.width / _columns;
            float frameHeight = (float)_sheet.height / _rows;
            _frames = new Sprite[_frameCount];

            for (int index = 0; index < _frameCount; index++)
            {
                int column = index % _columns;
                int row = index / _columns;
                Rect rect = new Rect(
                    column * frameWidth,
                    _sheet.height - (row + 1) * frameHeight,
                    frameWidth,
                    frameHeight);
                _frames[index] = Sprite.Create(_sheet, rect, new Vector2(0.5f, 0.5f), _pixelsPerUnit);
            }
        }
    }
}
