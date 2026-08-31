// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 10.6, 15.3, 15.5
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Rng;
using UnityEngine;

namespace FramedDrift.Garage
{
    /// <summary>Publicado quando o ultimo pedaco fecha uma planta. GDD 10.6.</summary>
    public struct BlueprintCompleted
    {
        public string PartId;
        public string DisplayName;
    }

    /// <summary>
    /// Salvage, reroll e craft direcionado.
    ///
    /// O crafting existe para CORTAR A CAUDA DO RNG (GDD 10.6): o jogador que cacou uma
    /// peca por horas precisa de um caminho deterministico. Sem isso, a unica resposta
    /// para "nao caiu" e "continue farmando", e a curva de frustracao nao tem teto.
    /// </summary>
    public sealed class Crafting
    {
        private readonly SaveData _save;
        private readonly ContentDatabase _content;
        private readonly InventoryManager _inventory;
        private readonly LootRoller _loot;
        private readonly EconomyLedger _economy;

        public Crafting(SaveData save, ContentDatabase content, InventoryManager inventory,
                        LootRoller loot, EconomyLedger economy)
        {
            _save = save;
            _content = content;
            _inventory = inventory;
            _loot = loot;
            _economy = economy;
        }

        // --- reroll de afixo (dreno de longo prazo, GDD 15.5) ------------------------

        /// <summary>
        /// Custo crescente para rerolar os afixos de uma peca. O custo sobe com a
        /// raridade e o iLvl porque e justamente na peca boa que o jogador vai insistir.
        /// </summary>
        public long RerollCost(PartInstance part)
        {
            if (part == null) return 0L;
            var b = _content.Balance;

            double cost = b.SalvageScrapBase * 6.0
                          * Mathf.Pow(b.SalvageRarityGrowth, (int)part.Rarity)
                          * (1.0 + part.Rolled.ItemLevel * b.SalvageItemLevelFactor);
            return (long)cost;
        }

        public bool CanReroll(PartInstance part)
        {
            return part != null && _economy.Scrap >= RerollCost(part);
        }

        /// <summary>
        /// Reroll: gasta scrap e troca a SEED da peca. Como a rolagem inteira deriva da
        /// seed (GDD 10.1), trocar a seed e literalmente rerolar - nada mais precisa ser
        /// gravado, e o save nao cresce.
        /// </summary>
        public PartInstance Reroll(PartInstance part)
        {
            if (part == null) return null;
            if (!_economy.SpendScrap(RerollCost(part))) return part;

            var rng = new DeterministicRng(part.Saved.Seed ^ (ulong)_save.NextPartUid);
            part.Saved.Seed = rng.NextULong() | 1UL;   // 0 e reservado para peca de fabrica

            // A entrada do inventario e o uid continuam os mesmos: uma build que referencia
            // esta peca segue valida, e o jogador ve a peca "mudar" no lugar dela.
            return _inventory.Refresh(part.Uid);
        }

        // --- craft direcionado -------------------------------------------------------

        /// <summary>
        /// Desmontar 3 pecas do mesmo slot -> Scrap. Scrap + Blueprint -> peca especifica
        /// com rolagem direcionada (GDD 10.6).
        /// </summary>
        public long SalvageBatch(IList<PartInstance> parts)
        {
            long total = 0L;
            for (int i = 0; i < parts.Count; i++)
                total += _inventory.Salvage(parts[i].Uid);
            return total;
        }

        public long CraftCost(string partId, Rarity rarity)
        {
            PartDef def = _content.Part(partId);
            var b = _content.Balance;
            return (long)(def.SellValue * 2.0 * Mathf.Pow(b.SalvageRarityGrowth, (int)rarity));
        }

        public bool HasBlueprint(string partId)
        {
            return _save.Progress.Blueprints.Contains(partId);
        }

        // --- pedacos de planta (10.6) --------------------------------------------------

        /// <summary>Quantos pedacos faltam para fechar uma planta.</summary>
        public int FragmentsPerBlueprint
        {
            get
            {
                int n = _content.Balance.BlueprintFragmentsPerBlueprint;
                return n < 1 ? 1 : n;
            }
        }

        /// <summary>Pedacos ja juntos desta planta. Zero quando ela ja esta completa.</summary>
        public int FragmentsOf(string partId)
        {
            SavedBlueprint entry = FindFragment(partId);
            return entry == null ? 0 : entry.Fragments;
        }

