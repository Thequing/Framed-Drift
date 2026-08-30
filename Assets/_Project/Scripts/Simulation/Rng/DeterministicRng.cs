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

        // --- distribuicoes usadas pela agregacao offline (GDD 17.2) -----------------

        private bool _hasSpareGaussian;
        private double _spareGaussian;

        /// <summary>
        /// Normal(0,1) por Box-Muller polar. O par gerado e guardado, entao o consumo do
        /// fluxo depende de quantas amostras foram pedidas - nunca chame isto de dentro
        /// do fluxo de Execution no meio de um segmento sem contar as chamadas, ou o
        /// replay deixa de bater.
        /// </summary>
        public double NextGaussian()
        {
            if (_hasSpareGaussian)
            {
                _hasSpareGaussian = false;
                return _spareGaussian;
            }

            double u, v, s;
            do
            {
                u = NextDouble() * 2.0 - 1.0;
                v = NextDouble() * 2.0 - 1.0;
                s = u * u + v * v;
            }
            while (s >= 1.0 || s == 0.0);

            double f = System.Math.Sqrt(-2.0 * System.Math.Log(s) / s);
            _spareGaussian = v * f;
            _hasSpareGaussian = true;
            return u * f;
        }

        public double NextGaussian(double mean, double stdDev)
        {
            return mean + stdDev * NextGaussian();
        }

        /// <summary>
        /// Poisson(lambda). Knuth para lambda pequeno, aproximacao normal acima de 30 -
        /// o metodo de Knuth faz lambda multiplicacoes e a agregacao offline chega a
        /// lambda na casa dos milhares (GDD 17.2).
        /// </summary>
        public int Poisson(double lambda)
        {
            if (lambda <= 0.0) return 0;

            if (lambda < 30.0)
            {
                double l = System.Math.Exp(-lambda);
                int k = 0;
                double p = 1.0;
                do
                {
                    k++;
                    p *= NextDouble();
                }
                while (p > l);
                return k - 1;
            }

            double sample = NextGaussian(lambda, System.Math.Sqrt(lambda));
            return sample < 0.0 ? 0 : (int)System.Math.Round(sample);
        }

        /// <summary>Binomial(n, p). Aproximada por Poisson ou Normal quando n e grande.</summary>
        public int Binomial(int n, double p)
        {
            if (n <= 0 || p <= 0.0) return 0;
            if (p >= 1.0) return n;

            if (n <= 64)
            {
                int hits = 0;
                for (int i = 0; i < n; i++) if (NextDouble() < p) hits++;
                return hits;
            }

            double mean = n * p;
            if (mean < 30.0)
            {
                int k = Poisson(mean);
                return k > n ? n : k;
            }

            double sd = System.Math.Sqrt(mean * (1.0 - p));
            double sample = NextGaussian(mean, sd);
            if (sample < 0.0) return 0;
            if (sample > n) return n;
            return (int)System.Math.Round(sample);
        }

        /// <summary>
        /// Indice sorteado com pesos. Devolve -1 quando a tabela esta vazia ou tem soma
        /// nao positiva - o chamador decide se isso e um erro de conteudo.
        /// </summary>
        public int WeightedIndex(float[] weights)
        {
            if (weights == null || weights.Length == 0) return -1;

            double total = 0.0;
            for (int i = 0; i < weights.Length; i++)
                if (weights[i] > 0f) total += weights[i];
            if (total <= 0.0) return -1;

            double roll = NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                roll -= weights[i];
                if (roll <= 0.0) return i;
            }
            return weights.Length - 1;
        }
    }
}
