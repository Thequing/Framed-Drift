// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 17.3, 17.4, 17.5
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// A tela de retorno (GDD 17.4).
    ///
    /// Duas regras governam este arquivo:
    ///
    /// <b>1. Nunca esconder o teto (17.3).</b> Se a garagem parou por ter atingido o
    /// limite de horas, a tela diz isso com todas as letras e oferece o upgrade. Um
    /// numero menor sem explicacao ensina o jogador a desconfiar do sistema, que e
    /// exatamente o que D-02 foi escrita para impedir.
    ///
    /// <b>2. A voz fala DO CARRO, nao do jogador (17.5).</b> "Seu Kanto AE completou 412
    /// corridas" - nao "voce completou". A escolha e barata e e o principal vetor de
    /// apego; precisa ser aplicada consistentemente em todo texto de resultado.
    /// </summary>
    public sealed class ReturnUI : MonoBehaviour
    {
        private OfflineReport _report;
        private Vector2 _logScroll;
        private bool _showFullLog;

        public bool IsShowing { get { return _report != null; } }

        private void OnEnable()
        {
            EventBus.Subscribe<OfflineReportReady>(OnReport);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OfflineReportReady>(OnReport);
        }

        private void OnReport(OfflineReportReady evt)
        {
            _report = evt.Report;
            _showFullLog = false;
        }

        private void OnGUI()
        {
            if (_report == null) return;

            GameManager game = GameManager.Instance;
            if (game == null) return;

            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            UiSkin.Fill(full, new Color(0f, 0f, 0f, 0.82f));

            float width = Mathf.Min(720f, Screen.width - 80f);
            float height = Mathf.Min(560f, Screen.height - 80f);
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            UiSkin.Fill(panel, UiSkin.Panel);
            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, panel.height - 40f));

            GUILayout.Label("BEM-VINDO DE VOLTA", UiSkin.Big);
            DrawAbsenceLine();
            GUILayout.Space(10f);

            if (_showFullLog) DrawFullLog(game);
            else DrawSummary(game);

            GUILayout.FlexibleSpace();
            DrawButtons(game);

            GUILayout.EndArea();
        }

        /// <summary>A linha que nunca mente sobre o teto.</summary>
        private void DrawAbsenceLine()
        {
            string absent = Duration(_report.ElapsedSeconds);
            string operated = Duration(_report.OperatedSeconds);

            GUILayout.Label("Ausente por " + absent + "   (operacao: " + operated + ")", UiSkin.Label);

            if (_report.HitCap)
            {
                GUI.color = new Color(1f, 0.78f, 0.3f);
                GUILayout.Label("Sua garagem parou no teto de "
                                + Duration(_report.OperatedSeconds)
                                + ". Amplie o turno para operar mais tempo.", UiSkin.Label);
                GUI.color = Color.white;
            }

            if (!string.IsNullOrEmpty(_report.StoppedReason))
            {
                GUI.color = UiSkin.Negative;
                GUILayout.Label(_report.StoppedReason + "  (as " + Duration(_report.StoppedAtSeconds) + ")",
                                UiSkin.Label);
                GUI.color = Color.white;
            }
        }

        private void DrawSummary(GameManager game)
        {
            string carName = CarName(game);

            GUILayout.Label(carName + " completou " + _report.Races + " corridas.", UiSkin.Title);
            GUILayout.Space(6f);

            Row("Corridas", _report.Races + "      Vitorias " + _report.Wins + "    Derrotas " + _report.Losses);
            Row("Drift Score", "+" + UiSkin.Number(_report.DriftScore));
            Row("Cash", "+" + UiSkin.Number(_report.Cash));
            Row("Scrap", "+" + UiSkin.Number(_report.Scrap));
            Row("Reputacao", "+" + _report.Reputation);
            Row("Pecas", _report.TotalDrops + "   " + RarityBreakdown());
            Row("Falhas", _report.Failures + "      Dano " + _report.DamageTaken.ToString("0"));

            if (_report.RepairCost > 0L)
                Row("Reparo automatico", "-" + UiSkin.Number(_report.RepairCost));

            if (_report.Highlights.Count == 0) return;

            GUILayout.Space(10f);
            GUILayout.Label("DESTAQUES", UiSkin.Title);

            int shown = Mathf.Min(4, _report.Highlights.Count);
            for (int i = 0; i < shown; i++)
                GUILayout.Label("   " + _report.Highlights[i].Text, UiSkin.Label);
        }

        /// <summary>
        /// O log completo e uma linha do tempo navegavel. Vale muito mais que o numero
        /// agregado - e ele que cria a relacao com o carro (GDD 17.4).
        /// </summary>
        private void DrawFullLog(GameManager game)
        {
            GUILayout.Label("LOG COMPLETO", UiSkin.Title);
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(320f));

            string carName = CarName(game);
            for (int i = 0; i < _report.Highlights.Count; i++)
            {
                OfflineLogEntry entry = _report.Highlights[i];
                GUILayout.Label(Duration(entry.AtSeconds).PadRight(10) + carName + " encontrou " + entry.Text,
                                UiSkin.Label);
            }

            if (_report.Highlights.Count == 0)
                GUILayout.Label("Nada notavel aconteceu nesta ausencia.", UiSkin.Mono);

            GUILayout.EndScrollView();
        }

        private void DrawButtons(GameManager game)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("COLETAR", GUILayout.Width(160f), GUILayout.Height(34f)))
            {
                game.CollectOfflineReport();
                _report = null;
                return;
            }

            if (GUILayout.Button(_showFullLog ? "VOLTAR" : "VER LOG COMPLETO",
                                 GUILayout.Width(200f), GUILayout.Height(34f)))
                _showFullLog = !_showFullLog;

            GUILayout.EndHorizontal();
        }

        private string RarityBreakdown()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = _report.DropsByRarity.Length - 1; i >= 0; i--)
            {
                if (_report.DropsByRarity[i] <= 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(_report.DropsByRarity[i]).Append(' ').Append((Rarity)i);
            }
            return sb.ToString();
        }

        private static string CarName(GameManager game)
        {
            var car = game.ActiveCar();
            return car == null ? "Seu carro" : "Seu " + car.DisplayName;
        }

        private static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, UiSkin.Label, GUILayout.Width(170f));
            GUILayout.Label(value, UiSkin.Label);
            GUILayout.EndHorizontal();
        }

        private static string Duration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            return hours + "h " + minutes.ToString("00") + "m";
        }
    }
}
