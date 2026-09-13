using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.UI.MainMenu.SocialProfile;
using UnityEditor;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class AdminLeaderboardTests
    {
        private const string SettingsUxml = "Assets/Project/UI/Shared/SettingsPanel.uxml";
        private const string AdminLeaderboardUxml = "Assets/Project/UI/MainMenu/AdminLeaderboardPanel.uxml";

        [Test]
        public void SettingsPanel_ContainsAdminLeaderboardButtonContract()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingsUxml);
            Assert.That(asset, Is.Not.Null);
            TemplateContainer root = asset.CloneTree();

            Button openBtn = root.Q<Button>("settings-admin-open-leaderboard");
            Assert.That(openBtn, Is.Not.Null, "SettingsPanel.uxml must have settings-admin-open-leaderboard");
        }

        [Test]
        public void AdminLeaderboardPanel_ContainsRequiredContracts()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AdminLeaderboardUxml);
            Assert.That(asset, Is.Not.Null);
            TemplateContainer root = asset.CloneTree();

            foreach (string name in new[]
            {
                "admin-leaderboard-modal",
                "admin-leaderboard-close",
                "admin-leaderboard-refresh",
                "admin-leaderboard-status",
                "admin-leaderboard-tab-overall",
                "admin-leaderboard-tab-level1",
                "admin-leaderboard-tab-level2",
                "admin-leaderboard-tab-level3",
                "admin-leaderboard-throne",
                "admin-leaderboard-throne-stage",
                "admin-leaderboard-throne-silver",
                "admin-leaderboard-throne-gold",
                "admin-leaderboard-throne-diamond",
                "admin-leaderboard-cohort-summary",
                "admin-leaderboard-count",
                "admin-leaderboard-list"
            })
            {
                Assert.That(root.Q(name), Is.Not.Null, $"Missing {name} in AdminLeaderboardPanel.uxml");
            }
        }

        [Test]
        public void LeaderboardRanking_MultiLevelEntries_FollowExactMainSortingRules()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry
                {
                    PlayerId = "l1_player",
                    DisplayName = "Level 1 Explorer",
                    HighestStage = 20,
                    Silver = 10,
                    Gold = 5,
                    Diamond = 2,
                    WeightedScore = LeaderboardRanking.Score(10, 5, 2), // 105
                    SnapshotAtUnixSeconds = 1000,
                    LevelId = "level1",
                    GradeBand = "Grade 4"
                },
                new LeaderboardEntry
                {
                    PlayerId = "l3_champion",
                    DisplayName = "Level 3 Champion",
                    HighestStage = 50,
                    Silver = 5,
                    Gold = 2,
                    Diamond = 1,
                    WeightedScore = LeaderboardRanking.Score(5, 2, 1), // 49
                    SnapshotAtUnixSeconds = 2000,
                    LevelId = "level3",
                    GradeBand = "Grade 6"
                },
                new LeaderboardEntry
                {
                    PlayerId = "l2_player_high_score",
                    DisplayName = "Level 2 Rich Explorer",
                    HighestStage = 20,
                    Silver = 100,
                    Gold = 50,
                    Diamond = 20,
                    WeightedScore = LeaderboardRanking.Score(100, 50, 20), // 1050
                    SnapshotAtUnixSeconds = 1500,
                    LevelId = "level2",
                    GradeBand = "Grade 5"
                },
                new LeaderboardEntry
                {
                    PlayerId = "l1_tied_earlier",
                    DisplayName = "Level 1 Early Achiever",
                    HighestStage = 20,
                    Silver = 100,
                    Gold = 50,
                    Diamond = 20,
                    WeightedScore = LeaderboardRanking.Score(100, 50, 20), // 1050
                    SnapshotAtUnixSeconds = 500, // Earlier than l2_player_high_score
                    LevelId = "level1",
                    GradeBand = "Grade 4"
                }
            };

            List<RankedLeaderboardEntry> ranked = LeaderboardRanking.Rank(entries, "self_none");

            Assert.That(ranked.Count, Is.EqualTo(4));

            // Rank 1: HighestStage = 50 (l3_champion)
            Assert.That(ranked[0].Entry.PlayerId, Is.EqualTo("l3_champion"));
            Assert.That(ranked[0].Rank, Is.EqualTo(1));

            // Rank 2: HighestStage = 20, Score = 1050, earlier timestamp (l1_tied_earlier @ 500s)
            Assert.That(ranked[1].Entry.PlayerId, Is.EqualTo("l1_tied_earlier"));
            Assert.That(ranked[1].Rank, Is.EqualTo(2));

            // Rank 3: HighestStage = 20, Score = 1050, later timestamp (l2_player_high_score @ 1500s)
            Assert.That(ranked[2].Entry.PlayerId, Is.EqualTo("l2_player_high_score"));
            Assert.That(ranked[2].Rank, Is.EqualTo(3));

            // Rank 4: HighestStage = 20, Score = 105 (l1_player)
            Assert.That(ranked[3].Entry.PlayerId, Is.EqualTo("l1_player"));
            Assert.That(ranked[3].Rank, Is.EqualTo(4));
        }

        [Test]
        public void LeaderboardRanking_TiedEntries_DetectedProperly()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry
                {
                    PlayerId = "twin_a",
                    DisplayName = "Alice",
                    HighestStage = 30,
                    WeightedScore = 500,
                    SnapshotAtUnixSeconds = 1000
                },
                new LeaderboardEntry
                {
                    PlayerId = "twin_b",
                    DisplayName = "Bob",
                    HighestStage = 30,
                    WeightedScore = 500,
                    SnapshotAtUnixSeconds = 1000
                }
            };

            List<RankedLeaderboardEntry> ranked = LeaderboardRanking.Rank(entries, "self_none");

            Assert.That(ranked[0].Rank, Is.EqualTo(1));
            Assert.That(ranked[1].Rank, Is.EqualTo(1));
            Assert.That(ranked[0].IsTied, Is.True);
            Assert.That(ranked[1].IsTied, Is.True);
        }
    }
}
