// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 19.2
// -----------------------------------------------------------------------------

using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>A camera elevada que faz o drift funcionar.
    ///
    /// Alvo: 30-40% MAIS ALTA que a linha de base do Horizon Chase (GDD 19.2). Na altura
    /// canonica de Top Gear o yaw fica tao encurtado que um carro derrapando aparece so
    /// como um carro mais estreito. Calibrar na Fase 1.
    ///
    /// posicao alvo = carro.pos + offset base (elevada) + offset de velocidade
    ///              + offset de drift;  FOV = base + f(v) + f(angulo).
    /// Toda transicao e amortecida. Durante drift a camera ATRASA de proposito.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        // TODO(GDD 19.2): implementar.
    }
}
