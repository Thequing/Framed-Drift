// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 3.6.1
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Simulation.Outcomes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FramedDrift.Racing
{
    /// <summary>Uma janela viva na tela, com o quanto dela ja passou.</summary>
    public struct ActiveWindow
    {
        public InputWindow Window;
        public float Progress;
    }

    /// <summary>
    /// A Entrada Perfeita (GDD 3.6.1).
    ///
    /// Na entrada de cada curva com drift, abre-se uma janela de input curta ancorada no
    /// carro. Acertar promove AQUELA curva em um nivel de qualidade: Good 1.00 ->
    /// Perfect 1.60. Errar nao custa nada; ausente, a rolagem do simulador vale.
    ///
    /// A regra que governa esta classe:
    ///
    ///     Presenca vale um BONUS, nunca um REQUISITO, e nunca uma TAXA.
    ///
    /// Requisito mataria D-06 e o pilar P3. Taxa - clicar mais rapido rende mais -
    /// transformaria o jogo num clicker e faria do modo Taskbar uma punicao. Por isso o
    /// input aqui e TEMPORIZACAO, com no maximo uma promocao por curva, 6-10 curvas por
    /// corrida, e nenhum ganho por cadencia.
    ///
    /// A restricao arquitetural que torna isto seguro esta em outro lugar: a promocao
    /// altera apenas o score, jamais a trajetoria (ver SegmentOutcome). Se o input
    /// mudasse v_drift, todos os segmentos seguintes precisariam ser re-simulados e a
    /// linha do tempo pre-computada de D-01 desmoronaria.
    /// </summary>
    public sealed class PerfectEntryController : MonoBehaviour
    {
        [Tooltip("Quanto antes a UI comeca a telegrafar a janela, em segundos.")]
        [SerializeField] private float _telegraphLead = 1.2f;

        private readonly List<ActiveWindow> _active = new List<ActiveWindow>();
        private readonly HashSet<int> _consumed = new HashSet<int>();
        private readonly HashSet<int> _announced = new HashSet<int>();

        private RaceResult _result;

        /// <summary>Janelas abertas agora. O HUD desenha o pulso a partir disto.</summary>
        public IReadOnlyList<ActiveWindow> Active { get { return _active; } }

        /// <summary>Proxima janela a abrir, para telegrafar. -1 quando nao ha.</summary>
        public int TelegraphedSegment { get; private set; }

        public int Hits { get; private set; }
        public int Missed { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnRaceStarted(RaceStarted evt)
        {
            _result = evt.Result;
            _active.Clear();
            _consumed.Clear();
            _announced.Clear();
            Hits = 0;
            Missed = 0;
            TelegraphedSegment = -1;
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _result == null || _result.InputWindows == null) return;

            float now = game.PlaybackTime;
            RefreshWindows(now);

            if (Pressed()) TryHit(game);
        }

        /// <summary>
        /// Atualiza a lista de janelas abertas.
        ///
        /// Como a linha do tempo inteira e conhecida ANTES de a reproducao comecar
        /// (GDD 20.6), a UI sabe que uma curva vem ai antes de o carro chegar nela - e
        /// por isso o telegrafo e honesto, e nao um aviso de ultima hora.
        /// </summary>
        private void RefreshWindows(float now)
        {
            _active.Clear();
            TelegraphedSegment = -1;

            InputWindow[] windows = _result.InputWindows;
            for (int i = 0; i < windows.Length; i++)
            {
                InputWindow window = windows[i];

                if (now < window.OpensAt)
                {
                    if (now >= window.OpensAt - _telegraphLead && TelegraphedSegment < 0)
                    {
                        TelegraphedSegment = window.SegmentIndex;
                        if (_announced.Add(window.SegmentIndex))
                            EventBus.Publish(new InputWindowOpened { Window = window });
                    }
                    continue;
                }

                if (now > window.ClosesAt)
                {
                    // Perdeu a janela: nada acontece. A rolagem do simulador vale, sem
                    // alteracao - e exatamente isso que faz a ausencia nao ser punicao.
                    if (_consumed.Add(-window.SegmentIndex - 1)) Missed++;
                    continue;
                }

                float span = Mathf.Max(0.0001f, window.ClosesAt - window.OpensAt);
                _active.Add(new ActiveWindow
                {
                    Window = window,
                    Progress = (now - window.OpensAt) / span,
                });
            }
        }

        /// <summary>
        /// Um acerto promove UMA curva. Clicar de novo na mesma janela nao faz nada -
        /// e o que impede a mecanica de virar cadencia de cliques.
        /// </summary>
        private void TryHit(GameManager game)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                int segment = _active[i].Window.SegmentIndex;
                if (_consumed.Contains(segment)) continue;

                _consumed.Add(segment);
                if (game.TryPromote(segment)) Hits++;
                return;
            }
        }

        private static bool Pressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }
    }
}
