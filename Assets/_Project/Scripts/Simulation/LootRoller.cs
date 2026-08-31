// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 10
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Rola raridade, afixos e passivas usando o fluxo de RNG de loot - separado do
    /// fluxo de execucao para que rebalancear drops nao altere corridas (GDD 20.3).
    ///
    /// Uma peca cai como <see cref="PartDrop"/> (base + raridade + iLvl + seed) e so vira
    /// <see cref="RolledPart"/> quando alguem precisa dos numeros. Rolar duas vezes a
    /// mesma seed tem que dar a mesma peca: e disso que dependem o save compacto e a
    /// possibilidade de rebalancear pools sem invalidar inventarios (GDD 10.1).
    /// </summary>
    public sealed class LootRoller
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;

        public LootRoller(ContentDatabase content)
        {
            _content = content;
            _balance = content.Balance;
        }

        // --- drop da corrida ------------------------------------------------------

        /// <summary>
        /// Quantas pecas caem e quais. O score empurra a chance de drop com retorno
        /// decrescente - "Fazer drift paga multiplicadores de drop" (GDD 3.5) sem que
        /// uma build de score de 12 milhoes vire uma maquina de loot infinito.
        /// </summary>
        public PartDrop[] RollDrops(RaceInstance race, RaceResult result, long driftScore,
                                    float dropChanceBonus, DeterministicRng rng)
        {
            var b = _balance;
            TierDef tier = _content.Tier((TierRank)MathUtil.Clamp(race.TierIndex, 0, 4));

            float scorePush = driftScore <= 0L
                ? 0f
                : (float)(driftScore / (driftScore + (double)b.DropChanceScoreDivisor));

            float chance = b.DropChanceBase
                           + result.RiskIndex * b.DropChancePerRisk
                           + scorePush
                           + dropChanceBonus
                           + race.Car.Passive(PassiveKind.DropChanceBonus);

            chance = MathUtil.Clamp(chance, 0f, b.DropChanceMax);

            // Uma peca garantida quando a rolagem passa, e uma segunda pelo excedente.
            var drops = new List<PartDrop>(2);
            if (rng.Chance(chance)) drops.Add(RollOne(race, tier, result.RiskIndex, rng));
            if (chance > 1f && rng.Chance(chance - 1f)) drops.Add(RollOne(race, tier, result.RiskIndex, rng));

            return drops.Count == 0 ? RaceRewards.NoDrops : drops.ToArray();
        }

        private PartDrop RollOne(RaceInstance race, TierDef tier, float riskIndex, DeterministicRng rng)
        {
            return new PartDrop
            {
                BaseId = PickBase(race, rng),
                Rarity = RollRarity(tier, riskIndex, rng),
                ItemLevel = RollItemLevel(tier, rng),
                Seed = rng.NextULong(),
            };
        }

        /// <summary>
        /// Um pedaco de planta, ou null. GDD 10.6.
        ///
        /// Roda no fluxo de EVENTOS, nunca no de loot (GDD 20.3 / 15.1, que lista planta
        /// como vinda de "drops raros, rivais, eventos"). Nao e detalhe: consumir o fluxo
        /// de loot aqui deslocaria toda a sequencia de drops ja existente, e uma seed
        /// antiga passaria a render outras pecas.
        ///
        /// O alvo e sorteado com peso INVERSO ao dropWeight. A planta existe para cortar
        /// a cauda do RNG (10.6), entao ela precisa apontar para a peca que quase nunca
        /// cai - sortear com o peso normal faria a planta repetir o que o loot ja da.
        ///
        /// Peca de serie (buyCost 0) fica de fora: ela e gratis e ja vem montada, entao
        /// uma planta dela seria progresso gasto em nada. Sao 7 das 20 bases tier D, e
        /// inclui-las diluia um terco dos pedacos.
        /// </summary>
        public string RollBlueprintFragment(RaceInstance race, DeterministicRng rng)
        {
            var b = _balance;
            if (b.BlueprintFragmentChance <= 0f) return null;
            if (!rng.Chance(b.BlueprintFragmentChance)) return null;

            string[] pool = race.PartPool;
            if (pool == null || pool.Length == 0) return null;

            int maxTier = MathUtil.Clamp(race.TierIndex, 0, (int)TierRank.S);

            var weights = new float[pool.Length];
            bool any = false;
            for (int i = 0; i < pool.Length; i++)
            {
                PartDef def = _content.Part(pool[i]);
                if ((int)def.Tier > maxTier) continue;
                if (def.DropWeight <= 0f) continue;
                if (def.BuyCost <= 0L) continue;   // peca de serie: planta dela nao vale nada

                weights[i] = 1f / def.DropWeight;
                any = true;
            }
            if (!any) return null;

            int index = rng.WeightedIndex(weights);
            return index < 0 ? null : pool[index];
        }

        /// <summary>
        /// Sorteia a base, do pool da regiao, LIMITADO AO TIER DA CORRIDA.
        ///
        /// O pool da regiao lista todas as pecas que existem ali, de D a S. Sem o corte
        /// por tier, uma corrida tier D dropava peca tier B no minuto zero: a escada de
        /// progressao inteira (GDD 10.2, 14.2) desabaria no primeiro drop de sorte, e a
        /// loja - cujo catalogo E o tier - perderia a razao de existir.
        ///
        /// O peso de quem passa no corte nao muda: cortar nao redistribui, so remove.
        /// </summary>
        private string PickBase(RaceInstance race, DeterministicRng rng)
        {
            string[] pool = race.PartPool;
            if (pool == null || pool.Length == 0)
                throw new ContentException("A corrida em " + race.TrackId + " nao tem pool de pecas.");

            int maxTier = MathUtil.Clamp(race.TierIndex, 0, (int)TierRank.S);

            var weights = new float[pool.Length];
            int firstEligible = -1;
            for (int i = 0; i < pool.Length; i++)
            {
                PartDef def = _content.Part(pool[i]);
                if ((int)def.Tier > maxTier) continue;    // ainda nao e para este jogador

                weights[i] = def.DropWeight;
                if (firstEligible < 0) firstEligible = i;
            }

            // Um pool sem NENHUMA peca do tier da corrida e erro de conteudo, nao azar:
            // silenciar com a primeira peca do pool entregaria uma peca acima do tier.
            if (firstEligible < 0)
                throw new ContentException("O pool de " + race.TrackId + " nao tem peca de tier "
                                           + (TierRank)maxTier + " ou abaixo.");

            // O fallback tambem precisa respeitar o corte: pool[0] pode ser tier S.
            int index = rng.WeightedIndex(weights);
            return pool[index < 0 ? firstEligible : index];
        }

        /// <summary>
        /// Pesos deslizam com o tier da pista e com o modificador de risco (GDD 10.2).
        /// O risco desloca peso das raridades baixas para as altas sem mudar o total.
        /// </summary>
        private Rarity RollRarity(TierDef tier, float riskIndex, DeterministicRng rng)
        {
            float[] weights = tier.RarityWeights;
            var shifted = new float[weights.Length];

            float push = riskIndex / 100f;
            for (int i = 0; i < weights.Length; i++)
            {
                // Common encolhe, Legendary cresce; o expoente e o degrau de raridade.
                float bias = 1f + push * (i - 1f) * 0.6f;
                shifted[i] = MathUtil.Max(0f, weights[i] * bias);
            }

            int index = rng.WeightedIndex(shifted);
            return (Rarity)MathUtil.Clamp(index < 0 ? 0 : index, 0, 4);
        }

        private int RollItemLevel(TierDef tier, DeterministicRng rng)
        {
            int cap = tier.ItemLevelCap < 1 ? 1 : tier.ItemLevelCap;
            int floor = cap / 2 < 1 ? 1 : cap / 2;
            return rng.Range(floor, cap + 1);
        }

        // --- materializacao ---------------------------------------------------------

        /// <summary>
        /// Reconstroi a peca a partir da seed. Determinismo aqui e requisito de save:
        /// a mesma seed precisa dar a mesma peca em toda carga (GDD 10.1).
        /// </summary>
        public RolledPart Materialize(PartDrop drop)
        {
            var b = _balance;
            PartDef def = _content.Part(drop.BaseId);
            var rng = new DeterministicRng(drop.Seed);

            var part = new RolledPart
            {
                BaseId = def.Id,
                DisplayName = def.DisplayName,
                Slot = def.Slot,
                Rarity = drop.Rarity,
                ItemLevel = drop.ItemLevel,
                Seed = drop.Seed,
                BaseStats = def.BaseStats,
                SetId = def.SetId,
                IsTire = def.IsTire,
                TireProfile = def.TireProfile,
                IsDifferential = def.IsDifferential,
                DifferentialType = def.DifferentialType,
                SellValue = def.SellValue,
            };

            part.Affixes = RollAffixes(def, drop, rng);
            RollPassive(def, drop, rng, part);
            return part;
        }

        /// <summary>
        /// A peca de fabrica: base artesanal, zero afixos, Common.
        ///
        /// E o que o carro inicial vem equipado e o que a loja vende. Precisa existir
        /// separado de <see cref="Materialize"/> porque uma peca "stock" com afixos
        /// rolados nao e stock - ela seria a linha de base do balanceamento e o
        /// balanceamento passaria a medir a sorte da rolagem inicial.
        /// </summary>
        public RolledPart Factory(string partId)
        {
            PartDef def = _content.Part(partId);
            return new RolledPart
            {
                BaseId = def.Id,
                DisplayName = def.DisplayName,
                Slot = def.Slot,
                Rarity = Rarity.Common,
                ItemLevel = 1,
                Seed = 0,
                BaseStats = def.BaseStats,
                Affixes = RolledPart.NoAffixes,
                SetId = def.SetId,
                IsTire = def.IsTire,
                TireProfile = def.TireProfile,
                IsDifferential = def.IsDifferential,
                DifferentialType = def.DifferentialType,
                SellValue = def.SellValue,
            };
        }

        private RolledAffix[] RollAffixes(PartDef def, PartDrop drop, DeterministicRng rng)
        {
            var b = _balance;
            int count = b.AffixCount(drop.Rarity);
            if (count <= 0) return RolledPart.NoAffixes;

            List<AffixDef> pool = PoolForSlot(def.Slot);
            if (pool.Count == 0) return RolledPart.NoAffixes;

            // Passo 1: escolhe QUAIS afixos, sem rolar valor ainda.
            //
            // Os dois passos sao separados porque o bonus de trade-off (GDD 10.3) depende
            // de quantos afixos prejudiciais a peca acabou tendo - e isso so se sabe
            // depois que todos foram escolhidos.
            var chosen = new List<AffixDef>(count);
            var used = new HashSet<string>();
            int drawbacks = 0;

            for (int i = 0; i < count; i++)
            {
                AffixDef pick = PickAffix(pool, used, rng, i > 0 && rng.Chance(b.NegativeAffixChance));
                if (pick == null) break;
                used.Add(pick.Id);
                chosen.Add(pick);
                if (pick.IsDrawback) drawbacks++;
            }

            if (chosen.Count == 0) return RolledPart.NoAffixes;

            // Passo 2: rola os valores. Quem carrega o trade-off rola mais alto.
            float rarityScale = 1f + (int)drop.Rarity * b.RarityValueScale;
            float drawbackBonus = 1f + drawbacks * b.NegativeAffixValueBonus;

            var result = new List<RolledAffix>(chosen.Count);
            for (int i = 0; i < chosen.Count; i++)
            {
                AffixDef a = chosen[i];
                float span = a.MaxValue - a.MinValue;
                float roll = a.MinValue + span * rng.NextFloat();
                float value = (roll + a.PerItemLevel * drop.ItemLevel) * rarityScale;

                if (!a.IsDrawback) value *= drawbackBonus;

                result.Add(new RolledAffix
                {
                    AffixId = a.Id,
                    Stat = a.Stat,
                    Value = MathUtil.Max(1f, value) * (a.Sign < 0 ? -1f : 1f),
                });
            }

            return result.ToArray();
        }

        /// <param name="wantDrawback">
        /// Quando true, o sorteio e restrito aos afixos prejudiciais. Nunca no primeiro
        /// afixo: uma peca cujo unico afixo e um prejuizo nao e "alto risco", e lixo.
        /// </param>
        private static AffixDef PickAffix(List<AffixDef> pool, HashSet<string> used,
                                          DeterministicRng rng, bool wantDrawback)
        {
            AffixDef pick = PickFrom(pool, used, rng, wantDrawback);
            if (pick == null && wantDrawback) pick = PickFrom(pool, used, rng, false);
            return pick;
        }

        private static AffixDef PickFrom(List<AffixDef> pool, HashSet<string> used,
                                         DeterministicRng rng, bool drawbacksOnly)
        {
            var weights = new float[pool.Count];
            bool any = false;
            for (int i = 0; i < pool.Count; i++)
            {
                if (used.Contains(pool[i].Id)) continue;
                if (drawbacksOnly != pool[i].IsDrawback) continue;
                weights[i] = pool[i].Weight;
                if (weights[i] > 0f) any = true;
            }
            if (!any) return null;

            int index = rng.WeightedIndex(weights);
            return index < 0 ? null : pool[index];
        }

        private void RollPassive(PartDef def, PartDrop drop, DeterministicRng rng, RolledPart part)
        {
            var b = _balance;

            // Passivas so a partir de Rare, e em Rare so em 40% das vezes (GDD 10.2).
            if (drop.Rarity < Rarity.Rare) return;
            if (drop.Rarity == Rarity.Rare && !rng.Chance(b.RarePassiveChance)) return;

            var pool = new List<PassiveDef>();
            for (int i = 0; i < _content.PassiveList.Count; i++)
            {
                PassiveDef p = _content.PassiveList[i];
                if (drop.Rarity < p.MinRarity) continue;
                if (!SlotAllowed(p.Slots, def.Slot)) continue;
                pool.Add(p);
            }
            if (pool.Count == 0) return;

            var weights = new float[pool.Count];
            for (int i = 0; i < pool.Count; i++) weights[i] = pool[i].Weight;

            int index = rng.WeightedIndex(weights);
            if (index < 0) return;

            PassiveDef chosen = pool[index];
            part.HasPassive = true;
            part.Passive = chosen.ToEffect();
            part.PassiveId = chosen.Id;
            part.PassiveText = chosen.DisplayName;
        }

        private List<AffixDef> PoolForSlot(PartSlot slot)
        {
            var pool = new List<AffixDef>();
            for (int i = 0; i < _content.AffixList.Count; i++)
                if (SlotAllowed(_content.AffixList[i].Slots, slot))
                    pool.Add(_content.AffixList[i]);
            return pool;
        }

        private static bool SlotAllowed(PartSlot[] slots, PartSlot slot)
        {
            if (slots == null || slots.Length == 0) return true;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == slot) return true;
            return false;
        }

        // --- salvage (10.6) ------------------------------------------------------------

        /// <summary>
        /// Scrap de desmontar. Escala por raridade e iLvl (GDD 10.6).
        ///
        /// O auto-desmontar sai junto com o inventario, nunca depois: um idle que enche o
        /// inventario e para de progredir enquanto o jogador dorme quebra o pilar P3
        /// (GDD 10.7 / risco R7).
        /// </summary>
        public long SalvageValue(RolledPart part)
        {
            var b = _balance;
            double scrap = b.SalvageScrapBase
                           * MathUtil.Pow(b.SalvageRarityGrowth, (int)part.Rarity)
                           * (1f + part.ItemLevel * b.SalvageItemLevelFactor);
            return (long)System.Math.Floor(scrap);
        }

        public long SalvageValue(PartDrop drop)
        {
            var b = _balance;
            double scrap = b.SalvageScrapBase
                           * MathUtil.Pow(b.SalvageRarityGrowth, (int)drop.Rarity)
                           * (1f + drop.ItemLevel * b.SalvageItemLevelFactor);
            return (long)System.Math.Floor(scrap);
        }
    }
}
