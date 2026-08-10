using System;
using System.Text;

namespace PowerMath.Gameplay.Combat
{
    public sealed class AnswerBuffer
    {
        private readonly StringBuilder _digits;

        public AnswerBuffer(int maximumLength)
        {
            if (maximumLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLength));
            }

            MaximumLength = maximumLength;
            _digits = new StringBuilder(maximumLength);
        }

        public int MaximumLength { get; }

        public int Length => _digits.Length;

        public bool IsEmpty => _digits.Length == 0;

        public bool TryAppend(int digit)
        {
            if (digit < 0 || digit > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(digit));
            }

            if (_digits.Length >= MaximumLength)
            {
                return false;
            }

            _digits.Append((char)('0' + digit));
            return true;
        }

        public bool Backspace()
        {
            if (_digits.Length == 0)
            {
                return false;
            }

            _digits.Length--;
            return true;
        }

        public void Clear()
        {
            _digits.Clear();
        }

        public string GetDisplayValue()
        {
            return _digits.ToString();
        }

        public bool TryGetNormalized(out string normalized)
        {
            normalized = string.Empty;
            if (_digits.Length == 0)
            {
                return false;
            }

            int firstNonZero = 0;
            while (firstNonZero < _digits.Length - 1 &&
                   _digits[firstNonZero] == '0')
            {
                firstNonZero++;
            }

            normalized = _digits.ToString(firstNonZero, _digits.Length - firstNonZero);
            return true;
        }
    }
}
