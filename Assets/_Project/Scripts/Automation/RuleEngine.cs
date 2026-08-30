// -----------------------------------------------------------------------------
//  Framed Drift  -  Automation
//  GDD 0.2  secoes 16.2, 16.3, D-06
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Progression;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Automation
{
    /// <summary>O que a automacao decidiu fazer depois de uma corrida. Vira log e toast.</summary>
    public struct AutomationAction
    {
        public string Text;
    }

    /// <summary>
    /// Aplica as regras da secao 16.2 depois de cada corrida.
    ///
    /// D-06 em codigo: este e o objeto que garante que nenhuma mecanica exija o jogador
    /// presente. Tudo que a tela de resultado oferece como escolha - reparar, desmontar,
    /// equipar a peca melhor, aceitar um evento - tem aqui a sua regra padrao.
    ///
    /// A ordem importa e e a mesma do passo 5 da secao 17.2: reparo, depois desmonte,
    /// depois equipar, depois parada. Reparar antes de desmontar significa que o cash do
    /// reparo sai antes do scrap entrar, que e o pior caso - e o pior caso e o que o
    /// jogador precisa ver refletido no relatorio.
    /// </summary>
    public sealed class RuleEngine
    {
        private readonly SaveData _save;
        private readonly ContentDatabase _content;
        private readonly InventoryManager _inventory;
        private readonly Crafting _crafting;
        private readonly BuildManager _builds;
        private readonly EconomyLedger _economy;
        private readonly ReputationSystem _reputation;

        private int _consecutiveFailures;

        public RuleEngine(SaveData save, ContentDatabase content, InventoryManager inventory,
                          Crafting crafting, BuildManager builds, EconomyLedger economy,
                          ReputationSystem reputation)
        {
            _save = save;
            _content = content;
            _inventory = inventory;
            _crafting = crafting;
            _builds = builds;
            _economy = economy;
            _reputation = reputation;
        }

        /// <summary>Motivo pelo qual a operacao parou. Nulo enquanto ela roda.</summary>
        public string StoppedReason { get; private set; }

        public bool IsStopped { get { return StoppedReason != null; } }

        public void Resume()
        {
            StoppedReason = null;
            _consecutiveFailures = 0;
        }

        /// <summary>
        /// Roda as regras sobre o resultado de uma corrida. Devolve o que foi feito, para
        /// o log - e o log que faz a automacao parecer viva em vez de silenciosa.
        /// </summary>
        public List<AutomationAction> AfterRace(CarInstance car, RaceResult result,
                                                AutomationRules rules, TrackDef track,
                                                RaceConditions conditions)
        {
            var actions = new List<AutomationAction>();
            if (car == null || result == null) return actions;

            // --- coleta de loot ---------------------------------------------------
            if (result.Rewards != null)
            {
                for (int i = 0; i < result.Rewards.Drops.Length; i++)
                {
                    PartInstance part = _inventory.Add(result.Rewards.Drops[i]);
                    if (part == null)
                    {
                        Stop("Inventario lotado", actions);
                        break;
                    }
                }
            }

            // --- 1. reparo ---------------------------------------------------------
            if (rules.AutoRepair && car.Damage >= rules.AutoRepairThreshold
                && _reputation.HasAutomation("auto_repair"))
            {
                long cost = _crafting.RepairCost(car, _save.Progress.TierIndex);
                if (_crafting.Repair(car, _save.Progress.TierIndex))
                    actions.Add(Log("Reparo automatico: -" + cost + " de cash"));
            }

            // --- 2. desmonte -------------------------------------------------------
            if ((rules.AutoSalvageCommon || rules.AutoSalvageUncommon)
                && _reputation.HasAutomation("auto_salvage"))
            {
                long scrap = _inventory.AutoSalvage(
                    rules.AutoSalvageCommon, rules.AutoSalvageUncommon, car.EquippedUids());

                if (scrap > 0L)
                {
                    _economy.AddScrap(scrap);
                    actions.Add(Log("Auto-desmontar: +" + scrap + " de scrap"));
                }
            }

            // --- 3. equipar a peca melhor -------------------------------------------
            if (rules.AutoEquipBetterPart && _reputation.HasAutomation("auto_equip"))
                AutoEquip(car, rules, track, conditions, actions);

            // --- 4. parada ----------------------------------------------------------
            if (result.Failures > 0) _consecutiveFailures++;
            else _consecutiveFailures = 0;

            if (rules.StopAfterConsecutiveFails > 0
                && _consecutiveFailures >= rules.StopAfterConsecutiveFails)
                Stop(_consecutiveFailures + " falhas consecutivas", actions);

            if (car.Damage > rules.StopIfDamageAbove)
                Stop("Dano passou de " + (int)rules.StopIfDamageAbove, actions);

            return actions;
        }

        /// <summary>
        /// Avalia cada slot e troca quando a peca candidata bate a equipada segundo o
        /// objetivo escolhido.
        ///
        /// O custo e real - N corridas simuladas por slot - entao a avaliacao acontece
        /// depois da corrida, uma vez, e nao a cada peca que cai.
        /// </summary>
        private void AutoEquip(CarInstance car, AutomationRules rules, TrackDef track,
                               RaceConditions conditions, List<AutomationAction> actions)
        {
            var slots = (PartSlot[])System.Enum.GetValues(typeof(PartSlot));

            for (int i = 0; i < slots.Length; i++)
            {
                PartInstance best = _builds.BestFor(car, _inventory, slots[i], track, conditions,
                                                    rules.EquipObjective, _save.Progress.Stage);
                if (best == null) continue;

                car.Equip(best);
                actions.Add(Log("Equipou " + best.DisplayName + " (" + best.Rarity + ")"));
            }
        }

        /// <summary>
        /// A decisao que o jogador ausente delegou: aceitar ou ignorar um evento/rival.
        ///
        /// Se nao houvesse resposta padrao aqui, o evento raro de 0,2% so contaria com o
        /// jogador olhando - e o jogo puniria o modo de uso principal (D-06).
        /// </summary>
        public bool AcceptEvent(AutomationRules rules, RiskBand band)
        {
            if (!rules.AcceptEvents) return false;
            if (!_reputation.HasAutomation("auto_event")) return false;

            // "Aceitar eventos com risco <= MEDIO" (GDD 16.2).
            return band <= RiskBand.Medium;
        }

        public bool AcceptRival(AutomationRules rules)
        {
            return rules.AcceptRivalChallenges && _reputation.HasAutomation("auto_event");
        }

        /// <summary>
        /// A pista atende ao piso de recompensa? "Recompensa min. x1,5" no painel.
        /// Uma pista abaixo do piso simplesmente nao e corrida pela automacao.
        /// </summary>
        public bool MeetsRewardFloor(AutomationRules rules, float rewardMultiplier)
        {
            return rewardMultiplier >= rules.MinimumRewardMultiplier;
        }

        /// <summary>Troca para a build de chuva quando o clima virar. GDD 16.2.</summary>
        public bool ShouldSwapToRainBuild(AutomationRules rules, Weather weather)
        {
            if (!rules.SwapToRainBuild) return false;
            return weather == Weather.Rain || weather == Weather.HeavyRain;
        }

        private void Stop(string reason, List<AutomationAction> actions)
        {
            if (IsStopped) return;
            StoppedReason = reason;
            actions.Add(Log("A operacao parou: " + reason.ToLowerInvariant() + "."));
        }

        private static AutomationAction Log(string text)
        {
            var action = new AutomationAction { Text = text };
            EventBus.Publish(action);
            return action;
        }
    }
}
