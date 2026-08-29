// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Rng
//  GDD 0.2  secao 20.3
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Rng
{
    /// <summary>
    /// xorshift128+. Implementado (e nao deixado em branco) porque determinismo nao e
    /// uma decisao de design em aberto: RaceInstance + Seed precisam reproduzir a
    /// corrida byte a byte, e um stub quebraria todo teste construido em cima disso.
    ///
    /// <c>UnityEngine.Random</c> e proibido neste assembly - o asmdef tem
    /// noEngineReferences, entao usar seria erro de compilacao, nao so ma pratica.
    /// </summary>
    public sealed class DeterministicRng
    {
        private ulong _s0;
        private ulong _s1;

        public DeterministicRng(ulong seed)
        {
            // SplitMix64 para espalhar a seed antes de semear o xorshift.
            _s0 = SplitMix(ref seed);
            _s1 = SplitMix(ref seed);
            if (_s0 == 0UL && _s1 == 0UL) _s1 = 0x9E3779B97F4A7C15UL;
        }

        private static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Proximo inteiro sem sinal de 64 bits.</summary>
        public ulong NextULong()
        {
            ulong s1 = _s0;
            ulong s0 = _s1;
            ulong result = s0 + s1;
            _s0 = s0;
            s1 ^= s1 << 23;
            _s1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
            return result;
        }

        /// <summary>[0,1). 53 bits de mantissa.</summary>
        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
        }

        /// <summary>[0,1) em float.</summary>
        public float NextFloat()
        {
            return (float)NextDouble();
        }

        /// <summary>[min,max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>[minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextULong() % (ulong)(maxExclusive - minInclusive));
        }

        /// <summary>True com probabilidade p.</summary>
        public bool Chance(float p)
        {
            return NextFloat() < p;
        }
    }
}
