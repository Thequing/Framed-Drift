// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 17, 20.5, 21.2, D-02
// -----------------------------------------------------------------------------

using FramedDrift.Simulation;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O teste que protege o pilar P3, e que a GDD 21.2 diz que NUNCA pode ser desativado.
    ///
    /// "Cash/hora offline fica dentro de +-8% do cash/hora online medido."
    ///
    /// Se este teste cair, a promessa central do jogo caiu junto: o que o carro faz
    /// ausente deixou de ser o que ele faria presente.
    /// </summary>
    public class OfflineParityTests
    {
        [Test]
        public void OfflineCashPerHour_MatchesOnlineWithinTolerance()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            // --- online: corrida a corrida ------------------------------------
            double onlineCash = 0.0;
            double onlineSeconds = 0.0;
            const int races = 1500;

            for (ulong seed = 0; seed < races; seed++)
            {
                RaceResult result = world.Resolver.ResolveComplete(world.StarterRace(seed * 3 + 91));
                onlineCash += result.Rewards.Cash;
                onlineSeconds += result.TotalTime + world.Content.Balance.InterRaceDelaySeconds;
            }

            double onlinePerHour = onlineCash / (onlineSeconds / 3600.0);

            // --- offline: reconstruido pela agregacao ---------------------------
            //
            // Auto-reparo ligado dos dois lados: sem ele a operacao PARA quando o dano
            // passa de 80 - comportamento correto (GDD 16.2), mas que mediria a regra de
            // parada em vez da agregacao. A paridade de D-02 e sobre a matematica da
            // reconstrucao.
            OfflineRules rules = OfflineRules.Default;
            rules.AutoRepair = true;

            const int trials = 30;
            const float hours = 4f;
            double offlineTotal = 0.0;

            for (int i = 0; i < trials; i++)
            {
                OfflineReport report = aggregator.Aggregate(
                    world.StarterRace(9001UL), hours * 3600f, 0, rules, 0f, (ulong)(i * 977 + 5));

                // Bruto dos dois lados: o custo de reparo e uma regra de automacao, nao
                // parte da recompensa que a corrida gerou.
                offlineTotal += report.Cash + report.RepairCost;
            }

            double offlinePerHour = offlineTotal / trials / hours;
            double delta = System.Math.Abs(offlinePerHour - onlinePerHour) / onlinePerHour;
            float tolerance = world.Content.Balance.OfflineParityTolerance;

            Assert.That(delta, Is.LessThanOrEqualTo((double)tolerance),
                "GDD 21.2: cash/hora offline deve ficar dentro de +-" + (tolerance * 100) + "% do online. "
                + "online " + onlinePerHour.ToString("N0") + "/h vs offline "
                + offlinePerHour.ToString("N0") + "/h (delta " + (delta * 100).ToString("0.00") + "%)");
        }

        [Test]
        public void OfflineCap_IsHonestAndUpgradable()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            OfflineRules rules = OfflineRules.Default;
            rules.AutoRepair = true;

            OfflineReport under = aggregator.Aggregate(world.StarterRace(1UL), 4f * 3600f, 0, rules, 0f, 1UL);
            OfflineReport over = aggregator.Aggregate(world.StarterRace(1UL), 30f * 3600f, 0, rules, 0f, 1UL);
            OfflineReport upgraded = aggregator.Aggregate(world.StarterRace(1UL), 30f * 3600f, 3, rules, 0f, 1UL);

            Assert.IsFalse(under.HitCap, "4 h esta abaixo do teto base de 8 h.");
            Assert.IsTrue(over.HitCap, "30 h esta acima do teto; o relatorio precisa dizer isso.");
            Assert.AreEqual(8f * 3600f, over.OperatedSeconds, 1f, "O teto base e de 8 horas (GDD 17.3).");

            // "Turno Extra": +2 h por nivel, ate 24 h.
            Assert.AreEqual(14f * 3600f, upgraded.OperatedSeconds, 1f,
                "Tres niveis de Turno Extra devem levar o teto a 14 h.");
        }

        [Test]
        public void ClockRunningBackwards_IsNeverPunished()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            OfflineReport report = aggregator.Aggregate(
                world.StarterRace(1UL), -5000f, 0, OfflineRules.Default, 0f, 1UL);

            // GDD 20.5: detectar relogio andando para tras e NAO punir - clamp em zero,
            // sem acusacao. O caso comum e fuso horario ou NTP, nao fraude.
            Assert.AreEqual(0, report.Races);
            Assert.AreEqual(0L, report.Cash);
        }

        [Test]
        public void AutomationRules_StopTheOperationAndSayWhy()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            OfflineReport unattended = aggregator.Aggregate(
                world.StarterRace(555UL), 8f * 3600f, 0, OfflineRules.Default, 0f, 7UL);

            // O passo 5 da secao 17.2 e o que faz o relatorio parecer VIVO em vez de uma
            // multiplicacao: o jogador precisa ver que a operacao parou, quando e por que.
            Assert.IsNotNull(unattended.StoppedReason,
                "Sem auto-reparo, a operacao deve parar quando o dano passa do limite.");
            Assert.Less(unattended.OperatedSeconds, 8f * 3600f);

            OfflineRules repairing = OfflineRules.Default;
            repairing.AutoRepair = true;
            OfflineReport attended = aggregator.Aggregate(
                world.StarterRace(555UL), 8f * 3600f, 0, repairing, 0f, 7UL);

            Assert.IsNull(attended.StoppedReason, "Com auto-reparo, a operacao vai ate o fim.");
            Assert.Greater(attended.Races, unattended.Races);
            Assert.Greater(attended.RepairCost, 0L, "O reparo automatico precisa cobrar.");
        }

        [Test]
        public void CollectiblesNeverAppearOffline()
        {
            TestWorld world = TestWorld.Shared;
            RaceInstance race = world.StarterRace(31337UL);

            RaceResult result = world.Resolver.ResolveComplete(race);

            // GDD 3.6.2: coletaveis nao sao "perdidos" na ausencia - eles simplesmente
            // nao sao gerados. E isso que mantem a promessa de D-02 literalmente
            // verdadeira, e nao apenas tecnicamente verdadeira.
            Assert.AreEqual(0, result.CollectiblesTaken);
            Assert.AreEqual(0, result.PromotionsApplied);
        }
    }
}
