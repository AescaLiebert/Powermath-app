using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct CombatCommandId : IEquatable<CombatCommandId>
    {
        private readonly string _value;

        public CombatCommandId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A combat command ID is required.",
                    nameof(value)
                );
            }

            _value = value;
        }

        public static CombatCommandId New()
        {
            return new CombatCommandId(Guid.NewGuid().ToString("N"));
        }

        public bool Equals(CombatCommandId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatCommandId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(_value ?? string.Empty);
        }

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }

    internal sealed class CommandReceiptCache
    {
        private readonly Dictionary<CombatCommandId, Receipt> _receipts =
            new Dictionary<CombatCommandId, Receipt>();

        public T GetOrAdd<T>(
            CombatCommandId commandId,
            string operation,
            Func<T> createReceipt)
        {
            if (string.IsNullOrEmpty(commandId.ToString()))
            {
                throw new ArgumentException(
                    "A non-default combat command ID is required.",
                    nameof(commandId)
                );
            }

            if (_receipts.TryGetValue(commandId, out Receipt existing))
            {
                if (!string.Equals(
                        existing.Operation,
                        operation,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A combat command ID cannot be reused for another operation."
                    );
                }

                return (T)existing.Value;
            }

            T value = createReceipt();
            _receipts.Add(commandId, new Receipt(operation, value));
            return value;
        }

        private sealed class Receipt
        {
            public Receipt(string operation, object value)
            {
                Operation = operation;
                Value = value;
            }

            public string Operation { get; }
            public object Value { get; }
        }
    }
}
