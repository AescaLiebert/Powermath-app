using System;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Combat;

namespace PowerMath.PlayerData
{
    public static class PlayerAnalyticsUpdater
    {
        public static void Apply(PlayerSnapshot player, GameplaySaveRequest request)
        {
            if (player == null || request.SavePoint != GameplaySavePoint.AttemptResolved ||
                request.Resolution == null)
                return;

            player.analytics = player.analytics ?? new PlayerSnapshot.AnalyticsData();
            if (string.Equals(
                player.analytics.lastAppliedAttemptId,
                request.TransactionId,
                StringComparison.Ordinal))
                return;

            AttemptResolution resolution = request.Resolution;
            if (!resolution.IsAcademic)
            {
                player.analytics.lastAppliedAttemptId = request.TransactionId;
                return;
            }
            bool correct = resolution.Academic.IsCorrect;
            int score = Math.Max(0, Math.Min(10, resolution.Academic.ResponseScore));
            int efficiency = correct ? score * 10 : 0;

            player.analytics.totalQuestionsResolved++;
            if (correct) player.analytics.totalCorrect++;
            else if (resolution.Academic.Outcome == QuestionOutcome.Timeout) player.analytics.totalTimeout++;
            else if (resolution.Academic.Outcome == QuestionOutcome.Abandoned) player.analytics.totalAbandoned++;
            else player.analytics.totalIncorrect++;
            player.analytics.responseScoreSum += score;
            player.analytics.responseEfficiencySum += efficiency;
            player.analytics.responseDurationMillisecondsSum += resolution.ResponseDurationMilliseconds;
            player.analytics.responseScoreHistogram = EnsureHistogram(player.analytics.responseScoreHistogram, 11);
            player.analytics.responseEfficiencyHistogram = EnsureHistogram(player.analytics.responseEfficiencyHistogram, 11);
            player.analytics.responseDuration100msHistogram = EnsureHistogram(
                player.analytics.responseDuration100msHistogram, 102);
            player.analytics.responseScoreHistogram[score]++;
            player.analytics.responseEfficiencyHistogram[efficiency / 10]++;
            player.analytics.responseDuration100msHistogram[Math.Min(
                101, resolution.ResponseDurationMilliseconds / 100)]++;
            player.analytics.lastAppliedAttemptId = request.TransactionId;

            PlayerSnapshot.RankAnalyticsData rank = ResolveRank(player.analytics, resolution.Academic.RankAtCommit);
            rank.resolved++;
            if (correct) rank.correct++;
            rank.responseScoreSum += score;
            rank.responseEfficiencySum += efficiency;

            PlayerSnapshot.QuestionAnalyticsData question = ResolveQuestion(
                player.analytics,
                resolution.Academic.QuestionId.Value);
            question.resolved++;
            if (correct) question.correct++;
            else if (resolution.Academic.Outcome == QuestionOutcome.Timeout) question.timeout++;
            else if (resolution.Academic.Outcome == QuestionOutcome.Abandoned) question.abandoned++;
            else question.incorrect++;
            question.responseScoreSum += score;
            question.responseDurationMillisecondsSum += resolution.ResponseDurationMilliseconds;
            question.responseEfficiencySum += efficiency;

        }

        private static PlayerSnapshot.RankAnalyticsData ResolveRank(
            PlayerSnapshot.AnalyticsData analytics,
            AcademicRank rank)
        {
            if (rank == AcademicRank.Gold)
                return analytics.gold = analytics.gold ?? new PlayerSnapshot.RankAnalyticsData();
            if (rank == AcademicRank.Diamond)
                return analytics.diamond = analytics.diamond ?? new PlayerSnapshot.RankAnalyticsData();
            return analytics.silver = analytics.silver ?? new PlayerSnapshot.RankAnalyticsData();
        }

        private static long[] EnsureHistogram(long[] source, int length)
        {
            if (source != null && source.Length == length) return source;
            var result = new long[length];
            if (source != null) Array.Copy(source, result, Math.Min(source.Length, length));
            return result;
        }

        private static PlayerSnapshot.QuestionAnalyticsData ResolveQuestion(
            PlayerSnapshot.AnalyticsData analytics,
            long questionId)
        {
            var values = new System.Collections.Generic.List<PlayerSnapshot.QuestionAnalyticsData>(
                analytics.byQuestion ?? Array.Empty<PlayerSnapshot.QuestionAnalyticsData>());
            PlayerSnapshot.QuestionAnalyticsData result = values.Find(value =>
                value != null && value.questionId == questionId);
            if (result == null)
            {
                result = new PlayerSnapshot.QuestionAnalyticsData { questionId = questionId };
                values.Add(result);
                values.Sort((left, right) => left.questionId.CompareTo(right.questionId));
                analytics.byQuestion = values.ToArray();
            }
            return result;
        }
    }
}
