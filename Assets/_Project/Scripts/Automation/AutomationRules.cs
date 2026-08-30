// -----------------------------------------------------------------------------
//  Framed Drift  -  Automation
//  GDD 0.2  secao 16.2
// -----------------------------------------------------------------------------

using System;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation;

namespace FramedDrift.Automation
{
    /// <summary>
    /// O painel de regras da secao 16.2.
    ///
    /// D-06: todo evento que aparece durante o farm tem uma regra padrao que o resolve
    /// na ausencia do jogador. Se o evento raro de 0,2% so contasse com o jogador
    /// olhando, o jogo puniria o modo de uso principal.
    ///
    /// Esta e a visao de RUNTIME das regras; a persistida e <see cref="SavedAutomation"/>.
    /// Sao duas porque o save precisa de tipos serializaveis simples e o runtime precisa
    /// do enum tipado - converter num lugar so evita que divirjam.
    /// </summary>
    [Serializable]
    public class AutomationRules
    {
        public bool AutoStartRace;
        public bool AutoRepair;
        public float AutoRepairThreshold = 40f;

        public bool AutoEquipBetterPart;
        public EquipObjective EquipObjective = EquipObjective.DriftScore;   // FramedDrift.Garage

        public bool AutoSalvageCommon;
        public bool AutoSalvageUncommon;
        public bool AcceptEvents = true;
        public bool AcceptRivalChallenges;
        public bool SwapToRainBuild;

        public float MinimumRewardMultiplier = 1f;
        public float StopIfDamageAbove = 80f;
        public int StopAfterConsecutiveFails = 3;

        public static AutomationRules From(SavedAutomation saved)
        {
            return new AutomationRules
            {
                AutoStartRace = saved.AutoStartRace,
                AutoRepair = saved.AutoRepair,
                AutoRepairThreshold = saved.AutoRepairThreshold,
                AutoEquipBetterPart = saved.AutoEquipBetterPart,
                EquipObjective = (EquipObjective)saved.EquipObjective,
                AutoSalvageCommon = saved.AutoSalvageCommon,
                AutoSalvageUncommon = saved.AutoSalvageUncommon,
                AcceptEvents = saved.AcceptEvents,
                AcceptRivalChallenges = saved.AcceptRivalChallenges,
                SwapToRainBuild = saved.SwapToRainBuild,
                MinimumRewardMultiplier = saved.MinimumRewardMultiplier,
                StopIfDamageAbove = saved.StopIfDamageAbove,
                StopAfterConsecutiveFails = saved.StopAfterConsecutiveFails,
            };
        }

        public void CopyTo(SavedAutomation saved)
        {
            saved.AutoStartRace = AutoStartRace;
            saved.AutoRepair = AutoRepair;
            saved.AutoRepairThreshold = AutoRepairThreshold;
            saved.AutoEquipBetterPart = AutoEquipBetterPart;
            saved.EquipObjective = (int)EquipObjective;
            saved.AutoSalvageCommon = AutoSalvageCommon;
            saved.AutoSalvageUncommon = AutoSalvageUncommon;
            saved.AcceptEvents = AcceptEvents;
            saved.AcceptRivalChallenges = AcceptRivalChallenges;
            saved.SwapToRainBuild = SwapToRainBuild;
            saved.MinimumRewardMultiplier = MinimumRewardMultiplier;
            saved.StopIfDamageAbove = StopIfDamageAbove;
            saved.StopAfterConsecutiveFails = StopAfterConsecutiveFails;
        }

        /// <summary>A forma que a reconstrucao offline consome. GDD 17.2, passo 5.</summary>
        public OfflineRules ToOfflineRules(int inventoryFreeSlots)
        {
            return new OfflineRules
            {
                AutoRepair = AutoRepair,
                AutoRepairThreshold = AutoRepairThreshold,
                AutoSalvageCommon = AutoSalvageCommon,
                AutoSalvageUncommon = AutoSalvageUncommon,
                StopIfDamageAbove = StopIfDamageAbove,
                StopAfterConsecutiveFails = StopAfterConsecutiveFails,
                InventoryFreeSlots = inventoryFreeSlots,
            };
        }
    }
}
