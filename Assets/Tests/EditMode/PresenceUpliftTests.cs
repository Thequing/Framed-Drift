// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 3.6, 6.2, 20.6, D-02
// -----------------------------------------------------------------------------

using FramedDrift.Simulation;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O criterio de saida da Fase 4.5 e a garantia estrutural de D-02.
    ///
    /// Estes sao os testes que protegem o risco R10 - "a interacao da 3.6 vira obrigacao
    /// e o jogo deixa de ser idle". O teto nao e assumido: e MEDIDO.
    /// </summary>
    public class PresenceUpliftTests
    {
        [Test]
        public void Uplift_Over1000Races_StaysWithinCap()
        {
            TestWorld world = TestWorld.Shared;
            BalanceSettings balance = world.Content.Balance;

            BalanceReport report = BalanceReport.Run(
                world.Resolver, world.StarterRace(11UL), 1000, 0x3611UL, "uplift");

            Assert.That(report.Uplift,
                Is.InRange((double)balance.PresenceUpliftMin, (double)balance.PresenceUpliftMax),
                "GDD 6.2: o uplift medido sobre 1.000 corridas precisa cair em ["
                + balance.PresenceUpliftMin + " - " + balance.PresenceUpliftMax + "]. Medido: "
                + (report.Uplift * 100).ToString("0.00") + "%. Fora disso, ajustar "
                + "promotableCurveFraction ou a janela de input no balance.json.");
        }

        [Test]
        public void UntouchedTimeline_ScoresExactlyLikeOffline()
        {
            TestWorld world = TestWorld.Shared;
            RaceInstance race = world.StarterRace(4242UL);

            RaceResult result = world.Resolver.Simulate(race);
            DriftScorer.Context context = world.Resolver.Scorer.BuildContext(race);

            long viaTimeline = world.Resolver.Scorer.Score(result.Timeline, in context);
            long viaOfflinePath = world.Resolver.Scorer.ScoreWithoutPromotions(result, in context);

            // Nao e "aproximadamente igual": e a MESMA funcao com a mesma entrada. E isso
            // que torna a paridade de D-02 estrutural em vez de algo a testar (GDD 20.6).
            Assert.AreEqual(viaTimeline, viaOfflinePath,
                "A timeline intocada precisa render exatamente o resultado offline.");
        }

        [Test]
        public void Promotion_NeverTouchesPhysics()
        {
            TestWorld world = TestWorld.Shared;
            RaceResult result = world.Resolver.Simulate(world.StarterRace(99UL));

            var before = new SegmentOutcome[result.Timeline.Length];
            System.Array.Copy(result.Timeline, before, before.Length);

            for (int i = 0; i < result.Timeline.Length; i++) result.Promote(i);

            for (int i = 0; i < result.Timeline.Length; i++)
            {
                Assert.AreEqual(before[i].VExit, result.Timeline[i].VExit, "VExit mudou no segmento " + i);
                Assert.AreEqual(before[i].VDrift, result.Timeline[i].VDrift, "VDrift mudou no segmento " + i);
                Assert.AreEqual(before[i].Angle, result.Timeline[i].Angle, "Angle mudou no segmento " + i);
                Assert.AreEqual(before[i].Quality, result.Timeline[i].Quality, "Quality mudou no segmento " + i);

                // Combo e estado continuo (GDD 5.7). Se uma promocao pudesse altera-lo,
                // todo segmento seguinte mudaria de valor e a promocao deixaria de ser
                // local - exatamente o que a restricao arquitetural da 3.6.1 proibe.
                Assert.AreEqual(before[i].Combo, result.Timeline[i].Combo, "Combo mudou no segmento " + i);
            }
        }

        [Test]
        public void Promotion_NeverUpgradesABadCurve()
        {
            TestWorld world = TestWorld.Shared;

            for (ulong seed = 0; seed < 200; seed++)
            {
                RaceResult result = world.Resolver.Simulate(world.StarterRace(seed));
                for (int i = 0; i < result.Timeline.Length; i++) result.Promote(i);

                for (int i = 0; i < result.Timeline.Length; i++)
                {
                    if (result.Timeline[i].Quality != DriftQuality.Bad) continue;

                    // "nunca Bad -> Good: erro do simulador continua erro" (GDD 3.6.1).
                    Assert.AreEqual(DriftQuality.Bad, result.Timeline[i].QualityFinal,
                        "Uma curva Bad foi promovida na seed " + seed + ", segmento " + i);
                }
            }
        }

        [Test]
        public void OnlyGoodCurves_AreEverPromotable()
        {
            TestWorld world = TestWorld.Shared;

            for (ulong seed = 0; seed < 200; seed++)
            {
                RaceResult result = world.Resolver.Simulate(world.StarterRace(seed));
                for (int i = 0; i < result.Timeline.Length; i++)
                {
                    if (!result.Timeline[i].Promotable) continue;
                    Assert.AreEqual(DriftQuality.Good, result.Timeline[i].Quality,
                        "Uma curva nao-Good abriu janela na seed " + seed + ", segmento " + i);
                }
            }
        }

        [Test]
        public void InputWindows_MatchPromotableCurves()
        {
            TestWorld world = TestWorld.Shared;
            RaceResult result = world.Resolver.Simulate(world.StarterRace(77UL));

            int promotable = 0;
            for (int i = 0; i < result.Timeline.Length; i++)
                if (result.Timeline[i].Promotable) promotable++;

            Assert.AreEqual(promotable, result.InputWindows.Length,
                "Toda curva promovivel precisa ter exatamente uma janela de input.");

            float window = world.Content.Balance.PerfectEntryWindowSeconds;
            for (int i = 0; i < result.InputWindows.Length; i++)
            {
                InputWindow input = result.InputWindows[i];
                Assert.AreEqual(window, input.ClosesAt - input.OpensAt, 0.0001f,
                    "A janela precisa durar perfectEntryWindowSeconds.");
            }
        }
    }
}
