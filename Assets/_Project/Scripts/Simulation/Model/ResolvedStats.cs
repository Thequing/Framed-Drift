// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secao 7
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// As 12 stats primarias ja resolvidas (base do carro + pecas + tuning + trait).
    /// A regra da secao 7.3 vale: se uma stat nova nao couber numa frase de tooltip,
    /// ela nao entra aqui.
    /// </summary>
    [System.Serializable]
    public struct ResolvedStats
    {
        /// <summary>hp. Aceleracao e velocidade maxima.</summary>
        public float Power;

        /// <summary>kg. Divide a potencia; peso baixo melhora tudo, mas reduz Stability.</summary>
        public float Weight;

        /// <summary>0-100. Quanta velocidade o carro sustenta em curva sem derrapar.</summary>
        public float Grip;

        /// <summary>0-100. Quao tarde o carro entra na curva.</summary>
        public float Braking;

        /// <summary>0-100. Rapidez de resposta; alimenta qualidade e transicao.</summary>
        public float Steering;

        /// <summary>0-100. Resistencia a rodar; reduz chance de falha.</summary>
        public float Stability;

        /// <summary>0-100. Facilidade de entrar em drift.</summary>
        public float Initiation;

        /// <summary>0-100. Quanto angulo o carro sustenta - o multiplicador de score.</summary>
        public float Angle;

        /// <summary>0-100. Chance de Perfect e de proximidade sem bater.</summary>
        public float DriftControl;

        /// <summary>0-100. Velocidade de troca de lado; bonus em S e curvas encadeadas.</summary>
        public float Transition;

        /// <summary>0-100. Reduz falha mecanica, dano recebido e custo de reparo.</summary>
        public float Reliability;

        /// <summary>0-100. Segura a temperatura; relevante em Endurance e Endless.</summary>
        public float Cooling;

        /// <summary>Contribuicao aerodinamica agregada do slot Aero/Peso. GDD 7.2 (TopSpeed).</summary>
        public float Aero;

        // --- derivadas (GDD 7.2) - exibidas, nunca editadas ----------------------
        // TODO: implementar junto com o SegmentSolver, usando BalanceSettings.
        //   PowerRatio   = Power / (Weight / 1000)
        //   Acceleration = clamp(40 + PowerRatio * 0.09, 0, 100)
        //   TopSpeed     = 130 + Power * 0.20 + Aero * 0.15 - Weight * 0.010
    }
}
