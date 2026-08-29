// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.PlayMode
//  GDD 0.2  secoes 20.5, 17
// -----------------------------------------------------------------------------

using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FramedDrift.Tests.PlayMode
{
    public class SaveLoadTests
    {
        [UnityTest]
        [Ignore("TODO(Fase 6): precisa do SaveManager.")]
        public IEnumerator OfflineProgress_MatchesOnlineWithinTolerance()
        {
            // Criterio de saida da Fase 6 e mitigacao de R4: fechar, esperar 1 h,
            // abrir e receber o esperado dentro de +/-8%. Nao desativavel (GDD 21.2).
            Assert.Fail("nao implementado");
            yield break;
        }

        [UnityTest]
        [Ignore("TODO(Fase 6): precisa do SaveManager.")]
        public IEnumerator ClockMovingBackwards_DoesNotPunishPlayer()
        {
            // GDD 20.5: clamp em zero, sem acusacao.
            Assert.Fail("nao implementado");
            yield break;
        }
    }
}
