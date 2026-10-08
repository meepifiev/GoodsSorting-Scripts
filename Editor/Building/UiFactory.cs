using System;
using _Project.Core.Audio;
using _Project.UI.Common;
using _Project.UI.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Editor.Building
{
    public enum RectAnchor
    {
        Center,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Stretch
    }

    public class UiFactory
    {
        private readonly TMP_FontAsset _font;
        private readonly AudioAsset _clickSound;

        public UiFactory(TMP_FontAsset font, AudioAsset clickSound)
        {
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _clickSound = clickSound;
        }

        public RectTransform CreateRect(string name, Transform parent, RectAnchor anchor, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = position;

            if (anchor != RectAnchor.Stretch)
            {
                rect.sizeDelta = size;
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return rect;
        }

        public Image CreateImage(string name, Transform parent, Sprite sprite, RectAnchor anchor, Vector2 position, Vector2 size, bool sliced = false)
        {
            RectTransform rect = CreateRect(name, parent, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = sliced == false;
            image.raycastTarget = false;

            if (size == Vector2.zero && sprite != null && anchor != RectAnchor.Stretch)
            {
                rect.sizeDelta = sprite.rect.size;
            }

            return image;
        }

        public Image CreateDim(string name, Transform parent, float alpha)
        {
            RectTransform rect = CreateRect(name, parent, RectAnchor.Stretch, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, alpha);
            image.raycastTarget = true;
            return image;
        }

        public TextMeshProUGUI CreateLabel(
            string name,
            Transform parent,
            string text,
            float fontSize,
            RectAnchor anchor,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment,
            string localizationKey = null)
        {
            RectTransform rect = CreateRect(name, parent, anchor, position, size);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            if (string.IsNullOrEmpty(localizationKey) == false)
            {
                LocalizedText localized = rect.gameObject.AddComponent<LocalizedText>();
                SerializedObject serialized = new SerializedObject(localized);
                serialized.FindProperty("_key").stringValue = localizationKey;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return label;
        }

        public Button CreateButton(string name, Transform parent, Sprite sprite, RectAnchor anchor, Vector2 position, Vector2 size, bool sliced = false)
        {
            Image image = CreateImage(name, parent, sprite, anchor, position, size, sliced);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            if (_clickSound != null)
            {
                ClickSoundButton click = image.gameObject.AddComponent<ClickSoundButton>();
                SerializedObject serialized = new SerializedObject(click);
                serialized.FindProperty("_clickSound").objectReferenceValue = _clickSound;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return button;
        }

        public Button CreateInvisibleButton(string name, Transform parent, RectAnchor anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            if (_clickSound != null)
            {
                ClickSoundButton click = rect.gameObject.AddComponent<ClickSoundButton>();
                SerializedObject serialized = new SerializedObject(click);
                serialized.FindProperty("_clickSound").objectReferenceValue = _clickSound;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return button;
        }

        public void ApplyAnchor(RectTransform rect, RectAnchor anchor)
        {
            Vector2 min;
            Vector2 max;

            switch (anchor)
            {
                case RectAnchor.Top:
                    min = max = new Vector2(0.5f, 1f);
                    break;
                case RectAnchor.Bottom:
                    min = max = new Vector2(0.5f, 0f);
                    break;
                case RectAnchor.TopLeft:
                    min = max = new Vector2(0f, 1f);
                    break;
                case RectAnchor.TopRight:
                    min = max = new Vector2(1f, 1f);
                    break;
                case RectAnchor.BottomLeft:
                    min = max = new Vector2(0f, 0f);
                    break;
                case RectAnchor.BottomRight:
                    min = max = new Vector2(1f, 0f);
                    break;
                case RectAnchor.Stretch:
                    min = Vector2.zero;
                    max = Vector2.one;
                    break;
                default:
                    min = max = new Vector2(0.5f, 0.5f);
                    break;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = anchor == RectAnchor.Stretch ? new Vector2(0.5f, 0.5f) : min;
        }
    }
}
