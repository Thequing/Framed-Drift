// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 5.1, 5.5, 12
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>Clima, horario e trafego da corrida. Alimenta k_weather e condMod.</summary>
    [System.Serializable]
    public struct RaceConditions
    {
        public Weather Weather;
        public TimeOfDay TimeOfDay;
        public TrafficDensity Traffic;
    }
}
