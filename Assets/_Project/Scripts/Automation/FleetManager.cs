// -----------------------------------------------------------------------------
//  Framed Drift  -  Automation
//  GDD 0.2  secoes 16.4, 15.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation.Balance;
using UnityEngine;

namespace FramedDrift.Automation
{
    /// <summary>Uma vaga de garagem em operacao. GDD 16.4.</summary>
    public sealed class FleetEntry
    {
        public SavedFleetSlot Saved;
        public CarInstance Car;

        /// <summary>Segundos ate a proxima corrida deste carro.</summary>
        public float NextRaceIn;

        /// <summary>Log curto da operacao, so os ultimos eventos.</summary>
        public readonly List<string> Log = new List<string>();

        public bool IsEmpty { get { return Car == null; } }

        public void Note(string line)
        {
            Log.Add(line);
            if (Log.Count > 32) Log.RemoveAt(0);
        }
    }

    /// <summary>
    /// A frota: carro -> pista -> regras -> log, uma linha por vaga.
    ///
    /// Comeca em 2 vagas e escala ate ~12 (GDD 16.4). Cada carro tem a SUA build, pista,
    /// regras e log - o painel principal do modo Completo e essa lista, e e ela que
    /// transforma "aperte correr" em "opere uma organizacao" (GDD 16).
    ///
    /// Vagas paralelas nao dividem tempo entre si: cada uma roda o seu proprio ciclo. O
    /// custo e desprezivel (uma corrida custa 0,006 ms), entao o limite de 12 e de
    /// legibilidade de UI, nao de CPU.
    /// </summary>
    public sealed class FleetManager
    {
        public const int StartingSlots = 2;
        public const int MaxSlots = 12;

        private readonly SaveData _save;
        private readonly BalanceSettings _balance;
        private readonly List<FleetEntry> _entries = new List<FleetEntry>();

        public FleetManager(SaveData save, BalanceSettings balance)
        {
            _save = save;
            _balance = balance;
            EnsureSlots(StartingSlots);
        }

        public IReadOnlyList<FleetEntry> Entries { get { return _entries; } }
        public int SlotCount { get { return _entries.Count; } }

        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _entries.Count; i++)
                    if (!_entries[i].IsEmpty && _entries[i].Saved.Running) n++;
                return n;
            }
        }

        public FleetEntry Primary
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                    if (!_entries[i].IsEmpty) return _entries[i];
                return _entries.Count > 0 ? _entries[0] : null;
            }
        }

        public void EnsureSlots(int count)
        {
            count = Mathf.Clamp(count, 1, MaxSlots);

            while (_save.Fleet.Count < count) _save.Fleet.Add(new SavedFleetSlot());
            while (_entries.Count < count)
                _entries.Add(new FleetEntry { Saved = _save.Fleet[_entries.Count] });
        }

        /// <summary>custoSlotGaragem(n) = 5.000 * 2,4^(n-2). GDD 15.3.</summary>
        public long NextSlotCost()
        {
            return (long)(_balance.GarageSlotCostBase
                          * Mathf.Pow(_balance.GarageSlotCostGrowth, _entries.Count - 1));
        }

        public bool BuySlot(Progression.EconomyLedger economy)
        {
            if (_entries.Count >= MaxSlots) return false;

            long cost = NextSlotCost();
            if (!economy.SpendCash(cost)) return false;

            EnsureSlots(_entries.Count + 1);
            return true;
        }

        public void Assign(int slotIndex, CarInstance car, int carIndex, string trackId)
        {
            if (slotIndex < 0 || slotIndex >= _entries.Count) return;

            FleetEntry entry = _entries[slotIndex];
            entry.Car = car;
            entry.Saved.CarIndex = carIndex;
            entry.Saved.TrackId = trackId;
            entry.NextRaceIn = 0f;
        }

        public void SetRunning(int slotIndex, bool running)
        {
            if (slotIndex < 0 || slotIndex >= _entries.Count) return;
            _entries[slotIndex].Saved.Running = running;
        }

        /// <summary>Reconecta as vagas salvas aos carros ja materializados, na carga.</summary>
        public void Rebind(IList<CarInstance> cars)
        {
            EnsureSlots(_save.Fleet.Count < StartingSlots ? StartingSlots : _save.Fleet.Count);

            for (int i = 0; i < _entries.Count; i++)
            {
                int index = _entries[i].Saved.CarIndex;
                _entries[i].Car = index >= 0 && index < cars.Count ? cars[index] : null;
            }
        }

        /// <summary>Intervalo entre corridas. GDD 17.2 usa a mesma constante.</summary>
        public float InterRaceDelay { get { return _balance.InterRaceDelaySeconds; } }
    }
}
