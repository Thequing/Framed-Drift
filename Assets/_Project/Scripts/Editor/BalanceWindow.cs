// -----------------------------------------------------------------------------
//  Framed Drift  -  Editor
//  GDD 0.2  secao 21.1
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using FramedDrift.Data;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using UnityEditor;
using UnityEngine;

namespace FramedDrift.EditorTools
{
    /// <summary>
    /// A ferramenta de balanceamento da secao 21.1.
    ///
    /// "Sem essa ferramenta, balancear este jogo a mao e inviavel. Ela e a peca de
    /// tooling de maior retorno do projeto inteiro e deve vir antes de qualquer
    /// conteudo."
    ///
    /// Roda N corridas com uma configuracao e reporta distribuicao de tempo, score,
    /// posicao, cash/hora, taxa de falha e drops por hora. O calculo NAO mora aqui: e o
    /// mesmo <see cref="BalanceReport"/> que os testes automatizados da 21.2 usam. Se
    /// fossem dois codigos, o numero que o designer ve e o que o teste verifica poderiam
    /// divergir - e o teste serviria para nada.
    /// </summary>
    public sealed class BalanceWindow : EditorWindow
    {
        private ContentDatabase _content;
        private RaceResolver _resolver;
        private LoadoutResolver _loadouts;
        private RaceFactory _factory;

        private int _carIndex;
        private int _trackIndex;
        private int _styleIndex = 1;
        private int _timeIndex;
        private int _weatherIndex;
        private int _races = 2000;
        private int _stage = 1;

        private float _tuneLock = 0.5f;
        private float _tuneAccel = 0.5f;
        private float _tuneDecel = 0.5f;

        private readonly List<BalanceReport> _history = new List<BalanceReport>();
        private BalanceReport _latest;
        private string _status = "Carregue o conteudo para comecar.";
        private Vector2 _scroll;

        [MenuItem("Framed Drift/Ferramenta de Balanceamento %#b")]
        public static void Open()
        {
            GetWindow<BalanceWindow>("Balanceamento").minSize = new Vector2(760f, 560f);
        }

        private void OnEnable()
        {
            TryLoadContent();
        }

