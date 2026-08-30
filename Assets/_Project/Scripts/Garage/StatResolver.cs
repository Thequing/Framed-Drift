// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 7.2, 7.3, 18.4
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>As quatro barras-resumo da garagem. GDD 7.3 / 18.4.</summary>
    public enum StatGroup { Power, Grip, Drift, Reliability }

    /// <summary>
    /// Apresentacao das stats: as 4 barras-resumo e os textos de tooltip.
    ///
    /// A regra da secao 7.3 e o contrato desta classe: o jogador casual le 4 numeros,
    /// o jogador de build le 12, e toda stat cabe numa frase que um leigo entende. Se
    /// uma stat nova nao couber nesse formato, ela nao entra no jogo.
    ///
    /// O calculo de stats em si vive em <c>LoadoutResolver</c>, no assembly puro - aqui
    /// so mora a leitura. Duplicar o calculo faria o tooltip divergir da corrida.
    /// </summary>
    public static class StatResolver
    {
        /// <summary>
        /// O valor 0-100 de uma barra-resumo. Sao medias ponderadas das 12 primarias,
        /// escolhidas para que mexer numa stat que o grupo promete mexa a barra.
        /// </summary>
        public static float Group(in ResolvedStats s, StatGroup group, BalanceSettings balance)
        {
            switch (group)
            {
                case StatGroup.Power:
                    // Potencia e o que o jogador sente como "anda": hp por tonelada, nao hp.
                    return Clamp01To100(Derived.PowerRatio(in s) / 3.5f);

                case StatGroup.Grip:
                    return Clamp01To100((s.Grip * 0.6f + s.Braking * 0.2f + s.Steering * 0.2f));

                case StatGroup.Drift:
                    return Clamp01To100(s.Angle * 0.4f + s.Initiation * 0.25f
                                        + s.DriftControl * 0.2f + s.Transition * 0.15f);

                default:
                    return Clamp01To100(s.Reliability * 0.6f + s.Stability * 0.25f + s.Cooling * 0.15f);
            }
        }

        public static string GroupName(StatGroup group)
        {
            switch (group)
            {
                case StatGroup.Power: return "Potencia";
                case StatGroup.Grip: return "Aderencia";
                case StatGroup.Drift: return "Drift";
                default: return "Confiabilidade";
            }
        }

        /// <summary>
        /// A frase de tooltip de cada stat. E a tabela 7.1 da GDD, literalmente - nunca
        /// mostrar uma stat sem a frase que a explica (GDD 18.5).
        /// </summary>
        public static string Tooltip(StatId id)
        {
            switch (id)
            {
                case StatId.Power: return "Aceleracao e velocidade maxima.";
                case StatId.Weight: return "Divide a potencia; peso baixo melhora tudo, mas reduz Estabilidade.";
                case StatId.Grip: return "Quanta velocidade o carro sustenta em curva SEM derrapar.";
                case StatId.Braking: return "Quao tarde o carro entra na curva; encurta o tempo de segmento.";
                case StatId.Steering: return "Rapidez de resposta; alimenta qualidade de execucao e transicao.";
                case StatId.Stability: return "Resistencia a rodar; reduz chance de falha.";
                case StatId.Initiation: return "Facilidade de ENTRAR em drift.";
                case StatId.Angle: return "Quanto angulo o carro sustenta - o multiplicador de score.";
                case StatId.DriftControl: return "Chance de Perfect e de proximidade sem bater.";
                case StatId.Transition: return "Velocidade de troca de lado; bonus em S e curvas encadeadas.";
                case StatId.Reliability: return "Reduz falha mecanica, dano recebido e custo de reparo.";
                case StatId.Cooling: return "Segura a temperatura; relevante em Endurance e Endless.";
                default: return "Contribuicao aerodinamica; alimenta a velocidade maxima.";
            }
        }

        /// <summary>As derivadas da secao 7.2 - exibidas, nunca editadas diretamente.</summary>
        public static string DerivedSummary(in ResolvedStats s, BalanceSettings balance)
        {
            return "Potencia/ton " + Derived.PowerRatio(in s).ToString("0")
                   + "   Aceleracao " + Derived.Acceleration(in s, balance).ToString("0")
                   + "   Vel. max " + Derived.TopSpeed(in s, balance).ToString("0") + " km/h";
        }

        /// <summary>Texto de delta para comparacao de peca. Vazio quando nao muda nada.</summary>
        public static string DeltaLine(StatId id, float delta)
        {
            if (delta > -0.05f && delta < 0.05f) return string.Empty;
            string sign = delta > 0f ? "+" : "";
            return sign + delta.ToString("0.#") + " " + StatOps.DisplayName(id);
        }

        private static float Clamp01To100(float v)
        {
            if (v < 0f) return 0f;
            if (v > 100f) return 100f;
            return v;
        }
    }
}
