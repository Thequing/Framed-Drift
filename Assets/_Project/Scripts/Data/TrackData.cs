// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 11
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 11. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Track", fileName = "TrackData")]
    public sealed class TrackData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public TierRank Tier;

        [Tooltip("Sequencia de modulos. Montada a mao no MVP; gerada na Fase 12.")]
        public ModuleData[] Modules;

        [Tooltip("Tempo de referencia base para o grid de adversarios. GDD 5.6.")]
        public float BaseTimeSeconds;

        public float TrackScoreMultiplier;
    }
}
