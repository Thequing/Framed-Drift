// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 10.6, 10.7
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Garage
{
    /// <summary>Evento publicado quando uma peca entra no inventario.</summary>
    public struct PartAcquired
    {
        public PartInstance Part;
    }

    /// <summary>Evento publicado quando o inventario enche. Risco R7.</summary>
    public struct InventoryFull { }

    /// <summary>
    /// Cap inicial de 60 slots, expansivel (GDD 10.7).
    ///
    /// O auto-desmontar por raridade e desbloqueado JUNTO com o inventario, nao depois:
    /// um idle game que enche o inventario e para de progredir enquanto o jogador dorme
    /// quebra o pilar P3 (risco R7). Por isso as regras de auto-desmonte vivem aqui e nao
    /// atras de um degrau da escada de automacao.
    /// </summary>
    public sealed class InventoryManager
    {
        private readonly SaveData _save;
        private readonly LootRoller _loot;
        private readonly Dictionary<int, PartInstance> _materialized = new Dictionary<int, PartInstance>();

        public InventoryManager(SaveData save, LootRoller loot)
        {
            _save = save;
            _loot = loot;
        }

        public int Count { get { return _save.Inventory.Count; } }
        public int Capacity { get { return _save.InventoryCap; } }
        public bool IsFull { get { return _save.InventoryFull; } }

        // --- acesso ----------------------------------------------------------------

        public PartInstance Get(int uid)
        {
            if (uid <= 0) return null;

            PartInstance cached;
            if (_materialized.TryGetValue(uid, out cached)) return cached;

            SavedPart saved = _save.FindPart(uid);
            if (saved == null) return null;

            PartInstance instance = PartInstance.Materialize(saved, _loot);
            _materialized[uid] = instance;
            return instance;
        }

        public List<PartInstance> All()
        {
            var list = new List<PartInstance>(_save.Inventory.Count);
            for (int i = 0; i < _save.Inventory.Count; i++)
            {
                PartInstance p = Get(_save.Inventory[i].Uid);
                if (p != null) list.Add(p);
            }
            return list;
        }

        public List<PartInstance> InSlot(PartSlot slot)
        {
            var list = new List<PartInstance>();
            for (int i = 0; i < _save.Inventory.Count; i++)
            {
                PartInstance p = Get(_save.Inventory[i].Uid);
                if (p != null && p.Slot == slot) list.Add(p);
            }
            return list;
        }

        // --- entrada ----------------------------------------------------------------

        /// <summary>Cria uma peca de fabrica (sem afixos) e a coloca no inventario.</summary>
        public PartInstance AddFactory(string baseId)
        {
            return Add(new SavedPart
            {
                BaseId = baseId,
                Rarity = Rarity.Common,
                ItemLevel = 1,
                Seed = 0UL,
            });
        }

        /// <summary>Guarda um drop. Devolve null quando o inventario esta cheio.</summary>
        public PartInstance Add(PartDrop drop)
        {
            return Add(new SavedPart
            {
                BaseId = drop.BaseId,
                Rarity = drop.Rarity,
                ItemLevel = drop.ItemLevel,
                Seed = drop.Seed,
            });
        }

        private PartInstance Add(SavedPart saved)
        {
            if (IsFull)
            {
                EventBus.Publish(new InventoryFull());
                return null;
            }

            saved.Uid = _save.NextPartUid++;
            _save.Inventory.Add(saved);

            PartInstance instance = PartInstance.Materialize(saved, _loot);
            _materialized[saved.Uid] = instance;

            EventBus.Publish(new PartAcquired { Part = instance });
            return instance;
        }

        // --- saida -------------------------------------------------------------------

        /// <summary>
        /// Descarta a materializacao em cache e refaz a partir da seed atual.
        /// O reroll (GDD 10.6) troca a seed no registro salvo e chama isto - a peca
        /// continua sendo a mesma entrada do inventario, com o mesmo uid, entao nenhuma
        /// build que a referencia se rompe.
        /// </summary>
        public PartInstance Refresh(int uid)
        {
            _materialized.Remove(uid);
            return Get(uid);
        }

        /// <summary>Remove sem gerar scrap. Usado ao equipar em outro carro, nao ao vender.</summary>
        public bool Remove(int uid)
        {
            for (int i = 0; i < _save.Inventory.Count; i++)
            {
                if (_save.Inventory[i].Uid != uid) continue;
                _save.Inventory.RemoveAt(i);
                _materialized.Remove(uid);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Desmonta e devolve o scrap. Pecas travadas pelo jogador nunca sao desmontadas -
        /// nem por ele sem querer, nem pela automacao.
        /// </summary>
        public long Salvage(int uid)
        {
            PartInstance part = Get(uid);
            if (part == null || part.Locked) return 0L;

            long scrap = _loot.SalvageValue(part.Rolled);
            Remove(uid);
            _save.Scrap += scrap;
            return scrap;
        }

        /// <summary>
        /// Auto-desmontar por raridade (GDD 10.7).
        ///
        /// Nunca desmonta peca equipada nem travada. Recebe os uids equipados de fora
        /// porque o inventario nao conhece builds - e nao deveria.
        /// </summary>
        public long AutoSalvage(bool common, bool uncommon, HashSet<int> equippedUids)
        {
            long total = 0L;

            for (int i = _save.Inventory.Count - 1; i >= 0; i--)
            {
                SavedPart saved = _save.Inventory[i];
                if (saved.Locked) continue;
                if (equippedUids != null && equippedUids.Contains(saved.Uid)) continue;

                bool wanted = (common && saved.Rarity == Rarity.Common)
                              || (uncommon && saved.Rarity == Rarity.Uncommon);
                if (!wanted) continue;

                total += Salvage(saved.Uid);
            }

            return total;
        }

        /// <summary>Expandir o inventario e um dreno de longo prazo. GDD 15.5.</summary>
        public void ExpandCapacity(int slots)
        {
            _save.InventoryCap += slots;
        }
    }
}
