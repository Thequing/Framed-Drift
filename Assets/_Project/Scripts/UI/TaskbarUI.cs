// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 18.2, 18.3, 3.6, D-07
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// Modo Compacto (~480x270) e um esboco do modo Taskbar (~360x48).
    ///
    /// <b>O Taskbar completo e Fase 15 (D-07).</b> O que existe aqui e o suficiente para
    /// MEDIR o orcamento da secao 20.4 desde a Fase 5 (risco R8) e para provar a
    /// afirmacao da secao 20.6: os tres modos consomem exatamente a MESMA Timeline, com
    /// outro renderizador. Nao ha simulacao alternativa em lugar nenhum deste arquivo.
    ///
    /// Uma regra de design ja vale aqui e nao pode ser afrouxada depois:
    ///
    ///   A Entrada Perfeita e os coletaveis CONTINUAM disponiveis no modo Compacto,
    ///   mas NAO EXISTEM no Taskbar. A 360x48 nao ha espaco para um alvo clicavel
    ///   honesto, e forca-lo criaria exatamente a pressao que a secao 3.6 proibe.
    /// </summary>
    public sealed class TaskbarUI : MonoBehaviour
    {
        [SerializeField] private bool _compactMode = true;

        private void OnGUI()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            if (_compactMode) DrawCompact(game);
            else DrawTaskbar(game);
        }

        /// <summary>
        /// Modo Compacto: sem 3D. HUD 2D com tracado esquematico, posicao do carro,
        /// score, combo, barra de progresso e proximo evento (GDD 18.2).
        /// </summary>
        private void DrawCompact(GameManager game)
        {
            var panel = new Rect(0f, 0f, Screen.width, Screen.height);
            UiSkin.Fill(panel, UiSkin.Background);

            if (game.CurrentResult == null)
            {
                GUI.Label(new Rect(16f, 16f, 400f, 24f), "Aguardando corrida...", UiSkin.Label);
                return;
            }

            var result = game.CurrentResult;
            float fraction = result.TotalTime <= 0f ? 0f : Mathf.Clamp01(game.PlaybackTime / result.TotalTime);

            GUI.Label(new Rect(16f, 12f, 460f, 34f),
                      "DRIFT " + UiSkin.Number(game.RunningScore), UiSkin.Big);

            int combo = CurrentCombo(game);
            GUI.Label(new Rect(16f, 56f, 240f, 22f),
                      "COMBO x" + combo + "     P" + result.Position, UiSkin.Title);

            DrawSchematic(game, new Rect(16f, 92f, Screen.width - 32f, 46f), fraction);

            GUI.Label(new Rect(16f, Screen.height - 34f, 460f, 22f),
                      game.ActiveTrack().DisplayName + "   "
                      + MapUI.WeatherName(game.CurrentRace.Conditions.Weather), UiSkin.Mono);
        }

        /// <summary>
        /// O tracado esquematico: uma barra onde cada CURVA COM DRIFT vira um marcador.
        ///
        /// E aqui que a Entrada Perfeita vive no modo Compacto - como um pulso no
        /// marcador da curva (GDD 18.2), e nao como um alvo no mundo 3D que nao existe.
        /// </summary>
        private void DrawSchematic(GameManager game, Rect rect, float fraction)
        {
            UiSkin.Fill(rect, UiSkin.BarTrack);

            var timeline = game.CurrentResult.Timeline;
            float total = game.CurrentResult.TotalTime;
            if (total <= 0f) return;

            for (int i = 0; i < timeline.Length; i++)
            {
                if (!timeline[i].Drift) continue;

                float at = timeline[i].TimeOffset / total;
                var marker = new Rect(rect.x + rect.width * at - 2f, rect.y + 6f, 4f, rect.height - 12f);

                Color color = UiSkin.Dim;
                if (i < game.CurrentSegment)
                {
                    switch (timeline[i].QualityFinal)
                    {
                        case DriftQuality.Perfect: color = UiSkin.Positive; break;
                        case DriftQuality.Bad: color = UiSkin.Negative; break;
                        default: color = UiSkin.Text; break;
                    }
                }
                else if (timeline[i].Promotable)
                {
                    // Pulsa a curva promovivel que esta chegando.
                    color = Color.Lerp(UiSkin.Dim, UiSkin.Accent, Mathf.PingPong(Time.time * 3f, 1f));
                }

                UiSkin.Fill(marker, color);
            }

            var head = new Rect(rect.x + rect.width * fraction - 3f, rect.y, 6f, rect.height);
            UiSkin.Fill(head, UiSkin.Accent);
        }

        /// <summary>
        /// O esboco da Fase 15 a 360x48: carro, traco, bandeira, score, combo, cash.
        /// Sem nenhum alvo clicavel - por decisao, nao por falta de tempo.
        /// </summary>
        private void DrawTaskbar(GameManager game)
        {
            var panel = new Rect(0f, 0f, Screen.width, Screen.height);
            UiSkin.Fill(panel, UiSkin.Background);

            float fraction = 0f;
            if (game.CurrentResult != null && game.CurrentResult.TotalTime > 0f)
                fraction = Mathf.Clamp01(game.PlaybackTime / game.CurrentResult.TotalTime);

            var track = new Rect(30f, Screen.height * 0.5f - 3f, Screen.width * 0.45f, 6f);
            UiSkin.Fill(track, UiSkin.BarTrack);
            UiSkin.Fill(new Rect(track.x, track.y, track.width * fraction, track.height), UiSkin.Accent);

            GUI.Label(new Rect(track.xMax + 12f, 6f, 120f, 22f),
                      UiSkin.Number(game.RunningScore), UiSkin.Title);
            GUI.Label(new Rect(track.xMax + 132f, 8f, 60f, 20f),
                      "x" + CurrentCombo(game), UiSkin.Label);
            GUI.Label(new Rect(Screen.width - 110f, 8f, 100f, 20f),
                      UiSkin.Number(game.Save.Cash), UiSkin.Label);
        }

        private static int CurrentCombo(GameManager game)
        {
            if (game.CurrentResult == null) return 0;
            var timeline = game.CurrentResult.Timeline;
            if (timeline.Length == 0) return 0;

            int i = Mathf.Clamp(game.CurrentSegment - 1, 0, timeline.Length - 1);
            return timeline[i].Combo;
        }
    }
}
