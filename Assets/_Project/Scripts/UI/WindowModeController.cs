// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 17.1, 18.1, 18.2, 18.3, 20.4, D-07
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Core;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// Alterna entre os modos de janela e aplica o orcamento de CPU de cada um.
    ///
    /// D-07: o modo Taskbar e feature de RELEASE (Fase 15), nao de MVP. Ele e a
    /// assinatura do produto, mas e uma feature de APRESENTACAO - construi-la cedo
    /// travaria decisoes de UI antes de saber quais informacoes importam.
    ///
    /// O que existe aqui desde ja e o ORCAMENTO (GDD 20.4), medido a partir da Fase 5 e
    /// nao da 15. O risco R8 - "modo Taskbar consome CPU demais e o jogador fecha" - se
    /// descobre cedo ou nao se resolve: se a arquitetura so couber no orcamento com uma
    /// reescrita, e melhor saber agora.
    /// </summary>
    public sealed class WindowModeController : MonoBehaviour
    {
        [SerializeField] private GameHud _fullHud;
        [SerializeField] private TaskbarUI _taskbar;
        [SerializeField] private WindowMode _mode = WindowMode.Full;

        private static readonly Vector2Int FullSize = new Vector2Int(1280, 720);
        private static readonly Vector2Int CompactSize = new Vector2Int(480, 270);
        private static readonly Vector2Int TaskbarSize = new Vector2Int(360, 48);

        public WindowMode Mode { get { return _mode; } }

        private void Start()
        {
            Apply(_mode);
        }

        public void Apply(WindowMode mode)
        {
            _mode = mode;

            if (_fullHud != null) _fullHud.enabled = mode == WindowMode.Full;
            if (_taskbar != null) _taskbar.enabled = mode == WindowMode.Taskbar;

            // O solucionador e o MESMO nos tres modos (D-01). O que muda e so a taxa de
            // amostragem e o que e desenhado (GDD 17.1).
            switch (mode)
            {
                case WindowMode.Full:
                    Resize(FullSize);
                    ExecutionBudget.Apply(ExecutionState.Active);
                    break;

                case WindowMode.Compact:
                    Resize(CompactSize);
                    ExecutionBudget.Apply(ExecutionState.Compact);
                    break;

                case WindowMode.Taskbar:
                    Resize(TaskbarSize);
                    ExecutionBudget.Apply(ExecutionState.Compact);
                    break;
            }

            GameManager game = GameManager.Instance;
            if (game != null) game.Save.Settings.WindowMode = (int)mode;
        }

        public void Cycle()
        {
            Apply((WindowMode)(((int)_mode + 1) % 3));
        }

        private static void Resize(Vector2Int size)
        {
#if !UNITY_EDITOR
            // Redimensionar no editor brigaria com a janela de Game; em build e o efeito
            // real. Sem borda e sempre-no-topo entram na Fase 15 junto com o resto do
            // modo Taskbar (GDD 18.3).
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
#endif
        }
    }
}
