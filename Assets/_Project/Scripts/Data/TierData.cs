// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 14
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 14. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Tier", fileName = "TierData")]
    public sealed class TierData : ScriptableObject
    {
        public TierRank Rank;
        public float DifficultyScale;
        public float RewardScale;

        [Tooltip("Pesos de drop por raridade. Ordem: Common..Legendary. GDD 10.2.")]
        public float[] RarityWeights = new float[5];

        [Tooltip("Teto de item level que este tier pode dropar.")]
        public int ItemLevelCap;
    }
}
