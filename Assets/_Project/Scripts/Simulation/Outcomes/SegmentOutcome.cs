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
    ///
    /// <b>Combo tambem e imutavel</b>, e isso e uma decisao, nao um esquecimento. Combo
    /// e estado continuo (GDD 5.7): se uma promocao pudesse alterar o combo - e com a
    /// trait Heranca de corrida um Perfect vale +2 em vez de +1 - todo segmento seguinte
    /// mudaria de valor e a promocao deixaria de ser local. A promocao mexe no
    /// qualityMult e no angleMult DAQUELA curva, e em nada mais.
    /// </summary>
    [System.Serializable]
    public struct SegmentOutcome
    {
        public int SegmentIndex;

        /// <summary>Segundos desde a largada. Define quando a janela de input abre.</summary>
        public float TimeOffset;

        /// <summary>Duracao deste segmento em segundos.</summary>
        public float Duration;

        public bool Drift;

        /// <summary>
        /// Angulo BRUTO sustentado neste segmento (0-100), antes da modulacao por
        /// qualidade. Foi ele que decidiu v_drift em 5.4; a modulacao de 6.1 e do scorer.
        /// </summary>
        public float Angle;

        /// <summary>Velocidade media do segmento em km/h. Alimenta speedMult. GDD 6.1.</summary>
        public float VDrift;

        /// <summary>Velocidade de saida em km/h. Imutavel.</summary>
        public float VExit;

        /// <summary>Comprimento do segmento em metros. Alimenta base = Length * 10.</summary>
        public float LengthM;

        /// <summary>Rolado pelo simulador. Identico online e offline para a mesma Seed.</summary>
        public DriftQuality Quality;

        /// <summary>Igual a Quality, ou promovida por input do jogador.</summary>
        public DriftQuality QualityFinal;

        /// <summary>True apenas quando Quality == Good. Bad nunca vira Good. GDD 3.6.1.</summary>
        public bool Promotable;

        /// <summary>Combo acumulado ao fim deste segmento. Imutavel - ver o resumo.</summary>
        public int Combo;

        /// <summary>0-1. Vale 0 em segmentos sem parede. GDD 6.1.</summary>
        public float Proximity;

        /// <summary>Transicao valida: S ou curva encadeada de sentido oposto. GDD 5.4 / 6.1.</summary>
        public bool IsTransition;

        /// <summary>Houve colisao por proximidade neste segmento. GDD 12.3.</summary>
        public bool Collision;

        /// <summary>Houve falha mecanica neste segmento. GDD 12.3.</summary>
        public bool Failure;

        /// <summary>Restaura QualityFinal ao valor rolado. Usado pelo replay e pelos testes.</summary>
        public void ResetPromotion()
        {
            QualityFinal = Quality;
        }
    }
}
