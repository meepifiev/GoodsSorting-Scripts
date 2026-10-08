using System;
using System.Collections.Generic;
using _Project.Core.Building;
using _Project.Core.Localization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Building
{
    public class BuildingNewDayBanner : MonoBehaviour
    {
        private const string DayKey = "building.newday.title";
        private const string CompletedKey = "building.newday.completed";
        private const string RewardKey = "building.newday.reward";
        private const string GoldKey = "building.reward.gold";
        private const string HammerKey = "building.reward.hammer";
        private const string SwapKey = "building.reward.swap";
        private const string ReplaceKey = "building.reward.replace";
        private const string FreezeKey = "building.reward.freeze";
        private const float FadeDuration = 0.35f;
        private const float HoldDuration = 2.5f;
        private const float FromScale = 0.7f;

        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _content;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _rewardLabel;

        private ILocalizationService _localization;
        private Sequence _sequence;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Initialize(GameObject root, CanvasGroup canvasGroup, RectTransform content, TextMeshProUGUI titleLabel, TextMeshProUGUI rewardLabel)
        {
            _root = root;
            _canvasGroup = canvasGroup;
            _content = content;
            _titleLabel = titleLabel;
            _rewardLabel = rewardLabel;
        }

        public void Show(BuildingDayResult result)
        {
            _titleLabel.text = result.AreaCompleted
                ? _localization.Get(CompletedKey)
                : string.Format(_localization.Get(DayKey), result.NextDay + 1);
            _rewardLabel.text = string.Format(_localization.Get(RewardKey), DescribeReward(result.Reward));

            _root.SetActive(true);
            _sequence?.Kill();
            _canvasGroup.alpha = 0f;
            _content.localScale = Vector3.one * FromScale;

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(Fade(1f))
                .Join(_content.DOScale(1f, FadeDuration).SetEase(Ease.OutBack))
                .AppendInterval(HoldDuration)
                .Append(Fade(0f))
                .OnComplete(() => _root.SetActive(false));
        }

        private Tween Fade(float target)
        {
            return DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, target, FadeDuration);
        }

        private string DescribeReward(BuildingDayReward reward)
        {
            List<string> parts = new List<string>();
            AppendPart(parts, GoldKey, reward.Gold);
            AppendPart(parts, HammerKey, reward.Hammer);
            AppendPart(parts, SwapKey, reward.Swap);
            AppendPart(parts, ReplaceKey, reward.Replace);
            AppendPart(parts, FreezeKey, reward.Freeze);
            return string.Join(", ", parts);
        }

        private void AppendPart(List<string> parts, string key, int amount)
        {
            if (amount > 0)
            {
                parts.Add(string.Format(_localization.Get(key), amount));
            }
        }
    }
}
