// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 6.2, 6.3, 12.1, 21.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// Os testes de balanceamento da secao 21.2, como testes de verdade.
    ///
    /// Eles usam o MESMO <see cref="BalanceReport"/> que a janela de editor da 21.1
    /// mostra ao designer. Se fossem dois codigos, o numero que o designer ve e o que o
    /// teste verifica poderiam divergir - e o teste serviria para nada.
    /// </summary>
    public class BalanceTests
    {
        private const int Races = 1500;

        private static BalanceReport StarterReport()
        {
            TestWorld world = TestWorld.Shared;
            return BalanceReport.Run(world.Resolver, world.StarterRace(1UL), Races, 0xBA1AUL,
                                     "kite / city_loop / dia / seco");
        }

        [Test]
        public void StarterCar_OnStarterTrack_ScoresInReferenceRange()
        {
            BalanceReport report = StarterReport();

            Assert.That(report.Score.Mean, Is.InRange(6000.0, 12000.0),
                "GDD 6.3: carro inicial, pista inicial, dia, seco deve render 6.000-12.000 "
                + "de Drift Score. Medido: " + report.Score.Mean.ToString("N0"));
        }

        [Test]
        public void StarterCar_OnStarterTrack_WinsBetween55And75Percent()
        {
            BalanceReport report = StarterReport();

            Assert.That(report.WinRate, Is.InRange(0.55, 0.75),
                "GDD 21.2: o carro inicial na pista inicial deve vencer 55-75% das corridas. "
                + "Medido: " + (report.WinRate * 100).ToString("0.0") + "%");
        }

        [Test]
        public void StarterRace_SitsInLowRiskBand()
        {
            TestWorld world = TestWorld.Shared;
            BalanceReport report = StarterReport();
            RiskBand band = world.Content.Balance.Band((float)report.Risk.Mean);

            // A corrida inicial em pleno dia PRECISA ser de risco baixo. Se ela ja abrisse
            // em MEDIO, o slider de risco da 16.2 nao teria para onde crescer.
            Assert.AreEqual(RiskBand.Low, band,
                "GDD 12.1: a corrida inicial de dia deve ficar na faixa BAIXO. RiskIndex medido: "
                + report.Risk.Mean.ToString("0.0"));
        }

        [Test]
        public void MediumRisk_FailureRateBetween3And6Percent()
        {
            TestWorld world = TestWorld.Shared;

            // Noite com chuva e como o jogador chega ao risco MEDIO na pratica.
            RaceInstance race = world.Race("kite_130", "city_loop", world.FactoryBuild(),
                                           TuningSetup.Neutral, DriftStyle.Balanced,
                                           TimeOfDay.Night, Weather.Rain, 7UL);

            BalanceReport report = BalanceReport.Run(world.Resolver, race, 3000, 0x5A1AUL, "medio");
            RiskBand band = world.Content.Balance.Band((float)report.Risk.Mean);

            Assert.AreEqual(RiskBand.Medium, band,
                "A configuracao de teste precisa estar de fato em risco MEDIO. Medido: "
                + report.Risk.Mean.ToString("0.0"));

            Assert.That(report.FailureRate, Is.InRange(0.03, 0.06),
                "GDD 21.2: taxa de falha em risco MEDIO deve ficar entre 3% e 6%. Medido: "
                + (report.FailureRate * 100).ToString("0.00") + "%");
        }

        [Test]
        public void ExtremeRisk_FailsAtLeastTwiceAsOftenAsMedium()
        {
            TestWorld world = TestWorld.Shared;

            RaceInstance medium = world.Race("kite_130", "city_loop", world.FactoryBuild(),
                                             TuningSetup.Neutral, DriftStyle.Balanced,
                                             TimeOfDay.Night, Weather.Rain, 7UL);
            RaceInstance extreme = world.Race("kite_130", "city_loop", world.FactoryBuild(),
                                              TuningSetup.Neutral, DriftStyle.Reckless,
                                              TimeOfDay.Night, Weather.HeavyRain, 7UL);

            BalanceReport a = BalanceReport.Run(world.Resolver, medium, 2000, 1UL, "medio");
            BalanceReport b = BalanceReport.Run(world.Resolver, extreme, 2000, 1UL, "extremo");

            Assert.AreEqual(RiskBand.Extreme, world.Content.Balance.Band((float)b.Risk.Mean),
                "A configuracao extrema precisa estar de fato em EXTREMO.");

            // O salto de MEDIO para EXTREMO e o que faz o slider de risco ser uma decisao.
            Assert.That(b.FailureRate, Is.GreaterThanOrEqualTo(a.FailureRate * 2.0),
                "GDD 12.3: risco EXTREMO deve falhar muito mais que MEDIO. "
                + (b.FailureRate * 100).ToString("0.0") + "% vs " + (a.FailureRate * 100).ToString("0.0") + "%");
        }

        [Test]
        public void DriftBuild_OutEarnsSpeedBuild()
        {
            TestWorld world = TestWorld.Shared;

            var driftParts = world.FactoryBuild();
            driftParts[(int)PartSlot.Differential] = world.Part("dif_lsd2way");
            driftParts[(int)PartSlot.Suspension] = world.Part("sus_coilover");
            driftParts[(int)PartSlot.Tires] = world.Part("tir_drift");
            driftParts[(int)PartSlot.Turbo] = world.Part("tur_sport");

            var speedParts = world.FactoryBuild();
            speedParts[(int)PartSlot.Engine] = world.Part("eng_sport");
            speedParts[(int)PartSlot.Transmission] = world.Part("trn_short");
            speedParts[(int)PartSlot.Brakes] = world.Part("brk_sport");
            speedParts[(int)PartSlot.AeroWeight] = world.Part("aer_fiber");
            speedParts[(int)PartSlot.Differential] = world.Part("dif_open");

            RaceInstance drift = world.Race("kanto_ae", "city_docks", driftParts,
                new TuningSetup { Lock = 0.9f, Accel = 0.8f, Decel = 0.7f },
                DriftStyle.Aggressive, TimeOfDay.Day, Weather.Clear, 1UL);

            RaceInstance speed = world.Race("kanto_ae", "city_docks", speedParts,
                new TuningSetup { Lock = 0.15f, Accel = 0.25f, Decel = 0.5f },
                DriftStyle.Safe, TimeOfDay.Day, Weather.Clear, 1UL);

            BalanceReport driftReport = BalanceReport.Run(world.Resolver, drift, 800, 23UL, "drift");
            BalanceReport speedReport = BalanceReport.Run(world.Resolver, speed, 800, 23UL, "speed");

            double ratio = driftReport.Cash.Mean / System.Math.Max(1.0, speedReport.Cash.Mean);

            Assert.That(ratio, Is.InRange(1.8, 2.6),
                "GDD 21.2: build de drift dedicada deve render 1,8x-2,6x mais cash que build de "
                + "velocidade. Medido: " + ratio.ToString("0.00") + "x");

            // A tensao central da secao 3.5: a build que ganha MENOS corridas ganha MAIS
            // dinheiro. Se isso deixar de valer, o tuning colapsou numa dimensao so.
            Assert.That(speedReport.WinRate, Is.GreaterThan(driftReport.WinRate),
                "GDD 3.5: a build de velocidade deve vencer mais corridas que a de drift.");
        }

        [Test]
        public void RacesVary_SoTheyAreNotIndistinguishable()
        {
            BalanceReport report = StarterReport();

            // Risco R2: "o simulador produz corridas indistinguiveis entre si".
            Assert.That(report.Score.StdDev, Is.GreaterThan(report.Score.Mean * 0.05),
                "Risco R2: as corridas precisam variar. sigma medido: "
                + report.Score.StdDev.ToString("N0"));
        }

        [Test]
        public void NoSinglePart_IsOptimalEverywhere()
        {
            TestWorld world = TestWorld.Shared;

            // GDD 21.2: "nenhuma peca isolada e otima em >70% das builds testadas".
            //
            // A versao minima disso e o diferencial: ele precisa ser uma ESCOLHA, nao um
            // upgrade. Medido no carro inicial, porque e nele que a peca de fato inverte
            // o comportamento - com o diferencial aberto (Initiation -20) o Kite para de
            // derrapar e passa a fazer a curva em grip.
            //
            // No Kanto a resposta e outra e igualmente correta: com a trait Torque bruto
            // (+20 Initiation) ele derrapa de qualquer jeito, e ali o diferencial aberto
            // e simplesmente a peca errada. Essa assimetria entre carros e o que a secao
            // 8.1 pede quando diz que cada carro muda COMO ele e jogado.
            var driftParts = world.FactoryBuild();
            driftParts[(int)PartSlot.Differential] = world.Part("dif_lsd2way");

            var gripParts = world.FactoryBuild();
            gripParts[(int)PartSlot.Differential] = world.Part("dif_open");

            RaceInstance drift = world.Race("kite_130", "city_loop", driftParts, TuningSetup.Neutral,
                                            DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, 5UL);
            RaceInstance grip = world.Race("kite_130", "city_loop", gripParts, TuningSetup.Neutral,
                                           DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, 5UL);

            BalanceReport driftReport = BalanceReport.Run(world.Resolver, drift, 600, 9UL, "2way");
            BalanceReport gripReport = BalanceReport.Run(world.Resolver, grip, 600, 9UL, "aberto");

            Assert.That(driftReport.Score.Mean, Is.GreaterThan(gripReport.Score.Mean * 2.0),
                "O LSD 2-way existe para pontuar: deve render muito mais score que o aberto.");

            Assert.That(gripReport.Time.Mean, Is.LessThan(driftReport.Time.Mean),
                "O diferencial aberto deve ser mais RAPIDO - e o trade-off que o torna uma "
                + "escolha e nao um upgrade inferior (GDD 9.2). Aberto "
                + gripReport.Time.Mean.ToString("0.0") + "s vs 2-way "
                + driftReport.Time.Mean.ToString("0.0") + "s");

            Assert.That(gripReport.WinRate, Is.GreaterThan(driftReport.WinRate),
                "GDD 3.5: quem abre mao do angulo precisa ganhar mais corridas em troca.");
        }
    }
}
