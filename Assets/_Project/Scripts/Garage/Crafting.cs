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

        /// <summary>
        /// Craft com blueprint: gasta scrap, consome o blueprint e entrega a peca na
        /// raridade pedida. E o caminho deterministico - a peca sai garantida.
        /// </summary>
        public PartInstance Craft(string partId, Rarity rarity, int itemLevel)
        {
            if (!HasBlueprint(partId)) return null;
            if (_inventory.IsFull) return null;

            // Cobrar depois de checar o inventario: debitar e so entao descobrir que a
            // peca nao cabe cobraria o jogador por nada.
            if (!_economy.SpendScrap(CraftCost(partId, rarity))) return null;

            _save.Progress.Blueprints.Remove(partId);

            var rng = new DeterministicRng((ulong)(_save.NextPartUid * 2654435761L));
            return _inventory.Add(new Simulation.Outcomes.PartDrop
            {
                BaseId = partId,
                Rarity = rarity,
                ItemLevel = itemLevel,
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
