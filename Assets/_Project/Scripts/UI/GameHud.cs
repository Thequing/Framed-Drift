// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 18.1, 18.2, 3.4
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Progression;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>As abas do rodape do Modo Completo. GDD 18.1.</summary>
    public enum HudTab { Corrida, Garagem, Mapa, Automacao, Missoes }

    /// <summary>
    /// A moldura do Modo Completo (GDD 18.1): barra superior, area central e as abas.
    ///
    /// A barra superior existe por causa do pilar P2 - leitura em tres segundos. Cash,
    /// reputacao, relogio e estado da operacao ficam SEMPRE visiveis, em qualquer aba,
    /// porque sao o que o jogador vem conferir numa sessao de relance (GDD 2).
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private GarageUI _garage;
        [SerializeField] private MapUI _map;
        [SerializeField] private AutomationUI _automation;
        [SerializeField] private ReturnUI _returnScreen;

        [SerializeField] private HudTab _tab = HudTab.Corrida;

        private readonly List<string> _toasts = new List<string>();
        private readonly List<float> _toastTimes = new List<float>();
        private Vector2 _missionScroll;

        private const float TopBarHeight = 42f;
        private const float TabBarHeight = 36f;

        private void OnEnable()
        {
            EventBus.Subscribe<UnlockGranted>(OnUnlock);
            EventBus.Subscribe<RaceFinished>(OnRaceFinished);
            EventBus.Subscribe<BlueprintCompleted>(OnBlueprint);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnlockGranted>(OnUnlock);
            EventBus.Unsubscribe<RaceFinished>(OnRaceFinished);
            EventBus.Unsubscribe<BlueprintCompleted>(OnBlueprint);
        }

        private void OnUnlock(UnlockGranted evt)
        {
            Toast(evt.Kind.ToUpperInvariant() + ": " + evt.DisplayName);
        }

        private void OnBlueprint(BlueprintCompleted evt)
        {
            Toast("PLANTA COMPLETA: " + evt.DisplayName.ToUpperInvariant());
        }

        private void OnRaceFinished(RaceFinished evt)
        {
            // As duas fontes de recompensa lado a lado. E a tensao central da secao 3.5,
            // e ela precisa ser comunicada EXPLICITAMENTE na UI de resultado - senao o
            // jogador nunca descobre que terminar em quarto pode pagar mais.
            long positionCash = evt.Race.TrackBaseCash;
            long scoreCash = evt.Rewards.Cash - positionCash;

            Toast("P" + evt.Result.Position + "  posicao +" + UiSkin.Number(positionCash)
                  + "   drift +" + UiSkin.Number(scoreCash < 0 ? 0 : scoreCash));

            if (evt.ScoreFromPresence > 0)
                Toast("presenca +" + UiSkin.Number(evt.ScoreFromPresence) + " de score");
        }

        private void Toast(string text)
        {
            _toasts.Add(text);
            _toastTimes.Add(Time.time);
            if (_toasts.Count <= 5) return;

            _toasts.RemoveAt(0);
            _toastTimes.RemoveAt(0);
        }

        private void Update()
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (Time.time - _toastTimes[i] <= 5f) continue;
                _toasts.RemoveAt(i);
                _toastTimes.RemoveAt(i);
            }
        }

        private void OnGUI()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            // A tela de retorno e modal: nada mais e interagivel enquanto ela estiver la.
            if (_returnScreen != null && _returnScreen.IsShowing) return;

            DrawTopBar(game);
            DrawTabBar();
            DrawToasts();

            var content = new Rect(16f, TopBarHeight + 12f, Screen.width - 32f,
                                   Screen.height - TopBarHeight - TabBarHeight - 28f);

            switch (_tab)
            {
                case HudTab.Garagem: if (_garage != null) _garage.Draw(content); break;
                case HudTab.Mapa: if (_map != null) _map.Draw(content); break;
                case HudTab.Automacao: if (_automation != null) _automation.Draw(content); break;
                case HudTab.Missoes: DrawMissions(game, content); break;
            }
        }

        private void DrawTopBar(GameManager game)
        {
            var bar = new Rect(0f, 0f, Screen.width, TopBarHeight);
            UiSkin.Fill(bar, UiSkin.Background);

            GUILayout.BeginArea(new Rect(16f, 10f, Screen.width - 32f, TopBarHeight));
            GUILayout.BeginHorizontal();

            GUILayout.Label("FRAMED DRIFT", UiSkin.Title, GUILayout.Width(170f));
            GUILayout.Label("REP " + game.Save.Reputation, UiSkin.Label, GUILayout.Width(80f));
            GUILayout.Label("CASH " + UiSkin.Number(game.Save.Cash), UiSkin.Label, GUILayout.Width(150f));
            GUILayout.Label("SCRAP " + UiSkin.Number(game.Save.Scrap), UiSkin.Label, GUILayout.Width(130f));
            GUILayout.Label(game.Clock.ClockLabel + " " + MapUI.PeriodName(game.Clock.Period),
                            UiSkin.Label, GUILayout.Width(110f));

            GUILayout.FlexibleSpace();

            if (game.Rules.IsStopped)
            {
                GUI.color = UiSkin.Negative;
                GUILayout.Label("OPERACAO PARADA", UiSkin.Label, GUILayout.Width(160f));
                GUI.color = Color.white;
            }
            else if (game.IsRacing)
            {
                GUILayout.Label(game.ActiveTrack().DisplayName, UiSkin.Mono, GUILayout.Width(240f));
            }
            else if (GUILayout.Button("INICIAR CORRIDA", GUILayout.Width(170f)))
            {
                game.StartRace();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawTabBar()
        {
            var bar = new Rect(0f, Screen.height - TabBarHeight, Screen.width, TabBarHeight);
            UiSkin.Fill(bar, UiSkin.Background);

            GUILayout.BeginArea(new Rect(16f, Screen.height - TabBarHeight + 5f, Screen.width - 32f, TabBarHeight));
            GUILayout.BeginHorizontal();

            var tabs = (HudTab[])System.Enum.GetValues(typeof(HudTab));
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.color = _tab == tabs[i] ? UiSkin.Accent : Color.white;
                if (GUILayout.Button(tabs[i].ToString().ToUpperInvariant(), GUILayout.Width(130f), GUILayout.Height(26f)))
                    _tab = tabs[i];
                GUI.color = Color.white;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawToasts()
        {
            float y = TopBarHeight + 8f;
            for (int i = 0; i < _toasts.Count; i++)
            {
                float age = (Time.time - _toastTimes[i]) / 5f;
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 1f - age * age);
                GUI.Label(new Rect(Screen.width - 420f, y, 400f, 20f), _toasts[i], UiSkin.Label);
                GUI.color = previous;
                y += 20f;
            }
        }

        private void DrawMissions(GameManager game, Rect area)
        {
            GUILayout.BeginArea(area);
            GUILayout.Label("MISSOES", UiSkin.Title);
            GUILayout.Label("Toda missao e cumprivel passivamente pela automacao (GDD 14.7).", UiSkin.Mono);
            GUILayout.Space(8f);

            _missionScroll = GUILayout.BeginScrollView(_missionScroll);

            MissionScope scope = (MissionScope)(-1);
            for (int i = 0; i < game.Missions.Missions.Count; i++)
            {
                Mission mission = game.Missions.Missions[i];

                if (mission.Scope != scope)
                {
                    scope = mission.Scope;
                    GUILayout.Space(6f);
                    GUILayout.Label(ScopeName(scope), UiSkin.Title);
                }

                GUILayout.BeginHorizontal();
                Rect row = GUILayoutUtility.GetRect(430f, 22f);
                UiSkin.Bar(row, "", mission.Fraction,
                           mission.Progress + "/" + mission.Target,
                           mission.Complete ? UiSkin.Positive : UiSkin.Accent);

                GUI.Label(new Rect(row.x, row.y, 118f, row.height), mission.Description, UiSkin.Label);

                GUI.enabled = mission.Complete && !mission.Claimed;
                if (GUILayout.Button(mission.Claimed ? "COLETADA" : "COLETAR", GUILayout.Width(110f)))
                {
                    game.Missions.Claim(mission, game.Economy);
                    game.SaveNow();
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUILayout.Label("      +" + UiSkin.Number(mission.RewardCash)
                                + " cash, +" + mission.RewardReputation + " rep", UiSkin.Mono);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string ScopeName(MissionScope scope)
        {
            switch (scope)
            {
                case MissionScope.Daily: return "DIARIAS";
                case MissionScope.Weekly: return "SEMANAIS";
                default: return "VITALICIAS";
            }
        }
    }
}
