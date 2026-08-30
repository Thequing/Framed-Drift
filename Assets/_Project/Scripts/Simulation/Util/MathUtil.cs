// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Util
//  GDD 0.2  secoes 5, 6
// -----------------------------------------------------------------------------

using System;

namespace FramedDrift.Simulation.Util
{
    /// <summary>
    /// Matematica minima do simulador. Existe porque este assembly tem
    /// noEngineReferences: <c>UnityEngine.Mathf</c> nao esta disponivel aqui (GDD 20.3).
    ///
    /// Tudo em double internamente onde a precisao importa, para que o resultado nao
    /// dependa de otimizacao de ponto flutuante da plataforma - determinismo e requisito,
    /// nao conveniencia (D-01).
    /// </summary>
    public static class MathUtil
    {
        public static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static float Clamp01(float v)
        {
            return Clamp(v, 0f, 1f);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Max(float a, float b) { return a > b ? a : b; }

        public static int Min(int a, int b) { return a < b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }

        public static float Sqrt(float v)
        {
            return v <= 0f ? 0f : (float)Math.Sqrt(v);
        }

        /// <summary>Sigmoide logistica. Usada na qualidade de execucao (GDD 5.5).</summary>
        public static float Sigmoid(float x)
        {
            // Guarda contra overflow de Exp em entradas extremas.
            if (x >= 40f) return 1f;
            if (x <= -40f) return 0f;
            return (float)(1.0 / (1.0 + Math.Exp(-x)));
        }

        public static float Pow(float b, float e)
        {
            return (float)Math.Pow(b, e);
        }
    }
}
