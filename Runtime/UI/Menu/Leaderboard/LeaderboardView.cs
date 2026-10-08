using System;
using System.Collections.Generic;
using _Project.Core.Leaderboard;
using _Project.Core.Menu;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Menu.Leaderboard
{
    public class LeaderboardView : MonoBehaviour
    {
        [SerializeField] private Transform _contentHolder;
        [SerializeField] private LeaderboardEntryView _rowPrefab;
        [SerializeField] private GameObject _listRoot;
        [SerializeField] private GameObject _authRoot;
        [SerializeField] private Button _authButton;
        [SerializeField] private int _topCount = 10;
#if UNITY_EDITOR
        [SerializeField] private bool _editorSimulateLoggedIn = true;
#endif

        private readonly List<LeaderboardEntryView> _rows = new List<LeaderboardEntryView>();

        private ILeaderboardService _leaderboard;
        private IPlayerAccountService _account;
        private IMenuNavigator _navigator;

        [Inject]
        public void Construct(ILeaderboardService leaderboard, IPlayerAccountService account, IMenuNavigator navigator)
        {
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void Start()
        {
            _navigator.TabChanged += OnTabChanged;

            if (_authButton != null)
            {
                _authButton.onClick.AddListener(OnAuthClicked);
            }

            if (_navigator.CurrentTab == MenuTab.Leaderboard)
            {
                Refresh();
            }
            else
            {
                ShowList();
            }
        }

        private void OnDestroy()
        {
            if (_navigator != null)
            {
                _navigator.TabChanged -= OnTabChanged;
            }

            if (_authButton != null)
            {
                _authButton.onClick.RemoveListener(OnAuthClicked);
            }
        }

        private void OnTabChanged(MenuTab tab)
        {
            if (tab == MenuTab.Leaderboard)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (IsLoggedIn() == false)
            {
                ShowAuth();
                return;
            }

            ShowList();
            LoadTop();
        }

        private bool IsLoggedIn()
        {
#if UNITY_EDITOR
            return _editorSimulateLoggedIn;
#else
            return _account.IsLoggedIn;
#endif
        }

        private void LoadTop()
        {
#if UNITY_EDITOR
            OnLoaded(FakeEntries());
#else
            _leaderboard.GetTop(_topCount, OnLoaded);
#endif
        }

        private void OnAuthClicked()
        {
            _account.Login(Refresh, null);
        }

        private void OnLoaded(IReadOnlyList<LeaderboardEntry> entries)
        {
            Clear();

            if (_contentHolder == null || _rowPrefab == null)
            {
                return;
            }

            foreach (LeaderboardEntry entry in entries)
            {
                LeaderboardEntryView row = Instantiate(_rowPrefab, _contentHolder);
                row.Bind(entry);
                _rows.Add(row);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_contentHolder);
        }

        private void Clear()
        {
            foreach (LeaderboardEntryView row in _rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _rows.Clear();
        }

        private void ShowAuth()
        {
            if (_authRoot != null)
            {
                _authRoot.SetActive(true);
            }

            if (_listRoot != null)
            {
                _listRoot.SetActive(false);
            }
        }

        private void ShowList()
        {
            if (_authRoot != null)
            {
                _authRoot.SetActive(false);
            }

            if (_listRoot != null)
            {
                _listRoot.SetActive(true);
            }
        }

        private List<LeaderboardEntry> FakeEntries()
        {
            List<LeaderboardEntry> list = new List<LeaderboardEntry>();

            int score = 120;

            for (int i = 0; i < _topCount; i++)
            {
                list.Add(new LeaderboardEntry(i + 1, "Player " + (i + 1), score, string.Empty));
                score = Mathf.Max(1, score - UnityEngine.Random.Range(5, 15));
            }

            return list;
        }
    }
}
