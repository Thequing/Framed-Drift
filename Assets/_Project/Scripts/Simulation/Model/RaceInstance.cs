// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secao 5.1
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// Tudo que o simulador precisa para resolver uma corrida.
    /// RaceInstance + Seed reproduzem a corrida byte a byte (GDD 20.3).
    /// </summary>
    [System.Serializable]
    public class RaceInstance
    {
        public Segment[] Track;
        public ResolvedStats Car;
        public DriverStats Driver;
        public RaceConditions Conditions;

        /// <summary>Tier da pista (D..S), como int para o calculo de t_ref. GDD 5.6.</summary>
        public int Difficulty;

        public DriftStyle Style;

        /// <summary>Semente mestra. Os fluxos por dominio derivam dela. GDD 20.3.</summary>
        public ulong Seed;
    }
}