        /// <summary>
        /// Credita um pedaco. Devolve true no pedaco que COMPLETA a planta.
        ///
        /// Uma planta ja completa e ainda nao usada nao acumula um segundo lote: o pedaco
        /// e descartado. Deixar empilhar transformaria a planta - que e um objetivo de
        /// longo prazo (10.6) - numa moeda, e o jogador que ignorasse a bancada juntaria
        /// craft infinito sem decidir nada.
        /// </summary>
        public bool AddBlueprintFragment(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return false;
            if (!_content.Parts.ContainsKey(partId)) return false;
            if (HasBlueprint(partId)) return false;

            SavedBlueprint entry = FindFragment(partId);
            if (entry == null)
            {
                entry = new SavedBlueprint { PartId = partId, Fragments = 0 };
                _save.Progress.BlueprintFragments.Add(entry);
            }

            entry.Fragments++;
            if (entry.Fragments < FragmentsPerBlueprint) return false;

            _save.Progress.BlueprintFragments.Remove(entry);
            _save.Progress.Blueprints.Add(partId);

            EventBus.Publish(new BlueprintCompleted
            {
                PartId = partId,
                DisplayName = _content.Part(partId).DisplayName,
            });
            return true;
        }

        private SavedBlueprint FindFragment(string partId)
        {
            List<SavedBlueprint> list = _save.Progress.BlueprintFragments;
            for (int i = 0; i < list.Count; i++)
                if (list[i].PartId == partId) return list[i];
            return null;
        }

        /// <summary>
        /// Craft com blueprint: gasta scrap, consome o blueprint e entrega a peca na
        /// raridade pedida. E o caminho deterministico - a peca sai garantida.
        /// </summary>
        /// <summary>
        /// A maior raridade que o tier atual permite (GDD 10.2).
        ///
        /// Sai do MESMO `rarityWeights` que o drop usa: peso zero significa "esta
        /// raridade nao existe neste tier", e no tier D isso exclui Epic e Legendary.
        /// </summary>
        public Rarity MaxCraftableRarity()
        {
            TierDef tier = _content.Tier(CurrentTier);
            var best = Rarity.Common;

            for (int i = 0; i < tier.RarityWeights.Length; i++)
                if (tier.RarityWeights[i] > 0f) best = (Rarity)i;

            return best;
        }

        public int MaxCraftableItemLevel()
        {
            return _content.Tier(CurrentTier).ItemLevelCap;
        }

        private TierRank CurrentTier
        {
            get
            {
                int i = _save.Progress.TierIndex;
                if (i < 0) i = 0;
                if (i > (int)TierRank.S) i = (int)TierRank.S;
                return (TierRank)i;
            }
        }

        /// <summary>
        /// Craft direcionado. A raridade e o iLvl sao GRAMPEADOS ao teto do tier.
        ///
        /// Sem o grampo, quem chamasse `Craft(id, Legendary, 999)` receberia a peca: a
        /// bancada era o unico caminho do jogo capaz de produzir raridade que o tier nao
        /// oferece, e isso vazaria por cima de toda a escada da 10.2. O grampo silencioso
        /// e proposital - a UI so oferece o que cabe, entao chegar aqui acima do teto e
        /// bug de chamador, e recusar deixaria o jogador sem a peca e sem a planta.
        /// </summary>
        public PartInstance Craft(string partId, Rarity rarity, int itemLevel)
        {
            if (!HasBlueprint(partId)) return null;
            if (_inventory.IsFull) return null;

            Rarity capped = rarity > MaxCraftableRarity() ? MaxCraftableRarity() : rarity;
            int level = itemLevel < 1 ? 1 : itemLevel;
            if (level > MaxCraftableItemLevel()) level = MaxCraftableItemLevel();

            // Cobrar depois de checar o inventario: debitar e so entao descobrir que a
            // peca nao cabe cobraria o jogador por nada.
            if (!_economy.SpendScrap(CraftCost(partId, capped))) return null;

            _save.Progress.Blueprints.Remove(partId);

            var rng = new DeterministicRng((ulong)(_save.NextPartUid * 2654435761L));
            return _inventory.Add(new Simulation.Outcomes.PartDrop
            {
                BaseId = partId,
                Rarity = capped,
                ItemLevel = level,
                Seed = rng.NextULong() | 1UL,
            });
        }

        // --- reparo (15.3) ---------------------------------------------------------------

        /// <summary>custoReparo(dano) = dano * 6 * (1 + tier * 0.3) * (1 - Reliability/300).</summary>
        public long RepairCost(CarInstance car, int tierIndex)
        {
            var b = _content.Balance;
            float reliability = car.Loadout.Stats.Reliability;

            double cost = car.Damage
                          * b.RepairCostPerDamage
                          * (1.0 + tierIndex * b.RepairCostTierFactor)
                          * (1.0 - reliability / b.RepairCostReliabilityDivisor);

            return cost <= 0.0 ? 0L : (long)cost;
        }

        public bool Repair(CarInstance car, int tierIndex)
        {
            if (car == null) return false;
            if (!_economy.SpendCash(RepairCost(car, tierIndex))) return false;

            car.Repair(100f);
            return true;
        }
    }
}
