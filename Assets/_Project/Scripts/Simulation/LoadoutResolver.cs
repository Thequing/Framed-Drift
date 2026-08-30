// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 7, 8.3, 9.2, 10.5, 5.7
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Achata carro + pecas + ajuste fino + traits + sets + dano num
    /// <see cref="CarLoadout"/> - a unica forma que o simulador conhece.
    ///
    /// Fica no assembly puro porque o auto-equipar precisa resolver uma build candidata
    /// 200 vezes por avaliacao (GDD 16.3) fora do thread principal, e porque a tela de
    /// garagem precisa do mesmo delta de stats que a simulacao usa - se fossem dois
    /// codigos, o tooltip mentiria (GDD 18.5).
    /// </summary>
    public sealed class LoadoutResolver
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;

        public LoadoutResolver(ContentDatabase content)
        {
            _content = content;
            _balance = content.Balance;
        }

        /// <param name="equipped">
        /// Uma entrada por slot da secao 9.1; null significa slot vazio. Indexado por
        /// <see cref="PartSlot"/>.
        /// </param>
        public CarLoadout Resolve(CarDef car, RolledPart[] equipped, TuningSetup tuning, float damage)
        {
            var b = _balance;
            ResolvedStats stats = car.BaseStats;

            var passives = new List<PassiveEffect>();
            var setCounts = new Dictionary<string, int>();

            TireProfile tire = TireProfile.Street;
            DifferentialType diff = DifferentialType.StreetLsd;

            if (equipped != null)
            {
                for (int i = 0; i < equipped.Length; i++)
                {
                    RolledPart p = equipped[i];
                    if (p == null) continue;

                    stats = StatOps.Sum(in stats, p.TotalStats());

                    if (p.HasPassive) passives.Add(p.Passive);
                    if (p.IsTire) tire = p.TireProfile;
                    if (p.IsDifferential) diff = p.DifferentialType;

                    if (!string.IsNullOrEmpty(p.SetId))
                    {
                        int n;
                        setCounts.TryGetValue(p.SetId, out n);
                        setCounts[p.SetId] = n + 1;
                    }
                }
            }

            ApplySetBonuses(setCounts, passives);
            ApplyTrait(car.Trait, ref stats);
            ApplyTuning(tuning, car.TuningRange, ref stats);

            // Passivas de stat plano (bonus de set do tipo "2 pcs +10 Angle", GDD 10.5)
            // entram como stat, nao como efeito condicional - o jogador precisa ve-las na
            // barra da garagem, nao so no tooltip.
            for (int i = 0; i < passives.Count; i++)
                if (passives[i].Kind == PassiveKind.FlatStat)
                    StatOps.Add(ref stats, passives[i].Stat, passives[i].Magnitude);

            StatOps.ClampToValidRanges(ref stats);
            stats = Derived.ApplyDamage(in stats, damage, b);

            return new CarLoadout
            {
                CarId = car.Id,
                Stats = stats,
                Tire = tire,
                Differential = diff,
                Trait = car.Trait,
                Passives = passives.Count == 0 ? CarLoadout.NoPassives : passives.ToArray(),
                BadRiskBonus = Axis(tuning.Decel, car.TuningRange.DecelMin, car.TuningRange.DecelMax)
                               * b.TuningDecelBadRisk,
            };
        }

        /// <summary>
        /// Efeitos de trait que mexem em stat. Os que mudam COMPORTAMENTO
        /// (Motor central, Heranca de corrida, Tracao integral no clima, Carro de rua no
        /// cash) sao tratados onde o comportamento acontece: solver e recompensa.
        /// </summary>
        private void ApplyTrait(TraitId trait, ref ResolvedStats stats)
        {
            var b = _balance;
            switch (trait)
            {
                case TraitId.LightChassis:
                    stats.Weight *= b.TraitLightChassisWeightFactor;
                    stats.Stability += b.TraitLightChassisStability;
                    break;

                case TraitId.RawTorque:
                    stats.Initiation += b.TraitRawTorqueInitiation;
                    stats.Grip += b.TraitRawTorqueGrip;
                    break;

                case TraitId.AllWheelGrip:
                    stats.Angle += b.TraitAllWheelAngle;
                    break;

                case TraitId.MidEngine:
                    stats.Transition += b.TraitMidEngineTransition;
                    break;

                case TraitId.FactoryCooling:
                    stats.Cooling += b.TraitFactoryCooling;
                    break;
            }
        }

        /// <summary>
        /// Ajuste fino do diferencial. Cada eixo e soma-zero-ish: nenhum slider e
        /// "mais e melhor" (GDD 9.4). 0,5 e o neutro.
        /// </summary>
        private void ApplyTuning(TuningSetup t, TuningRange range, ref ResolvedStats stats)
        {
            var b = _balance;

            float lockAxis = Axis(t.Lock, range.LockMin, range.LockMax);
            float accelAxis = Axis(t.Accel, range.AccelMin, range.AccelMax);
            float decelAxis = Axis(t.Decel, range.DecelMin, range.DecelMax);

            stats.Angle += lockAxis * b.TuningLockAngle;
            stats.Grip -= lockAxis * b.TuningLockGrip;

            stats.Initiation += accelAxis * b.TuningAccelInitiation;
            stats.Stability -= accelAxis * b.TuningAccelStability;

            stats.Transition += decelAxis * b.TuningDecelTransition;
        }

        /// <summary>Converte um slider 0-1 (grampeado a faixa do carro) em -1..+1.</summary>
        private static float Axis(float value, float min, float max)
        {
            return (MathUtil.Clamp(value, min, max) - 0.5f) * 2f;
        }

        private void ApplySetBonuses(Dictionary<string, int> counts, List<PassiveEffect> into)
        {
            foreach (KeyValuePair<string, int> kv in counts)
            {
                SetDef set = _content.SetOrNull(kv.Key);
                if (set == null || set.Bonuses == null) continue;

                // Bonus escalonados em 2/3/4 pecas: [0] = 2 pcs, [1] = 3 pcs, [2] = 4 pcs.
                int steps = kv.Value - 1;
                if (steps > set.Bonuses.Length) steps = set.Bonuses.Length;
                for (int i = 0; i < steps; i++)
                    if (set.Bonuses[i].Kind != PassiveKind.None) into.Add(set.Bonuses[i]);
            }
        }
    }
}
