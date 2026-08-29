// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secao 3.6.1
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Outcomes
{
    /// <summary>
    /// Uma janela de Entrada Perfeita. Como a linha do tempo inteira e conhecida antes
    /// da reproducao comecar (GDD 20.6), a UI pode telegrafar a curva com antecedencia.
    /// Uma corrida tem 6-10 destas.
    /// </summary>
    [System.Serializable]
    public struct InputWindow
    {
        public int SegmentIndex;

        /// <summary>Segundos desde a largada.</summary>
        public float OpensAt;

        /// <summary>OpensAt + BalanceSettings.PerfectEntryWindowSeconds.</summary>
        public float ClosesAt;
    }
}
