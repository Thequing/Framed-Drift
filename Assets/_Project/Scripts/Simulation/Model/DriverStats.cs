// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secao 5.1
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// Pos-MVP. O MVP usa um piloto neutro embutido (secao 22.1: "Pilotos - nenhum").
    /// Existe agora so para o simulador ja ter o parametro no lugar certo.
    /// </summary>
    [System.Serializable]
    public struct DriverStats
    {
        /// <summary>Somado a 'skill' no calculo de qualidade. GDD 5.5. Neutro = 0.</summary>
        public float DriverSkill;

        public static DriverStats Neutral
        {
            get { return new DriverStats { DriverSkill = 0f }; }
        }
    }
}
