// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 5.1, 7, 8.3, 9, 10.4
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// O carro como o simulador o enxerga: stats ja resolvidas, pneu, trait e a lista de
    /// passivas ativas.
    ///
    /// Tudo que vem da garagem (peca equipada, ajuste fino, dano, set completo) ja foi
    /// achatado aqui pelo <c>LoadoutResolver</c>. O simulador nao conhece inventario -
    /// e o que permite rodar 200 corridas de avaliacao (GDD 16.3) sem tocar no estado
    /// do jogador.
    /// </summary>
    public sealed class CarLoadout
    {
        public string CarId;
        public ResolvedStats Stats;
        public TireProfile Tire;
        public DifferentialType Differential;
        public TraitId Trait;

        /// <summary>Passivas de peca (GDD 10.4) e bonus de set (GDD 10.5) ja fundidos.</summary>
        public PassiveEffect[] Passives;

        /// <summary>
        /// Chance de Bad somada pelo eixo de desaceleracao do diferencial (GDD 9.2).
        /// Nao e uma stat: o jogador nunca le "risco de Bad" como numero, ele le o
        /// slider. Por isso mora aqui e nao em ResolvedStats.
        /// </summary>
        public float BadRiskBonus;

        public static readonly PassiveEffect[] NoPassives = new PassiveEffect[0];

        public CarLoadout()
        {
            Passives = NoPassives;
        }

        /// <summary>Soma as magnitudes de todas as passivas de um tipo. 0 quando nenhuma.</summary>
        public float Passive(PassiveKind kind)
        {
            float total = 0f;
            for (int i = 0; i < Passives.Length; i++)
                if (Passives[i].Kind == kind) total += Passives[i].Magnitude;
            return total;
        }

        /// <summary>Maior segundo parametro entre as passivas de um tipo. 0 quando nenhuma.</summary>
        public float PassiveThreshold(PassiveKind kind)
        {
            float best = 0f;
            for (int i = 0; i < Passives.Length; i++)
                if (Passives[i].Kind == kind && Passives[i].Magnitude2 > best)
                    best = Passives[i].Magnitude2;
            return best;
        }

        public bool HasPassive(PassiveKind kind)
        {
            for (int i = 0; i < Passives.Length; i++)
                if (Passives[i].Kind == kind) return true;
            return false;
        }
    }

    /// <summary>
    /// As stats derivadas da secao 7.2 - exibidas, nunca editadas diretamente.
    ///
    /// Sao funcao pura das stats primarias mais as constantes; nao existem como campo
    /// para que nao possam sair de sincronia com a build.
    /// </summary>
    public static class Derived
    {
        /// <summary>hp por tonelada.</summary>
        public static float PowerRatio(in ResolvedStats s)
        {
            float tonnes = s.Weight / 1000f;
            return tonnes <= 0f ? 0f : s.Power / tonnes;
        }

        public static float Acceleration(in ResolvedStats s, BalanceSettings b)
        {
            return MathUtil.Clamp(b.AccelBase + PowerRatio(in s) * b.AccelPerPowerRatio, 0f, 100f);
        }

        public static float TopSpeed(in ResolvedStats s, BalanceSettings b)
        {
            float v = b.TopSpeedBase
                      + s.Power * b.TopSpeedPerPower
                      + s.Aero * b.TopSpeedPerAero
                      - s.Weight * b.TopSpeedPerKg;
            return MathUtil.Max(40f, v);
        }

        /// <summary>
        /// Aplica o efeito do dano acumulado: reduz TODAS as stats proporcionalmente
        /// (GDD 5.7). Weight nao entra - carro amassado nao fica mais leve, e reduzir o
        /// peso seria um BONUS disfarcado de punicao.
        /// </summary>
        public static ResolvedStats ApplyDamage(in ResolvedStats s, float damage, BalanceSettings b)
        {
            float factor = 1f - MathUtil.Clamp01(damage / 100f) * b.DamageStatPenaltyAt100;
            ResolvedStats r = s;
            for (int i = 0; i < StatOps.Count; i++)
            {
                StatId id = (StatId)i;
                if (id == StatId.Weight) continue;
                StatOps.Scale(ref r, id, factor);
            }
            StatOps.ClampToValidRanges(ref r);
            return r;
        }
    }
}
