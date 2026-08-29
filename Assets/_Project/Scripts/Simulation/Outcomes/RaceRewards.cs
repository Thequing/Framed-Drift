// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secoes 10, 15
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Outcomes
{
    /// <summary>Recompensas roladas pelo LootRoller num fluxo de RNG separado. GDD 20.3.</summary>
    [System.Serializable]
    public class RaceRewards
    {
        public long Cash;
        public long Scrap;
        public int Xp;
        public int Reputation;

        /// <summary>Sementes de peca. A peca so e materializada quando o jogador coleta.</summary>
        public ulong[] LootSeeds;

        public string[] BlueprintIds;
    }
}
