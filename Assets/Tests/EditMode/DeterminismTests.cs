// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secao 20.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Rng;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// D-01 e D-02 dependem inteiramente de determinismo. Estes testes sao a rede.
    /// </summary>
    public class DeterminismTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new DeterministicRng(12345UL);
            var b = new DeterministicRng(12345UL);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(a.NextULong(), b.NextULong(), "divergiu no passo {0}", i);
            }
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = new DeterministicRng(1UL);
            var b = new DeterministicRng(2UL);

            bool anyDifference = false;
            for (int i = 0; i < 100 && !anyDifference; i++)
            {
                if (a.NextULong() != b.NextULong()) anyDifference = true;
            }

            Assert.IsTrue(anyDifference, "seeds diferentes produziram a mesma sequencia");
        }

        [Test]
        public void Streams_AreIndependent()
        {
            // Rebalancear loot nao pode alterar o resultado da corrida (GDD 20.3).
            var one = new RngStreams(999UL);
            var two = new RngStreams(999UL);

            for (int i = 0; i < 50; i++) two.Loot.NextULong();   // consome so o loot

            Assert.AreEqual(one.Execution.NextULong(), two.Execution.NextULong(),
                "consumir o fluxo de loot alterou o fluxo de execucao");
        }

        // TODO(Fase 3): RaceInstance + Seed reproduzem a corrida byte a byte.
        // TODO(Fase 3): teste que falha se UnityEngine.Random aparecer em Simulation.
        //   (o asmdef com noEngineReferences ja torna isso erro de compilacao)
    }
}
