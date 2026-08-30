// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 6.4, 18.1, 3.6.1
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Racing;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>Um pop-up empilhado da secao 6.4.</summary>
    public struct ScorePopup
    {
        public string Label;
        public long Value;
        public float Born;
        public Color Color;
    }

    /// <summary>
    /// O HUD da corrida (GDD 18.1) e o feedback visual da 6.4.
    ///
    /// O contador principal sobe com TWEENING, nunca salta. Isso nao e polimento: e o
    /// principal elemento de espetaculo do jogo (pilar P5), e um numero que pisca de
    /// 8.000 para 12.000 nao e espetaculo, e um relatorio.
    ///
    /// O HUD tambem e uma das duas mitigacoes obrigatorias da camera da 19.2 - com o
    /// yaw encurtado, parte da pericia que a secao 5 pontua fica invisivel, e o HUD (com
    /// a fumaca) e o que devolve essa leitura. Nao e opcional.
    /// </summary>
    public sealed class RaceUI : MonoBehaviour
    {
        [SerializeField] private PerfectEntryController _perfectEntry;
        [SerializeField] private float _popupLifetime = 1.6f;
        [SerializeField] private float _scoreTweenRate = 6f;

        private readonly List<ScorePopup> _popups = new List<ScorePopup>();
        private double _displayedScore;
        private double _targetScore;

        private void OnEnable()
        {
            EventBus.Subscribe<SegmentPlayed>(OnSegment);
            EventBus.Subscribe<CurvePromoted>(OnPromoted);
            EventBus.Subscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SegmentPlayed>(OnSegment);
            EventBus.Unsubscribe<CurvePromoted>(OnPromoted);
            EventBus.Unsubscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnRaceStarted(RaceStarted evt)
        {
            _popups.Clear();
            _displayedScore = 0.0;
            _targetScore = 0.0;
        }

        private void OnSegment(SegmentPlayed evt)
        {
            if (!evt.Outcome.Drift || evt.Score <= 0.0) return;

            GameManager game = GameManager.Instance;
            _targetScore = game != null ? game.RunningScore : _targetScore + evt.Score;

            // Um pop-up por FONTE, empilhados - e assim que o jogador aprende de onde o
            // score vem sem abrir menu nenhum (GDD 6.4).
            string label = "CURVA";
            Color color = UiSkin.Text;

            if (evt.Outcome.QualityFinal == DriftQuality.Perfect) { label = "PERFECT"; color = UiSkin.Positive; }
            else if (evt.Outcome.QualityFinal == DriftQuality.Bad) { label = "BAD"; color = UiSkin.Negative; }
            else if (evt.Outcome.IsTransition) { label = "TRANSICAO"; color = new Color(0.5f, 0.8f, 1f); }
            else if (evt.Outcome.Proximity > 0.35f) { label = "PROXIMIDADE"; color = new Color(1f, 0.8f, 0.3f); }

            Push(label, (long)evt.Score, color);

            if (evt.Outcome.Collision) Push("COLISAO", 0, UiSkin.Negative);
            if (evt.Outcome.Failure) Push("FALHA", 0, UiSkin.Negative);
        }

        private void OnPromoted(CurvePromoted evt)
        {
            Push("ENTRADA PERFEITA", 0, UiSkin.Accent);
        }

        private void Push(string label, long value, Color color)
        {
            _popups.Add(new ScorePopup { Label = label, Value = value, Born = Time.time, Color = color });
            if (_popups.Count > 8) _popups.RemoveAt(0);
        }

        private void Update()
        {
            // Tweening exponencial: alcanca rapido e nunca ultrapassa.
            _displayedScore = Mathf.Lerp((float)_displayedScore, (float)_targetScore,
                                         1f - Mathf.Exp(-_scoreTweenRate * Time.deltaTime));

            for (int i = _popups.Count - 1; i >= 0; i--)
                if (Time.time - _popups[i].Born > _popupLifetime) _popups.RemoveAt(i);
        }

        private void OnGUI()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.CurrentResult == null) return;

            DrawMainCounter(game);
            DrawSecondaryLine(game);
            DrawProgress(game);
            DrawPopups();
            DrawInputWindows();
        }

        private void DrawMainCounter(GameManager game)
        {
            var rect = new Rect(28f, Screen.height * 0.52f, 460f, 52f);
            GUI.Label(rect, "DRIFT  " + UiSkin.Number(_displayedScore), UiSkin.Big);

            int combo = CurrentCombo(game);
            if (combo <= 0) return;

            var comboRect = new Rect(rect.x, rect.yMax - 6f, 300f, 26f);
            Color previous = GUI.color;
            GUI.color = Color.Lerp(UiSkin.Text, UiSkin.Accent, Mathf.Clamp01(combo / 25f));
            GUI.Label(comboRect, "COMBO x" + combo, UiSkin.Title);
            GUI.color = previous;
        }

        private void DrawSecondaryLine(GameManager game)
        {
            var result = game.CurrentResult;
            var rect = new Rect(28f, Screen.height * 0.52f + 78f, 620f, 22f);

            int perfect = 0, drifts = 0;
            for (int i = 0; i < game.CurrentSegment && i < result.Timeline.Length; i++)
            {
                if (!result.Timeline[i].Drift) continue;
                drifts++;
                if (result.Timeline[i].QualityFinal == DriftQuality.Perfect) perfect++;
            }

            float yaw = 0f;
            var visualizer = FindAnyObjectByType<RaceVisualizer>();
            if (visualizer != null) yaw = Mathf.Abs(visualizer.CurrentYaw);

            GUI.Label(rect,
                "ANGULO " + yaw.ToString("0") + "deg      "
                + "PERFECT " + perfect + "/" + drifts + "      "
                + "P" + result.Position + "/" + game.Content.Balance.GridSize,
                UiSkin.Label);
        }

        private void DrawProgress(GameManager game)
        {
            var result = game.CurrentResult;
            float fraction = result.TotalTime <= 0f
                ? 0f
                : Mathf.Clamp01(game.PlaybackTime / result.TotalTime);

            var rect = new Rect(28f, Screen.height * 0.52f + 104f, 460f, 14f);
            UiSkin.Fill(rect, UiSkin.BarTrack);
            UiSkin.Fill(new Rect(rect.x, rect.y, rect.width * fraction, rect.height), UiSkin.Accent);

            GUI.Label(new Rect(rect.xMax + 10f, rect.y - 4f, 90f, 22f),
                      (fraction * 100f).ToString("0") + "%", UiSkin.Mono);
        }

        /// <summary>Os pop-ups empilhados da secao 6.4, subindo e desaparecendo.</summary>
        private void DrawPopups()
        {
            float x = Screen.width - 300f;
            float y = Screen.height * 0.40f;

            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                ScorePopup popup = _popups[i];
                float age = (Time.time - popup.Born) / _popupLifetime;

                Color previous = GUI.color;
                GUI.color = new Color(popup.Color.r, popup.Color.g, popup.Color.b, 1f - age);

                string text = popup.Value > 0
                    ? popup.Label.PadRight(14) + "+" + UiSkin.Number(popup.Value)
                    : popup.Label;

                GUI.Label(new Rect(x, y - age * 24f, 280f, 22f), text, UiSkin.Title);
                GUI.color = previous;

                y += 26f;
            }
        }

        /// <summary>
        /// A janela de Entrada Perfeita, ancorada no centro-baixo da tela.
        ///
        /// Um anel que fecha. Nao ha texto de instrucao permanente: se a mecanica
        /// precisasse ser explicada toda vez, ela seria requisito e nao bonus (GDD 3.6).
        /// </summary>
        private void DrawInputWindows()
        {
            if (_perfectEntry == null) return;

            if (_perfectEntry.TelegraphedSegment >= 0 && _perfectEntry.Active.Count == 0)
            {
                var telegraph = new Rect(Screen.width * 0.5f - 90f, Screen.height * 0.74f, 180f, 20f);
                Color previous = GUI.color;
                GUI.color = UiSkin.Dim;
                GUI.Label(telegraph, "curva chegando", UiSkin.Label);
                GUI.color = previous;
            }

            for (int i = 0; i < _perfectEntry.Active.Count; i++)
            {
                ActiveWindow window = _perfectEntry.Active[i];
                float remaining = 1f - window.Progress;

                var track = new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.74f, 220f, 16f);
                UiSkin.Fill(track, UiSkin.BarTrack);
                UiSkin.Fill(new Rect(track.x, track.y, track.width * remaining, track.height), UiSkin.Accent);

                GUI.Label(new Rect(track.x, track.y - 24f, 260f, 22f), "ENTRADA PERFEITA", UiSkin.Title);
            }
        }

        private static int CurrentCombo(GameManager game)
        {
            var timeline = game.CurrentResult.Timeline;
            int i = Mathf.Clamp(game.CurrentSegment - 1, 0, timeline.Length - 1);
            return timeline.Length == 0 ? 0 : timeline[i].Combo;
        }
    }
}
