using System;

namespace PowerMath.Gameplay.Academic
{
    public readonly struct RankCurrencyBalances
    {
        public RankCurrencyBalances(long silver, long gold, long diamond)
        {
            if (silver < 0 || gold < 0 || diamond < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(silver),
                    "Rank Currency balances cannot be negative."
                );
            }

            Silver = silver;
            Gold = gold;
            Diamond = diamond;
        }

        public long Silver { get; }
        public long Gold { get; }
        public long Diamond { get; }

        public long Get(AcademicRank rank)
        {
            switch (rank.Tier)
            {
                case AcademicRankTier.Silver:
                    return Silver;
                case AcademicRankTier.Gold:
                    return Gold;
                case AcademicRankTier.Diamond:
                    return Diamond;
                default:
                    throw new InvalidOperationException("Unsupported academic Rank.");
            }
        }

        public RankCurrencyBalances Add(AcademicRank rank, long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            checked
            {
                switch (rank.Tier)
                {
                    case AcademicRankTier.Silver:
                        return new RankCurrencyBalances(Silver + amount, Gold, Diamond);
                    case AcademicRankTier.Gold:
                        return new RankCurrencyBalances(Silver, Gold + amount, Diamond);
                    case AcademicRankTier.Diamond:
                        return new RankCurrencyBalances(Silver, Gold, Diamond + amount);
                    default:
                        throw new InvalidOperationException("Unsupported academic Rank.");
                }
            }
        }
    }
}
