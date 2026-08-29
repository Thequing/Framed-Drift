// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 8.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 8.2. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Car", fileName = "CarData")]
    public sealed class CarData : ScriptableObject
    {
        [Header("Identidade")]
        public string Id;
        public string DisplayName;

        [Header("Mecanica")]
        public DriveLayout Layout;
        public TierRank Tier;

        [Tooltip("As 12 stats primarias antes de pecas e tuning. GDD 7.1.")]
        public ResolvedStats BaseStats;

        [Tooltip("Quais dos 8 slots este carro aceita. GDD 9.1.")]
        public PartSlot[] SlotProfile;

        // TODO: Trait (GDD 8.3 - exatamente uma por carro, e o principal vetor de desejo)
        // TODO: TuningRange (GDD 9.2 - limites por eixo de ajuste fino)
        // TODO: UnlockRule (GDD 8.2 - reputacao, blueprint, rival derrotado, regiao)

        [Header("Arte")]
        [Tooltip("Deixado em branco de proposito - o modelo entra depois. " +
                 "Restricoes de silhueta: legivel a 360x48 E com yaw legivel da camera " +
                 "traseira elevada. GDD 19.1.")]
        public GameObject Prefab;
    }
}
