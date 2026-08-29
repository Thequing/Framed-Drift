// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 6.3, 21
// -----------------------------------------------------------------------------

using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>Alvos de balanceamento da secao 6.3 e mitigacao de R2.</summary>
    public class BalanceTests
    {
        [Test]
        [Ignore("TODO(Fase 3): precisa do RaceSimulator e do BalanceConfig preenchido.")]
        public void FirstHour_ScoresLandInDesignRange()
        {
            // Carro inicial, pista inicial, dia, seco -> 6.000 a 12.000. GDD 6.3.
            Assert.Fail("nao implementado");
        }

        [Test]
        [Ignore("TODO(Fase 3): mitigacao de R2 - corridas indistinguiveis entre si.")]
        public void Races_HaveMinimumVariancePerSegment()
        {
            Assert.Fail("nao implementado");
        }

        [Test]
        [Ignore("TODO(Fase 8): duas builds do mesmo carro rendem >40% diferente.")]
        public void TwoBuilds_SameCar_DifferMeaningfully()
        {
            Assert.Fail("nao implementado");
        }
    }
}
