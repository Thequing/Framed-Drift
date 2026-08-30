// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 5.7
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// O estado continuo carregado de segmento a segmento (GDD 5.7).
    ///
    /// Struct e passado por ref: uma corrida sao 18-30 segmentos e o auto-equipar
    /// resolve 200 corridas por avaliacao dentro de 60 ms (GDD 16.3 / 20.4). Alocar
    /// por segmento seria o unico jeito de estourar esse orcamento.
    ///
    /// Combustivel nao esta aqui, e foi cortado de proposito (GDD 5.7): adiciona uma
    /// restricao de tempo sem adicionar decisao, e conflita com o pilar P3.
    /// </summary>
    public struct RaceState
    {
        /// <summary>km/h.</summary>
        public float SpeedKmh;

        /// <summary>0-1. Reduz mu_eff.</summary>
        public float TireWear;

        /// <summary>0-1. Acima de TempOverheatThreshold: Power cai e a falha dobra.</summary>
        public float Temperature;

        /// <summary>Dano acumulado NESTA corrida, 0-100.</summary>
        public float Damage;

        public int Combo;
        public int MaxCombo;

        public float TimeSeconds;
        public float DistanceM;

        /// <summary>Metros de reta consecutiva. Passando do limite, o combo zera.</summary>
        public float StraightRunM;

        public TurnDirection LastCurveDirection;
        public bool LastSegmentWasDriftCurve;

        public int Failures;
        public int Collisions;
        public int DriftSegments;
        public int PerfectSegments;
        public int GoodSegments;
        public int BadSegments;

        public static RaceState Start(float initialSpeedKmh)
        {
            return new RaceState
            {
                SpeedKmh = initialSpeedKmh,
                LastCurveDirection = TurnDirection.None,
            };
        }
    }
}
