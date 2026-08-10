using System;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct StageId : IEquatable<StageId>
    {
        public const int First = 1;
        public const int Final = 200;

        public StageId(int value)
        {
            if (value < First || value > Final)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Stage must be between {First} and {Final}."
                );
            }

            Value = value;
        }

        public int Value { get; }

        public int WorldLevel => (Value + 4) / 5;

        public bool IsFinal => Value == Final;

        public bool TryNext(out StageId next)
        {
            if (IsFinal)
            {
                next = this;
                return false;
            }

            next = new StageId(Value + 1);
            return true;
        }

        public bool Equals(StageId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StageId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
