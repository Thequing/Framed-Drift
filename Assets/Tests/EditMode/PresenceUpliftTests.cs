// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 3.6, 6.2
// -----------------------------------------------------------------------------

using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O criterio de saida da Fase 4.5, e a mitigacao de R10.
    ///
    /// A regra da secao 3.6: presenca vale um BONUS, nunca um REQUISITO, e nunca uma TAXA.
    /// Abaixo de ~10% ninguem se importa; acima de ~40% o modo Taskbar vira punicao e o
    /// jogo deixa de ser idle.
    /// </summary>
    public class PresenceUpliftTests
    {
        [Test]
        [Ignore("TODO(Fase 4.5): precisa do RaceSimulator e do DriftScorer.")]
        public void PresenceUplift_StaysWithinDesignCeiling()
        {
            // Arrange: 1.000 corridas com a mesma configuracao.
            // Act:     score com todas as promocoes vs. score sem nenhuma.
            // Assert:  uplift em [0.20, 0.30].
            Assert.Fail("nao implementado");
        }

        [Test]
        [Ignore("TODO(Fase 4.5): precisa do RaceSimulator e do DriftScorer.")]
        public void UntouchedTimeline_ScoresExactlyLikeOffline()
        {
            // A paridade de D-02 e estrutural: e literalmente a mesma funcao.
            // DriftScorer.Score(timeline intocada) == resultado do OfflineAggregator.
            Assert.Fail("nao implementado");
        }

        [Test]
        [Ignore("TODO(Fase 4.5): precisa do RaceSimulator.")]
        public void BadQuality_IsNeverPromotable()
        {
            // Erro do simulador continua erro. So Good -> Perfect. GDD 3.6.1.
            Assert.Fail("nao implementado");
        }
    }
}
