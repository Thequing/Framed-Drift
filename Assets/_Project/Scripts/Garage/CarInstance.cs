// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 8, 9, 5.7
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>
    /// A visao de runtime de um carro: o registro salvo, a definicao artesanal e o
    /// loadout ja resolvido.
    ///
    /// O loadout e cacheado e invalidado por <see cref="MarkDirty"/> porque a garagem o
    /// reconsulta a cada frame para desenhar as barras (GDD 18.4) - e resolver 8 slots,
    /// afixos, traits, sets e tuning por frame seria desperdicio puro.
    /// </summary>
    public sealed class CarInstance
    {
        public readonly SavedCar Saved;
        public readonly CarDef Definition;

        private readonly LoadoutResolver _resolver;
        private readonly InventoryManager _inventory;

        private CarLoadout _loadout;
        private bool _dirty = true;

        public CarInstance(SavedCar saved, CarDef definition,
                           LoadoutResolver resolver, InventoryManager inventory)
        {
            Saved = saved;
            Definition = definition;
            _resolver = resolver;
            _inventory = inventory;
        }

        public string Id { get { return Definition.Id; } }
        public string DisplayName { get { return Definition.DisplayName; } }
        public float Damage { get { return Saved.Damage; } }

        public SavedBuild ActiveBuild
        {
            get
            {
                if (Saved.Builds.Count == 0) Saved.Builds.Add(new SavedBuild { Name = "BUILD 01" });
                int i = Saved.ActiveBuild < 0 || Saved.ActiveBuild >= Saved.Builds.Count
                    ? 0 : Saved.ActiveBuild;
                return Saved.Builds[i];
            }
        }

        public DriftStyle Style { get { return ActiveBuild.Style; } }

        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>As stats resolvidas: base + pecas + afixos + trait + sets + tuning + dano.</summary>
        public CarLoadout Loadout
        {
            get
            {
                if (_dirty || _loadout == null)
                {
                    _loadout = Resolve(ActiveBuild);
                    _dirty = false;
                }
                return _loadout;
            }
        }

        /// <summary>
        /// Resolve uma build hipotetica sem alterar a ativa.
        ///
        /// E o que o tooltip de comparacao usa (GDD 18.5, "comparacao sempre visivel") e
        /// o que o auto-equipar usa para avaliar uma peca candidata (GDD 16.3). Os dois
        /// passam pela MESMA funcao que a corrida usa - se fossem duas, o tooltip mentiria.
        /// </summary>
        public CarLoadout Resolve(SavedBuild build)
        {
            RolledPart[] equipped = EquippedParts(build);
            var tuning = new TuningSetup
            {
                Lock = build.TuneLock,
                Accel = build.TuneAccel,
                Decel = build.TuneDecel,
            };
            return _resolver.Resolve(Definition, equipped, tuning, Saved.Damage);
        }

        /// <summary>A build ativa com UMA peca trocada. Base do delta de tooltip.</summary>
        public CarLoadout ResolveWith(PartInstance candidate)
        {
            SavedBuild current = ActiveBuild;
            var hypothetical = new SavedBuild
            {
                Name = current.Name,
                SlotUids = (int[])current.SlotUids.Clone(),
                TuneLock = current.TuneLock,
                TuneAccel = current.TuneAccel,
                TuneDecel = current.TuneDecel,
                Style = current.Style,
            };
            hypothetical.SlotUids[(int)candidate.Slot] = candidate.Uid;
            return Resolve(hypothetical);
        }

        public RolledPart[] EquippedParts(SavedBuild build)
        {
            var equipped = new RolledPart[8];
            for (int slot = 0; slot < equipped.Length && slot < build.SlotUids.Length; slot++)
            {
                PartInstance part = _inventory.Get(build.SlotUids[slot]);
                if (part != null) equipped[slot] = part.Rolled;
            }
            return equipped;
        }

        public PartInstance Equipped(PartSlot slot)
        {
            return _inventory.Get(ActiveBuild.SlotUids[(int)slot]);
        }

        /// <summary>
        /// Equipar nao pede confirmacao: e reversivel (GDD 18.5). Desmontar Legendary pede.
        /// </summary>
        public void Equip(PartInstance part)
        {
            if (part == null) return;
            if (!AcceptsSlot(part.Slot)) return;

            ActiveBuild.SlotUids[(int)part.Slot] = part.Uid;
            MarkDirty();
        }

        public void Unequip(PartSlot slot)
        {
            ActiveBuild.SlotUids[(int)slot] = 0;
            MarkDirty();
        }

        public bool AcceptsSlot(PartSlot slot)
        {
            PartSlot[] profile = Definition.SlotProfile;
            if (profile == null || profile.Length == 0) return true;
            for (int i = 0; i < profile.Length; i++)
                if (profile[i] == slot) return true;
            return false;
        }

        public HashSet<int> EquippedUids()
        {
            var uids = new HashSet<int>();
            for (int b = 0; b < Saved.Builds.Count; b++)
                for (int s = 0; s < Saved.Builds[b].SlotUids.Length; s++)
                    if (Saved.Builds[b].SlotUids[s] > 0) uids.Add(Saved.Builds[b].SlotUids[s]);
            return uids;
        }

        // --- estado continuo entre corridas (5.7) -------------------------------------

        public void ApplyRaceDamage(float damage)
        {
            Saved.Damage = UnityEngine.Mathf.Clamp(Saved.Damage + damage, 0f, 100f);
            MarkDirty();
        }

        public void Repair(float amount)
        {
            Saved.Damage = UnityEngine.Mathf.Max(0f, Saved.Damage - amount);
            MarkDirty();
        }
    }
}
