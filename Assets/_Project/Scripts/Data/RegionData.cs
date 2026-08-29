// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 11 / 19.4
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 11 / 19.4. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Region", fileName = "RegionData")]
    public sealed class RegionData : ScriptableObject
    {
        public string Id;
        public string DisplayName;

        public ModuleData[] AvailableModules;
        public Weather[] PossibleWeather;

        [Tooltip("Cada cenario precisa mudar geometria, iluminacao, clima, trafego, " +
                 "obstaculos, eventos, drops e trilha - nao apenas textura. GDD 19.4.")]
        public PartData[] PartPool;

        public float CashMultiplier;
        public float ScoreMultiplier;

        // TODO: Rivais (GDD 13), trilha sonora (GDD 19.5)
    }
}
