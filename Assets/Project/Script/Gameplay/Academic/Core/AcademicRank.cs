using System;

namespace PowerMath.Gameplay.Academic
{
    public enum AcademicRankTier
    {
        Silver = 0,
        Gold = 1,
        Diamond = 2
    }

    public readonly struct AcademicRank : IEquatable<AcademicRank>
    {
        public AcademicRank(AcademicRankTier tier)
        {
            if (!Enum.IsDefined(typeof(AcademicRankTier), tier))
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            Tier = tier;
        }

        public static AcademicRank Silver => new AcademicRank(AcademicRankTier.Silver);
        public static AcademicRank Gold => new AcademicRank(AcademicRankTier.Gold);
        public static AcademicRank Diamond => new AcademicRank(AcademicRankTier.Diamond);

        public AcademicRankTier Tier { get; }

        public double DamageMultiplier
        {
            get
            {
                switch (Tier)
                {
                    case AcademicRankTier.Silver:
                        return 1d;
                    case AcademicRankTier.Gold:
                        return 1.5d;
                    case AcademicRankTier.Diamond:
                        return 2d;
                    default:
                        throw new InvalidOperationException("Unsupported academic Rank.");
                }
            }
        }

        public AcademicRank PromoteOne()
        {
            return Tier == AcademicRankTier.Diamond
                ? this
                : new AcademicRank((AcademicRankTier)((int)Tier + 1));
        }

        public AcademicRank DemoteOne()
        {
            return Tier == AcademicRankTier.Silver
                ? this
                : new AcademicRank((AcademicRankTier)((int)Tier - 1));
        }

        public static bool TryParseExact(string value, out AcademicRank rank)
        {
            switch (value)
            {
                case "Silver":
                    rank = Silver;
                    return true;
                case "Gold":
                    rank = Gold;
                    return true;
                case "Diamond":
                    rank = Diamond;
                    return true;
                default:
                    rank = default;
                    return false;
            }
        }

        public bool Equals(AcademicRank other)
        {
            return Tier == other.Tier;
        }

        public override bool Equals(object obj)
        {
            return obj is AcademicRank other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Tier;
        }

        public override string ToString()
        {
            return Tier.ToString();
        }

        public static bool operator ==(AcademicRank left, AcademicRank right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AcademicRank left, AcademicRank right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct RankTransition
    {
        public RankTransition(AcademicRank previous, AcademicRank current)
        {
            Previous = previous;
            Current = current;
        }

        public AcademicRank Previous { get; }
        public AcademicRank Current { get; }
        public bool Changed => Previous != Current;
        public bool IsPromotion => Changed && Current.Tier > Previous.Tier;
        public bool IsDemotion => Changed && Current.Tier < Previous.Tier;
    }

    public static class RankProgressionPolicy
    {
        public static RankTransition Evaluate(
            AcademicRank current,
            int completedAuditScore)
        {
            int score = Math.Max(0, Math.Min(AuditWindow.MaximumScore, completedAuditScore));
            AcademicRank next = current;
            if (score >= 40)
            {
                next = current.PromoteOne();
            }
            else if (score <= 25)
            {
                next = current.DemoteOne();
            }

            return new RankTransition(current, next);
        }
    }
}
