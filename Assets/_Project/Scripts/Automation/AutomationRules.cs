// -----------------------------------------------------------------------------
//  Framed Drift  -  Automation
//  GDD 0.2  secao 16.2
// -----------------------------------------------------------------------------

using System;

namespace FramedDrift.Automation
{
    /// <summary>Criterio objetivo do auto-equipar, escolhido pelo jogador. GDD 16.3.</summary>
    public enum EquipObjective
    {
        DriftScore,
        CashPerHour,
        LapTime,
        Reliability,
        ActiveBuild,
    }

    /// <summary>
    /// O painel de regras da secao 16.2.
    ///
    /// D-06: todo evento que aparece durante o farm tem uma regra padrao que o resolve
    /// na ausencia do jogador. Se o evento raro de 0,2% so contasse com o jogador
    /// olhando, o jogo puniria o modo de uso principal.
    /// </summary>
    [Serializable]
    public class AutomationRules
    {
        public bool AutoStartRace;
        public bool AutoRepair;
        public float AutoRepairThreshold;      // dano > 40

        public bool AutoEquipBetterPart;
        public EquipObjective EquipObjective;

        public bool AutoSalvageCommon;
        public bool AcceptEvents;
        public bool AcceptRivalChallenges;
        public bool SwapToRainBuild;

        public float MinimumRewardMultiplier;  // x1.5
        public float StopIfDamageAbove;        // 80
        public int StopAfterConsecutiveFails;  // 3
    }
}