        private void TryLoadContent()
        {
            try
            {
                _content = GameContent.Reload();
                _resolver = new RaceResolver(_content);
                _loadouts = new LoadoutResolver(_content);
                _factory = new RaceFactory(_content);
                _status = "Conteudo carregado: " + _content.CarList.Count + " carros, "
                          + _content.TrackList.Count + " pistas, " + _content.PartList.Count + " pecas.";
            }
            catch (System.Exception e)
            {
                _content = null;
                _status = "FALHA: " + e.Message;
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawToolbar();
            EditorGUILayout.HelpBox(_status, _content == null ? MessageType.Error : MessageType.Info);

            if (_content == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawConfiguration();
            EditorGUILayout.Space(8f);
            DrawActions();
            EditorGUILayout.Space(8f);
            DrawResults();
            EditorGUILayout.Space(8f);
            DrawExitCriteria();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Recarregar sem recompilar e requisito da secao 20.2 - editar o JSON e ver o
            // efeito na hora e o ciclo que torna o balanceamento viavel.
            if (GUILayout.Button("Recarregar conteudo", EditorStyles.toolbarButton, GUILayout.Width(150f)))
                TryLoadContent();

            if (GUILayout.Button("Limpar historico", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                _history.Clear();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("CONFIGURACAO", EditorStyles.boldLabel);

            _carIndex = EditorGUILayout.Popup("Carro", _carIndex, Names(_content.CarList.ConvertAll(c => c.DisplayName)));
            _trackIndex = EditorGUILayout.Popup("Pista", _trackIndex, Names(_content.TrackList.ConvertAll(t => t.DisplayName)));
            _styleIndex = EditorGUILayout.Popup("Estilo", _styleIndex, System.Enum.GetNames(typeof(DriftStyle)));
            _timeIndex = EditorGUILayout.Popup("Horario", _timeIndex, System.Enum.GetNames(typeof(TimeOfDay)));
            _weatherIndex = EditorGUILayout.Popup("Clima", _weatherIndex, System.Enum.GetNames(typeof(Weather)));

            _stage = EditorGUILayout.IntSlider("Stage", _stage, 1, _content.Balance.StagesPerTier);
            _races = EditorGUILayout.IntField("Corridas", _races);

            EditorGUILayout.LabelField("Ajuste fino do diferencial (GDD 9.2)", EditorStyles.miniBoldLabel);
            _tuneLock = EditorGUILayout.Slider("Trava", _tuneLock, 0f, 1f);
            _tuneAccel = EditorGUILayout.Slider("Aceleracao", _tuneAccel, 0f, 1f);
            _tuneDecel = EditorGUILayout.Slider("Desaceleracao", _tuneDecel, 0f, 1f);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("RODAR", GUILayout.Height(28f))) Run();

            if (GUILayout.Button("RODAR 10.000 (criterio da Fase 3)", GUILayout.Height(28f)))
            {
                _races = 10000;
                Run();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void Run()
        {
            CarDef car = _content.CarList[Mathf.Clamp(_carIndex, 0, _content.CarList.Count - 1)];
            TrackDef track = _content.TrackList[Mathf.Clamp(_trackIndex, 0, _content.TrackList.Count - 1)];

            var tuning = new TuningSetup { Lock = _tuneLock, Accel = _tuneAccel, Decel = _tuneDecel };
            CarLoadout loadout = _loadouts.Resolve(car, FactoryBuild(car), tuning, 0f);

            var conditions = new RaceConditions
            {
                Weather = (Weather)_weatherIndex,
                TimeOfDay = (TimeOfDay)_timeIndex,
                Traffic = TrafficDensity.Medium,
            };

            RaceInstance race = _factory.Build(track, loadout, conditions, (DriftStyle)_styleIndex,
                                               _stage, 1f, 1UL);

            var stopwatch = Stopwatch.StartNew();
            string label = car.DisplayName + " / " + track.DisplayName + " / "
                           + (DriftStyle)_styleIndex + " / " + (TimeOfDay)_timeIndex;

            _latest = BalanceReport.Run(_resolver, race, Mathf.Max(1, _races), 0xBA1A0CEUL, label);
            stopwatch.Stop();

            _history.Insert(0, _latest);
            if (_history.Count > 12) _history.RemoveAt(_history.Count - 1);

            _status = _races + " corridas em " + stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " s"
                      + "   (" + (stopwatch.Elapsed.TotalMilliseconds / _races).ToString("0.0000") + " ms/corrida)";
        }

        /// <summary>Build de fabrica: base artesanal, zero afixos - a linha de base honesta.</summary>
        private RolledPart[] FactoryBuild(CarDef car)
        {
            string[] stock =
            {
                "eng_stock", "tur_stock", "trn_stock", "dif_street",
                "sus_stock", "tir_street", "brk_stock", "aer_stock",
            };

            var parts = new RolledPart[8];
            for (int i = 0; i < stock.Length; i++) parts[i] = _resolver.Loot.Factory(stock[i]);
            return parts;
        }

        private void DrawResults()
        {
            if (_latest == null) return;

            EditorGUILayout.LabelField("RESULTADO", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_latest.ToString(), GUILayout.MinHeight(240f));

            if (_history.Count <= 1) return;

            EditorGUILayout.LabelField("HISTORICO (comparacao lado a lado)", EditorStyles.boldLabel);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(BalanceReport.LineHeader());
            for (int i = 0; i < _history.Count; i++) sb.AppendLine(_history[i].ToLine());

            EditorGUILayout.TextArea(sb.ToString(), GUILayout.MinHeight(140f));
        }

        /// <summary>
        /// Os criterios de saida que dependem destes numeros, verificados na hora.
        ///
        /// Deixa-los na tela e o que impede a ferramenta de virar um painel bonito que
        /// ninguem confronta com o contrato da GDD.
        /// </summary>
        private void DrawExitCriteria()
        {
            if (_latest == null) return;

            EditorGUILayout.LabelField("CRITERIOS DA GDD", EditorStyles.boldLabel);
            BalanceSettings b = _content.Balance;

            Criterion("6.3  carro inicial / pista inicial: score 6.000-12.000",
                      _latest.Score.Mean >= 6000 && _latest.Score.Mean <= 12000,
                      _latest.Score.Mean.ToString("N0"));

            Criterion("21.2 carro inicial vence 55-75%",
                      _latest.WinRate >= 0.55 && _latest.WinRate <= 0.75,
                      (_latest.WinRate * 100).ToString("0.0") + "%");

            Criterion("3.6 / 6.2  uplift de presenca em [" + b.PresenceUpliftMin + " - " + b.PresenceUpliftMax + "]",
                      _latest.Uplift >= b.PresenceUpliftMin && _latest.Uplift <= b.PresenceUpliftMax,
                      (_latest.Uplift * 100).ToString("0.00") + "%");

            Criterion("21.2 falha em risco MEDIO entre 3% e 6%",
                      b.Band((float)_latest.Risk.Mean) != RiskBand.Medium
                      || (_latest.FailureRate >= 0.03 && _latest.FailureRate <= 0.06),
                      (_latest.FailureRate * 100).ToString("0.00") + "%  (risco "
                      + _latest.Risk.Mean.ToString("0") + ")");

            Criterion("R2  variancia minima: score varia entre corridas",
                      _latest.Score.StdDev > _latest.Score.Mean * 0.05,
                      "sigma " + _latest.Score.StdDev.ToString("N0"));
        }

        private static void Criterion(string label, bool pass, string value)
        {
            EditorGUILayout.BeginHorizontal();

            Color previous = GUI.color;
            GUI.color = pass ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.45f);
            EditorGUILayout.LabelField(pass ? "OK  " : "FALHA", GUILayout.Width(50f));
            GUI.color = previous;

            EditorGUILayout.LabelField(label, GUILayout.Width(420f));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }

        private static string[] Names(List<string> source)
        {
            return source.ToArray();
        }
    }
}
