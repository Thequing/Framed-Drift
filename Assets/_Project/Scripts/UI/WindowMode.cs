// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secao 18
// -----------------------------------------------------------------------------

namespace FramedDrift.UI
{
    /// <summary>Os tres modos de janela da secao 18.</summary>
    public enum WindowMode
    {
        /// <summary>1280x720+. Corrida 2.5D + HUD completo. GDD 18.1.</summary>
        Full,

        /// <summary>~480x270. Sem 3D, tracado esquematico. GDD 18.2.</summary>
        Compact,

        /// <summary>~360x48. Fase 15 (D-07). Sem interacao - ver 18.2. GDD 18.3.</summary>
        Taskbar,
    }
}
