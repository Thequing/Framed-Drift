// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secao 5.5
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Outcomes
{
    /// <summary>
    /// O resultado de UM segmento.
    ///
    /// A separacao entre <see cref="Quality"/> e <see cref="QualityFinal"/> e o que
    /// torna a Entrada Perfeita (GDD 3.6.1) segura: o input do jogador edita apenas
    /// QualityFinal. A fisica - <see cref="VExit"/>, <see cref="Angle"/> - ja foi
    /// decidida pelo simulador e e IMUTAVEL. Mexer nela obrigaria a re-simular todos
    /// os segmentos seguintes e derrubaria a linha do tempo pre-computada de D-01.
    /// </summary>
    [System.Serializable]
    public struct SegmentOutcome
    {
        public int SegmentIndex;

        /// <summary>Segundos desde a largada. Define quando a janela de input abre.</summary>
        public float TimeOffset;

        public bool Drift;

        /// <summary>Angulo efetivo sustentado neste segmento.</summary>
        public float Angle;

        /// <summary>Velocidade de saida em km/h. Imutavel.</summary>
        public float VExit;

        /// <summary>Rolado pelo simulador. Identico online e offline para a mesma Seed.</summary>
        public DriftQuality Quality;

        /// <summary>Igual a Quality, ou promovida por input do jogador.</summary>
        public DriftQuality QualityFinal;

        /// <summary>True apenas quando Quality == Good. Bad nunca vira Good. GDD 3.6.1.</summary>
        public bool Promotable;

        /// <summary>Combo acumulado ao fim deste segmento.</summary>
        public int Combo;
    }
}
