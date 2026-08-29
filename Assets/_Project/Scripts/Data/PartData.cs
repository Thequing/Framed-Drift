// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 10.1
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 10.1. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Part", fileName = "PartData")]
    public sealed class PartData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public PartSlot Slot;
        public TierRank Tier;

        [Tooltip("Contribuicao base antes dos afixos rolados.")]
        public ResolvedStats BaseStats;

        public long SellValue;

        // TODO: AffixPool (GDD 10.3 - pool por slot, com faixa de valor)
        // TODO: PassivePool (GDD 10.4 - condicionais, Rare+)
        // TODO: DropSources (GDD 10.5)

        [Header("Arte")]
        public Sprite Icon;
    }
}
