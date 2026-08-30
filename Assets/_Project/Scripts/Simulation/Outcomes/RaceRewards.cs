// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secoes 10, 15
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Outcomes
{
    /// <summary>
    /// Uma peca que caiu, ainda nao materializada.
    ///
    /// Guardar (base, raridade, iLvl, seed) em vez dos valores rolados mantem o save
    /// pequeno e deixa o pool de afixos rebalancavel sem invalidar inventarios (GDD 10.1).
    /// </summary>
    [System.Serializable]
    public struct PartDrop
    {
        public string BaseId;
        public Rarity Rarity;
        public int ItemLevel;
        public ulong Seed;
    }

    /// <summary>Recompensas roladas pelo LootRoller num fluxo de RNG separado. GDD 20.3.</summary>
    [System.Serializable]
    public class RaceRewards
    {
        public long Cash;
        public long Scrap;
        public int Xp;
        public int Reputation;

        public PartDrop[] Drops;
        public string[] BlueprintIds;

        /// <summary>O Drift Score que gerou estas recompensas. GDD 15.2.</summary>
        public long DriftScore;

        public static readonly PartDrop[] NoDrops = new PartDrop[0];
        public static readonly string[] NoBlueprints = new string[0];

        public RaceRewards()
        {
            Drops = NoDrops;
            BlueprintIds = NoBlueprints;
        }
    }
}
