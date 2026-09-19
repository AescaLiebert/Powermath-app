using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    public enum AdminLeaderboardFilter
    {
        Overall,
        Level1,
        Level2,
        Level3
    }

    public sealed class AdminLeaderboardPanelController : IDisposable
    {
        private const int BatchSize = 20;
        private bool _batchLoading;

        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Button _refresh;
        private readonly Label _status;
        private readonly Button _tabOverall;
        private readonly Button _tabLevel1;
        private readonly Button _tabLevel2;
        private readonly Button _tabLevel3;

        private readonly Label _throneName;
        private readonly Label _throneStage;
        private readonly Label _throneSilver;
        private readonly Label _throneGold;
        private readonly Label _throneDiamond;
        private readonly VisualElement _thronePetSlot;

        private readonly Label _cohortSummary;
        private readonly Label _countLabel;
        private readonly ScrollView _list;

        private readonly FirestoreLeaderboardRepository _repository;
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly MonoBehaviour _host;
        private readonly LeaderboardCharacterVideoPresenter _characterVideo;

        private Dictionary<string, List<LeaderboardEntry>> _levelData =
            new Dictionary<string, List<LeaderboardEntry>>(StringComparer.OrdinalIgnoreCase);
        private List<RankedLeaderboardEntry> _cachedRanked;
        private AdminLeaderboardFilter _currentFilter = AdminLeaderboardFilter.Overall;
        private int _renderedCount;
        private bool _bound;
        private bool _loading;
        private Action _onClosed;
        private VisualElement _loadMoreIndicator;
        private Label _loadMoreLabel;

        public AdminLeaderboardPanelController(
            VisualElement root,
            MonoBehaviour host,
            GameApiSettings settings,
            PlayerSnapshot player)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _settings = settings;
            _player = player;

            _modal = root.Q<VisualElement>("admin-leaderboard-modal");
            _close = root.Q<Button>("admin-leaderboard-close");
            _refresh = root.Q<Button>("admin-leaderboard-refresh");
            _status = root.Q<Label>("admin-leaderboard-status");

            _tabOverall = root.Q<Button>("admin-leaderboard-tab-overall");
            _tabLevel1 = root.Q<Button>("admin-leaderboard-tab-level1");
            _tabLevel2 = root.Q<Button>("admin-leaderboard-tab-level2");
            _tabLevel3 = root.Q<Button>("admin-leaderboard-tab-level3");

            _throneName = root.Q<Label>("admin-leaderboard-throne");
            _throneStage = root.Q<Label>("admin-leaderboard-throne-stage");
            _throneSilver = root.Q<Label>("admin-leaderboard-throne-silver");
            _throneGold = root.Q<Label>("admin-leaderboard-throne-gold");
            _throneDiamond = root.Q<Label>("admin-leaderboard-throne-diamond");
            _thronePetSlot = _modal?.Q<VisualElement>(".pet-sprite-placeholder");

            _cohortSummary = root.Q<Label>("admin-leaderboard-cohort-summary");
            _countLabel = root.Q<Label>("admin-leaderboard-count");
            _list = root.Q<ScrollView>("admin-leaderboard-list");

            _repository = new FirestoreLeaderboardRepository(host, settings);
            var adminVideoHost = new GameObject("AdminLeaderboardVideoHost");
            if (host != null)
            {
                adminVideoHost.transform.SetParent(host.transform, false);
            }
            _characterVideo = adminVideoHost.AddComponent<LeaderboardCharacterVideoPresenter>();
            if (_modal != null)
            {
                _characterVideo.Bind(_modal);
            }
        }

        public bool IsValid => _modal != null && _close != null && _refresh != null &&
                               _tabOverall != null && _tabLevel1 != null && _tabLevel2 != null &&
                               _tabLevel3 != null && _list != null;

        public bool IsOpen => _modal != null &&
                              _modal.style.display != DisplayStyle.None &&
                              !_modal.ClassListContains("is-hidden");

        public AdminLeaderboardFilter CurrentFilter => _currentFilter;

        public void Bind()
        {
            if (_bound || !IsValid) return;

            _close.clicked += Close;
            _refresh.clicked += Reload;
            _tabOverall.clicked += () => SelectFilter(AdminLeaderboardFilter.Overall);
            _tabLevel1.clicked += () => SelectFilter(AdminLeaderboardFilter.Level1);
            _tabLevel2.clicked += () => SelectFilter(AdminLeaderboardFilter.Level2);
            _tabLevel3.clicked += () => SelectFilter(AdminLeaderboardFilter.Level3);

            if (_list != null && _list.verticalScroller != null)
            {
                _list.verticalScroller.valueChanged += OnScrollChanged;
            }

            _modal.style.display = DisplayStyle.None;
            _modal.AddToClassList("is-hidden");
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;

            _close.clicked -= Close;
            _refresh.clicked -= Reload;
            if (_list != null && _list.verticalScroller != null)
            {
                _list.verticalScroller.valueChanged -= OnScrollChanged;
            }

            _repository.Cancel();
            _characterVideo.Hide();
            if (_characterVideo != null && _characterVideo.gameObject != null)
            {
                UnityEngine.Object.Destroy(_characterVideo.gameObject);
            }
            _bound = false;
        }

        public void Open(Action onClosed = null)
        {
            if (!IsValid) return;

            _onClosed = onClosed;
            _modal.style.display = DisplayStyle.Flex;
            _modal.RemoveFromClassList("is-hidden");
            _modal.Focus();

            SelectFilter(_currentFilter);
            Reload();
        }

        public void Close()
        {
            if (!IsValid) return;

            _repository.Cancel();
            _loading = false;
            _modal.style.display = DisplayStyle.None;
            _modal.AddToClassList("is-hidden");
            _characterVideo.Hide();
            SetSemanticState();

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        public void SelectFilter(AdminLeaderboardFilter filter)
        {
            _currentFilter = filter;
            UpdateTabVisuals();
            ApplyCurrentFilter();
        }

        private void Reload()
        {
            if (_loading) return;

            _loading = true;
            _refresh.SetEnabled(false);
            if (_status != null)
            {
                _status.text = PowerMath.Localization.LocalizationService.Get("menu.refreshing");
            }
            SetSemanticState("is-loading");

            _repository.LoadAll(
                data =>
                {
                    _loading = false;
                    _refresh.SetEnabled(true);
                    _levelData = data ?? new Dictionary<string, List<LeaderboardEntry>>(StringComparer.OrdinalIgnoreCase);
                    ApplyCurrentFilter();
                    if (_status != null)
                    {
                        _status.text = "Updated " + DateTime.Now.ToString("t", CultureInfo.CurrentCulture);
                    }
                    SetSemanticState("is-ready");
                },
                error =>
                {
                    _loading = false;
                    _refresh.SetEnabled(true);
                    if (_status != null)
                    {
                        _status.text = string.IsNullOrWhiteSpace(error)
                            ? PowerMath.Localization.LocalizationService.Get("errors.leaderboardInvalid")
                            : error;
                    }
                    SetSemanticState("is-error");
                });
        }

        private void ApplyCurrentFilter()
        {
            List<LeaderboardEntry> rawEntries = GetEntriesForFilter(_currentFilter);
            string selfId = _player?.profile?.publicPlayerId ?? string.Empty;

            try
            {
                _cachedRanked = LeaderboardRanking.Rank(rawEntries, selfId);
            }
            catch (Exception ex)
            {
                PowerMath.Diagnostics.AppLog.Error("AdminLeaderboard", "Ranking failed: " + ex.Message);
                _cachedRanked = new List<RankedLeaderboardEntry>();
            }

            Render();
        }

        private List<LeaderboardEntry> GetEntriesForFilter(AdminLeaderboardFilter filter)
        {
            var list = new List<LeaderboardEntry>();
            if (_levelData == null || _levelData.Count == 0) return list;

            switch (filter)
            {
                case AdminLeaderboardFilter.Overall:
                    foreach (KeyValuePair<string, List<LeaderboardEntry>> pair in _levelData)
                    {
                        if (pair.Value != null) list.AddRange(pair.Value);
                    }
                    break;
                case AdminLeaderboardFilter.Level1:
                    if (_levelData.TryGetValue("level1", out List<LeaderboardEntry> l1) && l1 != null)
                        list.AddRange(l1);
                    break;
                case AdminLeaderboardFilter.Level2:
                    if (_levelData.TryGetValue("level2", out List<LeaderboardEntry> l2) && l2 != null)
                        list.AddRange(l2);
                    break;
                case AdminLeaderboardFilter.Level3:
                    if (_levelData.TryGetValue("level3", out List<LeaderboardEntry> l3) && l3 != null)
                        list.AddRange(l3);
                    break;
            }

            return list;
        }

        private void UpdateTabVisuals()
        {
            _tabOverall?.EnableInClassList("is-selected", _currentFilter == AdminLeaderboardFilter.Overall);
            _tabLevel1?.EnableInClassList("is-selected", _currentFilter == AdminLeaderboardFilter.Level1);
            _tabLevel2?.EnableInClassList("is-selected", _currentFilter == AdminLeaderboardFilter.Level2);
            _tabLevel3?.EnableInClassList("is-selected", _currentFilter == AdminLeaderboardFilter.Level3);
        }

        private void Render()
        {
            _list.Clear();
            _renderedCount = 0;

            RankedLeaderboardEntry first = _cachedRanked?.FirstOrDefault();
            if (_throneName != null)
                _throneName.text = first == null ? "No standings yet" : first.Entry.DisplayName;
            if (_throneStage != null)
                _throneStage.text = first == null ? "--" : first.Entry.HighestStage.ToString(CultureInfo.CurrentCulture);
            if (_throneSilver != null)
                _throneSilver.text = LeaderboardPanelController.FormatNumber(first?.Entry.Silver ?? 0);
            if (_throneGold != null)
                _throneGold.text = LeaderboardPanelController.FormatNumber(first?.Entry.Gold ?? 0);
            if (_throneDiamond != null)
                _throneDiamond.text = LeaderboardPanelController.FormatNumber(first?.Entry.Diamond ?? 0);

            _characterVideo?.Show(first?.Entry.CharacterId);
            RenderThronePet(first?.Entry.PetId);

            if (_cohortSummary != null)
            {
                switch (_currentFilter)
                {
                    case AdminLeaderboardFilter.Overall:
                        _cohortSummary.text = "OVERALL STANDINGS · ALL LEVELS (1–3)";
                        break;
                    case AdminLeaderboardFilter.Level1:
                        _cohortSummary.text = "LEVEL 1 STANDINGS · GRADE 4";
                        break;
                    case AdminLeaderboardFilter.Level2:
                        _cohortSummary.text = "LEVEL 2 STANDINGS · GRADE 5";
                        break;
                    case AdminLeaderboardFilter.Level3:
                        _cohortSummary.text = "LEVEL 3 STANDINGS · GRADE 6";
                        break;
                }
            }

            if (_countLabel != null)
            {
                int count = _cachedRanked?.Count ?? 0;
                _countLabel.text = count == 1 ? "1 explorer total" : $"{count} explorers total";
            }

            LoadNextBatch();
        }

        private void OnScrollChanged(float value)
        {
            if (_batchLoading || _cachedRanked == null || _renderedCount >= _cachedRanked.Count || _list == null) return;
            float max = _list.verticalScroller != null ? _list.verticalScroller.highValue : 0;
            if (max > 0 && (value >= max - 200f || value >= max * 0.8f))
            {
                LoadNextBatch();
            }
        }

        private void LoadNextBatch()
        {
            if (_batchLoading || _cachedRanked == null || _renderedCount >= _cachedRanked.Count)
            {
                UpdateLoadMoreIndicator();
                return;
            }

            _batchLoading = true;
            try
            {
                if (_loadMoreIndicator != null && _loadMoreIndicator.parent == _list)
                    _loadMoreIndicator.RemoveFromHierarchy();

                bool showCohort = _currentFilter == AdminLeaderboardFilter.Overall;
                int target = Math.Min(_renderedCount + BatchSize, _cachedRanked.Count);
                for (int i = _renderedCount; i < target; i++)
                {
                    VisualElement row = LeaderboardPanelController.CreateAuthoredRow(_cachedRanked[i], showCohort);
                    if (row != null)
                    {
                        _list.Add(row);
                    }
                }
                _renderedCount = target;

                UpdateLoadMoreIndicator();
            }
            finally
            {
                _batchLoading = false;
            }
        }

        private void UpdateLoadMoreIndicator()
        {
            if (_cachedRanked == null || _cachedRanked.Count == 0)
            {
                if (_loadMoreIndicator != null && _loadMoreIndicator.parent == _list)
                    _loadMoreIndicator.RemoveFromHierarchy();
                return;
            }

            if (_loadMoreIndicator == null)
            {
                _loadMoreIndicator = new VisualElement { name = "admin-leaderboard-load-more" };
                _loadMoreIndicator.AddToClassList("leaderboard-load-more");
                _loadMoreIndicator.style.alignItems = Align.Center;
                _loadMoreIndicator.style.justifyContent = Justify.Center;
                _loadMoreIndicator.style.paddingTop = 16;
                _loadMoreIndicator.style.paddingBottom = 24;

                _loadMoreLabel = new Label { name = "admin-leaderboard-load-more-label" };
                _loadMoreLabel.AddToClassList("leaderboard-load-more-label");
                _loadMoreLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 0.9f);
                _loadMoreLabel.style.fontSize = 13;
                _loadMoreIndicator.Add(_loadMoreLabel);
            }

            if (_renderedCount < _cachedRanked.Count)
            {
                _loadMoreLabel.text = string.Format(CultureInfo.InvariantCulture,
                    "Showing {0} of {1} explorers · Scroll to load more", _renderedCount, _cachedRanked.Count);
                if (_loadMoreIndicator.parent != _list)
                    _list.Add(_loadMoreIndicator);
            }
            else
            {
                if (_cachedRanked.Count > BatchSize)
                {
                    _loadMoreLabel.text = string.Format(CultureInfo.InvariantCulture,
                        "Showing all {0} explorers", _cachedRanked.Count);
                    if (_loadMoreIndicator.parent != _list)
                        _list.Add(_loadMoreIndicator);
                }
                else if (_loadMoreIndicator.parent == _list)
                {
                    _loadMoreIndicator.RemoveFromHierarchy();
                }
            }
        }

        private void RenderThronePet(string petId)
        {
            if (_thronePetSlot == null) return;
            _thronePetSlot.Clear();
            Sprite petIcon = LeaderboardPanelController.ResolvePetIcon(petId);
            if (petIcon != null)
            {
                var image = new Image
                {
                    name = "Throne Pet Icon",
                    sprite = petIcon,
                    pickingMode = PickingMode.Ignore
                };
                image.AddToClassList("leaderboard-pet-throne-image");
                _thronePetSlot.Add(image);
                _thronePetSlot.style.display = DisplayStyle.Flex;
            }
            else
            {
                _thronePetSlot.style.display = DisplayStyle.None;
            }
        }

        private void SetSemanticState(string state = null)
        {
            if (_modal == null) return;
            _modal.EnableInClassList("is-loading", state == "is-loading");
            _modal.EnableInClassList("is-ready", state == "is-ready");
            _modal.EnableInClassList("is-error", state == "is-error");
        }
    }
}
