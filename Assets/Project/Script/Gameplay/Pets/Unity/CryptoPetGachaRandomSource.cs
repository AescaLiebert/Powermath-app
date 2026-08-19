using System;
using System.Security.Cryptography;

namespace PowerMath.Gameplay.Pets
{
    public sealed class CryptoPetGachaRandomSource : IPetGachaRandomSource, IDisposable
    {
        private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
        private readonly byte[] _buffer = new byte[sizeof(uint)];

        public int NextExclusive(int maximumExclusive)
        {
            if (maximumExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            uint bound = (uint)maximumExclusive;
            uint threshold = unchecked(0u - bound) % bound;
            uint sample;
            do
            {
                _generator.GetBytes(_buffer);
                sample = BitConverter.ToUInt32(_buffer, 0);
            } while (sample < threshold);
            return (int)(sample % bound);
        }

        public void Dispose()
        {
            _generator.Dispose();
        }
    }
}
