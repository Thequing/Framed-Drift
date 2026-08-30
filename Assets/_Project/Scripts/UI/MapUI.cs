// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 11.1, 11.4, 11.6, 12.1
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// A terceira pergunta da secao 3.4: onde eu mando esse carro correr?
    ///
    /// Cada pista mostra os quatro numeros que fazem a escolha ser uma escolha:
    /// dificuldade, RISCO, recompensa e chance de drop. Risco alto aumenta recompensa,
    /// drop raro e chance de falha - NESTA ordem de destaque (GDD 12.1), porque e nesta
    /// ordem que o jogador precisa entende-los.
    ///
    /// Mostra tambem a previsao do tempo: o clima da proxima corrida e conhecido com
    /// antecedencia (GDD 11.5), e e isso que permite trocar para a build de chuva antes
    /// da largada - ou deixar a automacao fazer isso.
    /// </summary>
    public sealed class MapUI : MonoBehaviour
    {
        private Vector2 _scroll;

        public void Draw(Rect area)
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            GUILayout.BeginArea(area);
            GUILayout.Label("MAPA - " + game.Content.Region(game.Save.Progress.RegionId).DisplayName.ToUpperInvariant(),
                            UiSkin.Title);

            DrawForecast(game);
            GUILayout.Space(8f);

            _scroll = GUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < game.Content.TrackList.Count; i++)
                DrawTrackRow(game, game.Content.TrackList[i]);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawForecast(GameManager game)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Relogio " + game.Clock.ClockLabel
                            + "   (" + PeriodName(game.Clock.Period) + ")", UiSkin.Label, GUILayout.Width(220f));

            GUILayout.Label("Previsao: " + WeatherName(game.Forecast), UiSkin.Label, GUILayout.Width(200f));

            GUILayout.Label("Proximo periodo em "
                            + game.Clock.HoursUntilPeriodChange.ToString("0.0") + " h de jogo",
                            UiSkin.Mono);
            GUILayout.EndHorizontal();
        }

        private void DrawTrackRow(GameManager game, TrackDef track)
        {
            bool unlocked = game.Reputation.TrackUnlocked(track.Id);
            bool active = game.Save.Progress.TrackId == track.Id;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();

            GUI.color = active ? UiSkin.Accent : (unlocked ? Color.white : UiSkin.Dim);
            GUILayout.Label(track.DisplayName, UiSkin.Title, GUILayout.Width(300f));
            GUI.color = Color.white;

            if (!unlocked)
            {
                GUILayout.Label("Requer reputacao " + track.ReputationRequired, UiSkin.Mono);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            RaceInstance preview = Preview(game, track);
            float risk = game.Resolver.Simulator != null ? PreviewRisk(game, preview) : 0f;
            RiskBand band = game.Content.Balance.Band(risk);
            float reward = game.Resolver.RewardMultiplier(preview);

            GUI.color = UiSkin.RiskColor(band);
            GUILayout.Label("RISCO " + UiSkin.RiskLabel(band), UiSkin.Label, GUILayout.Width(120f));
            GUI.color = Color.white;

            GUILayout.Label("Recompensa x" + reward.ToString("0.00"), UiSkin.Label, GUILayout.Width(150f));
            GUILayout.Label("Tier " + track.Tier + " - " + track.ModuleIds.Length + " segmentos",
                            UiSkin.Mono, GUILayout.Width(180f));

            GUI.enabled = !active;
            if (GUILayout.Button("CORRER AQUI", GUILayout.Width(130f)))
            {
                game.Save.Progress.TrackId = track.Id;
                game.SaveNow();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static RaceInstance Preview(GameManager game, TrackDef track)
        {
            var conditions = new RaceConditions
            {
                Weather = game.Forecast,
                TimeOfDay = game.Clock.Period,
                Traffic = TrafficDensity.Medium,
            };

            return game.Factory.Build(track, game.ActiveCar().Loadout, conditions,
                                      game.ActiveCar().Style, game.Save.Progress.Stage,
                                      game.Save.Prestige.ScoreMultiplier, 1UL);
        }

        /// <summary>
        /// O risco mostrado e o MESMO que a corrida vai usar - calculado pelo solver, nao
        /// estimado pela UI. Um numero de vitrine que nao bate com o jogo e pior que
        /// nenhum numero.
        /// </summary>
        private static float PreviewRisk(GameManager game, RaceInstance race)
        {
            float difficultySum = 0f;
            for (int i = 0; i < race.Track.Length; i++) difficultySum += race.Track[i].Difficulty;
            float average = race.Track.Length == 0 ? 0f : difficultySum / race.Track.Length;

            var solver = new Simulation.SegmentSolver(game.Content.Balance);
            return solver.RiskIndex(race, average);
        }

        public static string PeriodName(TimeOfDay period)
        {
            return period == TimeOfDay.Night ? "Noite" : "Dia";
        }

        public static string WeatherName(Weather weather)
        {
            switch (weather)
            {
                case Weather.Clear: return "Seco";
                case Weather.Cloudy: return "Nublado";
                case Weather.Rain: return "Chuva";
                case Weather.HeavyRain: return "Chuva forte";
                case Weather.Fog: return "Neblina";
                default: return "Neve";
            }
        }
    }
}
