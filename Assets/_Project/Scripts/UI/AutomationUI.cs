// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 16.1, 16.2, 16.3, 16.4
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Automation;
using FramedDrift.Garage;
using FramedDrift.Progression;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// O painel de regras da secao 16.2.
    ///
    /// A automacao E a progressao de meta-jogo: o jogador comeca apertando "correr" e
    /// termina operando uma organizacao (GDD 16). Por isso cada linha aqui aparece
    /// DESABILITADA com o seu limiar de reputacao visivel, em vez de simplesmente nao
    /// existir - ver o degrau seguinte e o que faz o degrau atual valer a pena.
    /// </summary>
    public sealed class AutomationUI : MonoBehaviour
    {
        public void Draw(Rect area)
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            AutomationRules rules = game.Automation;

            GUILayout.BeginArea(area);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(430f));
            GUILayout.Label("AUTOMACAO", UiSkin.Title);

            rules.AutoStartRace = Rule(game, "auto_race", "Iniciar corrida automaticamente", rules.AutoStartRace);
            rules.AutoRepair = Rule(game, "auto_repair", "Reparar quando dano > "
                                    + (int)rules.AutoRepairThreshold, rules.AutoRepair);

            if (game.Reputation.HasAutomation("auto_repair"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(24f);
                GUILayout.Label("Limite de dano", UiSkin.Mono, GUILayout.Width(120f));
                rules.AutoRepairThreshold = Mathf.Round(
                    GUILayout.HorizontalSlider(rules.AutoRepairThreshold, 10f, 90f, GUILayout.Width(160f)));
                GUILayout.EndHorizontal();
            }

            rules.AutoSalvageCommon = Rule(game, "auto_salvage", "Desmontar Common", rules.AutoSalvageCommon);
            rules.AutoSalvageUncommon = Rule(game, "auto_salvage", "Desmontar Uncommon", rules.AutoSalvageUncommon);
            rules.AutoEquipBetterPart = Rule(game, "auto_equip", "Equipar peca melhor", rules.AutoEquipBetterPart);

            if (game.Reputation.HasAutomation("auto_equip")) DrawObjective(rules);

            rules.SwapToRainBuild = Rule(game, "auto_build", "Trocar para build de chuva quando chover",
                                         rules.SwapToRainBuild);
            rules.AcceptEvents = Rule(game, "auto_event", "Aceitar eventos com risco <= MEDIO", rules.AcceptEvents);
            rules.AcceptRivalChallenges = Rule(game, "auto_event", "Aceitar desafios de rival",
                                               rules.AcceptRivalChallenges);

            GUILayout.Space(10f);
            GUILayout.Label("PARAR SE", UiSkin.Title);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Dano acima de", UiSkin.Label, GUILayout.Width(130f));
            rules.StopIfDamageAbove = Mathf.Round(
                GUILayout.HorizontalSlider(rules.StopIfDamageAbove, 20f, 100f, GUILayout.Width(160f)));
            GUILayout.Label(rules.StopIfDamageAbove.ToString("0"), UiSkin.Mono, GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Falhas consecutivas", UiSkin.Label, GUILayout.Width(130f));
            rules.StopAfterConsecutiveFails = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(rules.StopAfterConsecutiveFails, 0f, 10f, GUILayout.Width(160f)));
            GUILayout.Label(rules.StopAfterConsecutiveFails == 0
                            ? "nunca" : rules.StopAfterConsecutiveFails.ToString(),
                            UiSkin.Mono, GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            if (game.Rules.IsStopped)
            {
                GUILayout.Space(8f);
                GUI.color = UiSkin.Negative;
                GUILayout.Label("A operacao parou: " + game.Rules.StoppedReason, UiSkin.Label);
                GUI.color = Color.white;
                if (GUILayout.Button("RETOMAR", GUILayout.Width(140f))) game.Rules.Resume();
            }

            GUILayout.EndVertical();

            GUILayout.Space(24f);
            DrawLadder(game);
            GUILayout.Space(24f);
            DrawFleet(game);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Uma linha do painel. Trancada, mostra o limiar - "cada degrau e conquistado
        /// DEPOIS que o jogador sentiu o atrito de fazer aquilo a mao" (GDD 16.1).
        /// </summary>
        private static bool Rule(GameManager game, string unlockId, string label, bool value)
        {
            bool unlocked = game.Reputation.HasAutomation(unlockId);

            GUILayout.BeginHorizontal();
            GUI.enabled = unlocked;
            bool result = GUILayout.Toggle(value && unlocked, "  " + label, UiSkin.Label);
            GUI.enabled = true;

            if (!unlocked)
            {
                int threshold = ThresholdOf(unlockId);
                GUI.color = UiSkin.Dim;
                GUILayout.Label("rep " + threshold, UiSkin.Mono, GUILayout.Width(60f));
                GUI.color = Color.white;
            }

            GUILayout.EndHorizontal();
            return unlocked ? result : value;
        }

        private static void DrawObjective(AutomationRules rules)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(24f);
            GUILayout.Label("Criterio", UiSkin.Mono, GUILayout.Width(60f));

            var objectives = (EquipObjective[])System.Enum.GetValues(typeof(EquipObjective));
            for (int i = 0; i < objectives.Length; i++)
            {
                GUI.color = rules.EquipObjective == objectives[i] ? UiSkin.Accent : Color.white;
                if (GUILayout.Button(ObjectiveName(objectives[i]), GUILayout.Width(78f)))
                    rules.EquipObjective = objectives[i];
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawLadder(GameManager game)
        {
            GUILayout.BeginVertical(GUILayout.Width(260f));
            GUILayout.Label("ESCADA", UiSkin.Title);
            GUILayout.Label("Reputacao " + game.Save.Reputation
                            + "  ->  proximo em " + game.Reputation.NextThreshold(), UiSkin.Mono);
            GUILayout.Space(6f);

            for (int i = 0; i < ReputationSystem.AutomationLadder.Length; i++)
            {
                string id = ReputationSystem.AutomationLadder[i];
                bool has = game.Reputation.HasAutomation(id);

                GUI.color = has ? UiSkin.Positive : UiSkin.Dim;
                GUILayout.Label((has ? "[x] " : "[ ] ") + ReputationSystem.AutomationLabel(id)
                                + "   (rep " + ReputationSystem.AutomationThresholds[i] + ")", UiSkin.Label);
                GUI.color = Color.white;
            }

            GUILayout.EndVertical();
        }

        /// <summary>A frota da secao 16.4: uma linha por vaga, carro -> pista -> log.</summary>
        private static void DrawFleet(GameManager game)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("FROTA", UiSkin.Title);

            for (int i = 0; i < game.Fleet.SlotCount; i++)
            {
                FleetEntry entry = game.Fleet.Entries[i];

                GUILayout.BeginHorizontal();
                GUILayout.Label("CARRO " + (i + 1).ToString("00"), UiSkin.Label, GUILayout.Width(80f));

                if (entry.IsEmpty)
                {
                    GUILayout.Label("- vazia -", UiSkin.Mono);
                }
                else
                {
                    GUILayout.Label(entry.Car.DisplayName, UiSkin.Label, GUILayout.Width(120f));
                    GUILayout.Label(string.IsNullOrEmpty(entry.Saved.TrackId)
                                    ? "sem pista"
                                    : game.Content.Track(entry.Saved.TrackId).DisplayName,
                                    UiSkin.Mono, GUILayout.Width(190f));
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            long cost = game.Fleet.NextSlotCost();
            GUI.enabled = game.Economy.CanAfford(cost) && game.Fleet.SlotCount < FleetManager.MaxSlots;
            if (GUILayout.Button("COMPRAR VAGA (" + UiSkin.Number(cost) + ")", GUILayout.Width(230f)))
            {
                game.Fleet.BuySlot(game.Economy);
                game.SaveNow();
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        private static int ThresholdOf(string unlockId)
        {
            for (int i = 0; i < ReputationSystem.AutomationLadder.Length; i++)
                if (ReputationSystem.AutomationLadder[i] == unlockId)
                    return ReputationSystem.AutomationThresholds[i];
            return 0;
        }

        private static string ObjectiveName(EquipObjective objective)
        {
            switch (objective)
            {
                case EquipObjective.DriftScore: return "Score";
                case EquipObjective.CashPerHour: return "Cash/h";
                case EquipObjective.LapTime: return "Tempo";
                case EquipObjective.Reliability: return "Confiab.";
                default: return "Build";
            }
        }
    }
}
