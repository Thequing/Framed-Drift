// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 7.1, 10.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// Endereco de uma stat primaria. Existe para que um afixo (GDD 10.3) possa dizer
    /// "+14 Power" como DADO, sem um switch por stat espalhado pelo codigo.
    ///
    /// A ordem e a mesma da tabela 7.1 - e a ordem em que a UI as exibe.
    /// </summary>
    public enum StatId
    {
        Power,
        Weight,
        Grip,
        Braking,
        Steering,
        Stability,
        Initiation,
        Angle,
        DriftControl,
        Transition,
        Reliability,
        Cooling,
        Aero,
    }

    /// <summary>
    /// Leitura e escrita de <see cref="ResolvedStats"/> por <see cref="StatId"/>.
    ///
    /// Nao usa reflexao de proposito: o resolvedor de stats roda 200 vezes por avaliacao
    /// de auto-equipar (GDD 16.3) dentro de um orcamento de 60 ms.
    /// </summary>
    public static class StatOps
    {
        public const int Count = 13;

        public static float Get(in ResolvedStats s, StatId id)
        {
            switch (id)
            {
                case StatId.Power: return s.Power;
                case StatId.Weight: return s.Weight;
                case StatId.Grip: return s.Grip;
                case StatId.Braking: return s.Braking;
                case StatId.Steering: return s.Steering;
                case StatId.Stability: return s.Stability;
                case StatId.Initiation: return s.Initiation;
                case StatId.Angle: return s.Angle;
                case StatId.DriftControl: return s.DriftControl;
                case StatId.Transition: return s.Transition;
                case StatId.Reliability: return s.Reliability;
                case StatId.Cooling: return s.Cooling;
                default: return s.Aero;
            }
        }

        public static void Add(ref ResolvedStats s, StatId id, float delta)
        {
            switch (id)
            {
                case StatId.Power: s.Power += delta; break;
                case StatId.Weight: s.Weight += delta; break;
                case StatId.Grip: s.Grip += delta; break;
                case StatId.Braking: s.Braking += delta; break;
                case StatId.Steering: s.Steering += delta; break;
                case StatId.Stability: s.Stability += delta; break;
                case StatId.Initiation: s.Initiation += delta; break;
                case StatId.Angle: s.Angle += delta; break;
                case StatId.DriftControl: s.DriftControl += delta; break;
                case StatId.Transition: s.Transition += delta; break;
                case StatId.Reliability: s.Reliability += delta; break;
                case StatId.Cooling: s.Cooling += delta; break;
                default: s.Aero += delta; break;
            }
        }

        public static void Scale(ref ResolvedStats s, StatId id, float factor)
        {
            Add(ref s, id, Get(in s, id) * (factor - 1f));
        }

        public static ResolvedStats Sum(in ResolvedStats a, in ResolvedStats b)
        {
            ResolvedStats r = a;
            for (int i = 0; i < Count; i++)
            {
                StatId id = (StatId)i;
                Add(ref r, id, Get(in b, id));
            }
            return r;
        }

        /// <summary>
        /// Grampeia as stats em faixas plausiveis apos peca + tuning + dano.
        ///
        /// Peso tem piso rigido: uma pilha de afixos de "-kg" que zerasse o peso faria
        /// PowerRatio explodir e quebraria toda a tabela de 6.3.
        /// </summary>
        public static void ClampToValidRanges(ref ResolvedStats s)
        {
            s.Power = MathUtil.Max(1f, s.Power);
            s.Weight = MathUtil.Max(400f, s.Weight);
            s.Grip = MathUtil.Clamp(s.Grip, 0f, 100f);
            s.Braking = MathUtil.Clamp(s.Braking, 0f, 100f);
            s.Steering = MathUtil.Clamp(s.Steering, 0f, 100f);
            s.Stability = MathUtil.Clamp(s.Stability, 0f, 100f);
            s.Initiation = MathUtil.Clamp(s.Initiation, 0f, 100f);
            s.Angle = MathUtil.Clamp(s.Angle, 0f, 100f);
            s.DriftControl = MathUtil.Clamp(s.DriftControl, 0f, 100f);
            s.Transition = MathUtil.Clamp(s.Transition, 0f, 100f);
            s.Reliability = MathUtil.Clamp(s.Reliability, 0f, 100f);
            s.Cooling = MathUtil.Clamp(s.Cooling, 0f, 100f);
            s.Aero = MathUtil.Clamp(s.Aero, -50f, 200f);
        }

        /// <summary>Nome curto para tooltip e log. GDD 7.3.</summary>
        public static string DisplayName(StatId id)
        {
            switch (id)
            {
                case StatId.Power: return "Potencia";
                case StatId.Weight: return "Peso";
                case StatId.Grip: return "Aderencia";
                case StatId.Braking: return "Freio";
                case StatId.Steering: return "Direcao";
                case StatId.Stability: return "Estabilidade";
                case StatId.Initiation: return "Iniciacao";
                case StatId.Angle: return "Angulo";
                case StatId.DriftControl: return "Controle";
                case StatId.Transition: return "Transicao";
                case StatId.Reliability: return "Confiabilidade";
                case StatId.Cooling: return "Arrefecimento";
                default: return "Aero";
            }
        }
    }
}
