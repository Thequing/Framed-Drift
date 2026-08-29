// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 3.6.1
// -----------------------------------------------------------------------------

using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>A Entrada Perfeita. Abre uma janela de input curta na entrada de cada curva com
    /// drift; acertar promove Good -> Perfect naquela curva.
    ///
    /// REGRA QUE NAO PODE SER QUEBRADA: isto altera APENAS SegmentOutcome.QualityFinal.
    /// Jamais VExit, Angle ou qualquer estado continuo. Mexer na fisica obrigaria a
    /// re-simular os segmentos seguintes e derrubaria D-01.
    ///
    /// Bad nunca vira Good - erro do simulador continua erro.</summary>
    public sealed class PerfectEntryController : MonoBehaviour
    {
        // TODO(GDD 3.6.1): implementar.
    }
}
