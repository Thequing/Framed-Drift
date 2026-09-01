// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 5.1, 11, 14.2, 21.2
// -----------------------------------------------------------------------------

using System.Text;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// A escada de PISTA. O tier de uma corrida sai da pista (`RaceFactory`), entao e a
    /// pista - e nada mais - que liga rarityWeights, itemLevelCap, difficultyScale,
    /// rewardScale e o corte de tier do loot.
    ///
    /// O criterio de vitoria da GDD 21.2 (55-75%) e escrito para "carro inicial na pista
    /// inicial". Aqui ele sobe um degrau: vale para a pista de ENTRADA de cada tier, com
    /// a build daquele tier. E o que faz subir de tier ser uma troca de patamar em vez de
    /// um aumento de dificuldade sem contrapartida.
    ///
    /// As demais pistas do tier ficam DE PROPOSITO abaixo da faixa com a build de
    /// entrada - esse e o degrau interno do tier. Ver os dois testes.
    /// </summary>
    public sealed class TrackLadderTests
    {
        private const int Samples = 600;

        private static readonly string[] TierDBuild =
        {
            "eng_stock", "tur_stock", "trn_stock", "dif_street",
            "sus_stock", "tir_street", "brk_stock", "aer_stock",
        };

        private static readonly string[] TierCBuild =
        {
            "eng_forged", "tur_twin", "trn_dogbox", "dif_clutch",
            "sus_adjustable", "tir_semislick", "brk_slotted", "aer_widebody",
        };

        private static readonly string[] TierBBuild =
        {
            "eng_stroker", "tur_bigframe", "trn_sequential", "dif_comp",
            "sus_pillowball", "tir_racing", "brk_bigbrake", "aer_carbon",
        };

        // O pneu de cada build e o mais AGARRADO disponivel ate aquele tier, nao o
        // pneu daquele tier: perfil nao e nivel (GDD 9.3), e o tir_drift_comp de A e a
        // alternativa de angulo, nao um upgrade do racing de B.
        private static readonly string[] TierABuild =
        {
            "eng_billet", "tur_compound", "trn_dct", "dif_spool",
            "sus_multilink", "tir_racing", "brk_carbon", "aer_active",
        };

        private static readonly string[] TierSBuild =
        {
            "eng_race", "tur_antilag", "trn_straightcut", "dif_active",
            "sus_damper", "tir_slick", "brk_race", "aer_ground",
        };

        /// <summary>
        /// A pista de ENTRADA de cada tier - a de menor reputacao exigida - tem de cair
        /// na faixa de 55-75% da GDD 21.2 com a build daquele tier.
        ///
        /// So a de entrada. As outras pistas do mesmo tier sao o degrau INTERNO: com a
        /// build de entrada elas vencem menos de proposito, e e isso que empurra o jogador
        /// a montar antes de subir. `city_docks` e `city_underpass` sempre foram assim -
        /// 36% e 38% com a build de fabrica - e tratar isso como falha seria calibrar
        /// para o lado errado, achatando a progressao dentro do tier.
        /// </summary>
        [Test]
        public void PistaDeEntradaDeCadaTierVenceNaFaixaDaGdd()
        {
            ContentDatabase content = TestWorld.Shared.Content;
            var report = new StringBuilder("Pista de entrada por tier:");
            bool ok = true;

            for (int t = 0; t <= (int)TierRank.S; t++)
            {
                var tier = (TierRank)t;
                TrackDef entry = EntryTrack(content, tier);
                if (entry == null) continue;

                Measurement m = Measure(entry, BuildFor(tier));
                report.Append('\n').Append(Line(entry, m));

                if (m.WinRate < 0.55f || m.WinRate > 0.75f) ok = false;
            }

            TestContext.WriteLine(report.ToString());
            Assert.IsTrue(ok, report.ToString());
        }

        /// <summary>
        /// Dentro de um tier, a pista mais cara em reputacao tem de ser mais DIFICIL com a
        /// mesma build. Sem isso, "destravar" a segunda pista do tier nao significaria
        /// nada - seria a mesma corrida com outro nome.
        /// </summary>
        [Test]
        public void DentroDoTierAPistaSeguinteEMaisDificil()
        {
            ContentDatabase content = TestWorld.Shared.Content;
            var report = new StringBuilder("Degrau dentro do tier:");
            bool ok = true;

            for (int t = 0; t <= (int)TierRank.S; t++)
            {
                var tier = (TierRank)t;
                TrackDef entry = EntryTrack(content, tier);
                if (entry == null) continue;

                float entryWin = Measure(entry, BuildFor(tier)).WinRate;

                for (int i = 0; i < content.TrackList.Count; i++)
                {
                    TrackDef track = content.TrackList[i];
                    if (track.Tier != tier || track.Id == entry.Id) continue;

                    Measurement m = Measure(track, BuildFor(tier));
                    report.Append('\n').Append(Line(track, m))
                          .Append("  (entrada ").Append((entryWin * 100f).ToString("0.0")).Append("%)");

                    if (m.WinRate > entryWin) ok = false;
                }
            }

            TestContext.WriteLine(report.ToString());
            Assert.IsTrue(ok, report.ToString());
        }

        private static string Line(TrackDef track, Measurement m)
        {
            return "  " + track.Id.PadRight(22)
                   + "tier " + track.Tier
                   + "  vitorias " + (m.WinRate * 100f).ToString("0.0") + "%"
                   + "  score " + m.MeanScore.ToString("N0")
                   + "  cash " + m.MeanCash.ToString("N0")
                   + "  tempo " + m.MeanTime.ToString("0.0")
                   + "  tRef " + track.BaseTimeSeconds.ToString("0.0")
                   + "  k " + (track.BaseTimeSeconds / m.MeanTime).ToString("0.000");
        }

        /// <summary>A pista de menor reputacao exigida dentro do tier.</summary>
        private static TrackDef EntryTrack(ContentDatabase content, TierRank tier)
        {
            TrackDef best = null;
            for (int i = 0; i < content.TrackList.Count; i++)
            {
                TrackDef track = content.TrackList[i];
                if (track.Tier != tier) continue;
                if (best == null || track.ReputationRequired < best.ReputationRequired) best = track;
            }
            return best;
        }

        /// <summary>
        /// Subir de tier tem de PAGAR. Nao e automatico: a dificuldade sobe junto, e uma
        /// pista de tier alto mal calibrada pode render menos por corrida que a anterior.
        /// </summary>
        [Test]
        public void SubirDeTierAumentaOCashPorCorrida()
        {
            ContentDatabase content = TestWorld.Shared.Content;

            double previous = 0.0;
            var report = new StringBuilder("Cash por corrida na melhor pista de cada tier:");

            for (int t = 0; t <= (int)TierRank.S; t++)
            {
                var tier = (TierRank)t;
                double cash = BestCashAtTier(content, tier);

                report.Append('\n').Append("  tier ").Append(tier)
                      .Append("  ").Append(cash.ToString("N0"));

                Assert.Greater(cash, previous,
                    "O tier " + tier + " tem de pagar mais que o anterior. " + report);
                previous = cash;
            }

            TestContext.WriteLine(report.ToString());
        }

        /// <summary>
        /// O teto da GDD 11.6 e de CONDICAO, e o tier multiplica por FORA dele.
        ///
        /// Os exemplos que definem o teto de 4,5 na 11.6 sao todos de condicao empilhada
        /// ("Montanha, 02h, tempestade"), com o tier parado. Grampear o tier junto
        /// quebraria o topo da escada: o rewardScale de S e 6,20 e estoura 4,5 sozinho,
        /// em pista seca, de dia, sem trafego - tier A e S pagariam o mesmo que um tier B
        /// na chuva, e a 14.2 ("suba de tier em vez de moer o mesmo tier") viraria mentira.
        /// </summary>
        [Test]
        public void OTetoDaCondicaoNaoGrampeiaAEscadaDeTier()
        {
            TestWorld world = TestWorld.Shared;
            var b = world.Content.Balance;

            // Condicao branda: o teto nem entra na conta, entao o multiplicador tem de ser
            // exatamente o rewardScale do tier.
            for (int t = 0; t <= (int)TierRank.S; t++)
            {
                var tier = (TierRank)t;
                TrackDef track = FirstTrackAtTier(world.Content, tier);

                RaceInstance race = world.Race("kite_130", track.Id, world.FactoryBuild(),
                                               TuningSetup.Neutral, DriftStyle.Balanced,
                                               TimeOfDay.Day, Weather.Clear, 1UL);

                float mult = world.Resolver.RewardMultiplier(race);
                Assert.Greater(mult, 0f);

                if (tier == TierRank.S)
                    Assert.Greater(mult, b.RewardMultCap,
                        "O tier S sozinho (rewardScale 6,20) tem de passar do teto de condicao.");
            }
        }

        /// <summary>A parte de CONDICAO continua grampeada, que e o que a 11.6 pediu.</summary>
        [Test]
        public void ACondicaoSozinhaNuncaPassaDoTeto()
        {
            TestWorld world = TestWorld.Shared;
            var b = world.Content.Balance;

            TrackDef d = FirstTrackAtTier(world.Content, TierRank.D);

            // A pior combinacao possivel, no tier cujo rewardScale e exatamente 1,0.
            RaceInstance race = world.Race("kite_130", d.Id, world.FactoryBuild(),
                                           TuningSetup.Neutral, DriftStyle.Reckless,
                                           TimeOfDay.Night, Weather.HeavyRain, 1UL);
            race.Conditions.Traffic = TrafficDensity.Heavy;

            float mult = world.Resolver.RewardMultiplier(race);

            Assert.AreEqual(1f, world.Content.Tier(TierRank.D).RewardScale, 0.001f,
                "O teste so mede a condicao se o tier D valer 1,0.");
            Assert.LessOrEqual(mult, b.RewardMultCap + 0.001f,
                "Clima, horario e trafego empilhados tem de continuar grampeados em "
                + b.RewardMultCap + ".");
        }

        [Test]
        public void CadaTierTemPeloMenosUmaPista()
        {
            ContentDatabase content = TestWorld.Shared.Content;

            for (int t = 0; t <= (int)TierRank.S; t++)
                AssertTierHasTrack(content, (TierRank)t);
        }

        /// <summary>
        /// O corte de tier do loot so vale se alguma corrida chegar la. Esta e a prova de
        /// que a peca de tier C, hoje, tem um caminho de DROP e nao so o da loja.
        /// </summary>
        [Test]
        public void PistaDeTierCDropaPecaDeTierC()
        {
            ContentDatabase content = TestWorld.Shared.Content;
            TrackDef track = FirstTrackAtTier(content, TierRank.C);

            bool sawC = false;
            for (ulong seed = 1UL; seed <= 3000UL && !sawC; seed++)
            {
                RaceInstance race = Race(track, TierCBuild, seed);
                RaceResult result = TestWorld.Shared.Resolver.ResolveComplete(race);

                PartDrop[] drops = result.Rewards.Drops;
                for (int i = 0; i < drops.Length; i++)
                    if (content.Part(drops[i].BaseId).Tier == TierRank.C) sawC = true;
            }

            Assert.IsTrue(sawC, "Nenhuma peca tier C caiu em 3000 corridas na pista de tier C.");
        }

        private static double BestCashAtTier(ContentDatabase content, TierRank tier)
        {
            double best = 0.0;
            for (int i = 0; i < content.TrackList.Count; i++)
            {
                TrackDef track = content.TrackList[i];
                if (track.Tier != tier) continue;

                double cash = Measure(track, BuildFor(tier)).MeanCash;
                if (cash > best) best = cash;
            }
            return best;
        }

        private static void AssertTierHasTrack(ContentDatabase content, TierRank tier)
        {
            for (int i = 0; i < content.TrackList.Count; i++)
                if (content.TrackList[i].Tier == tier) return;

            Assert.Fail("Nenhuma pista no tier " + tier + ".");
        }

        private static TrackDef FirstTrackAtTier(ContentDatabase content, TierRank tier)
        {
            for (int i = 0; i < content.TrackList.Count; i++)
                if (content.TrackList[i].Tier == tier) return content.TrackList[i];

            Assert.Fail("Nenhuma pista no tier " + tier + ".");
            return null;
        }

        private static string[] BuildFor(TierRank tier)
        {
            if (tier == TierRank.C) return TierCBuild;
            if (tier == TierRank.B) return TierBBuild;
            if (tier == TierRank.A) return TierABuild;
            if (tier == TierRank.S) return TierSBuild;
            return TierDBuild;
        }

        private struct Measurement
        {
            public float WinRate;
            public double MeanScore;
            public double MeanCash;
            public double MeanTime;
        }

        private static Measurement Measure(TrackDef track, string[] build)
        {
            int wins = 0;
            double score = 0.0;
            double cash = 0.0;
            double time = 0.0;

            for (int i = 0; i < Samples; i++)
            {
                RaceInstance race = Race(track, build, 0x51EDU + (ulong)i);
                RaceResult result = TestWorld.Shared.Resolver.ResolveComplete(race);

                if (result.Won) wins++;
                score += result.Rewards.DriftScore;
                cash += result.Rewards.Cash;
                time += result.TotalTime;
            }

            return new Measurement
            {
                WinRate = wins / (float)Samples,
                MeanScore = score / Samples,
                MeanCash = cash / Samples,
                MeanTime = time / Samples,
            };
        }

        private static RaceInstance Race(TrackDef track, string[] build, ulong seed)
        {
            var parts = new RolledPart[8];
            for (int i = 0; i < build.Length; i++)
                parts[i] = TestWorld.Shared.Resolver.Loot.Factory(build[i]);

            return TestWorld.Shared.Race("kite_130", track.Id, parts, TuningSetup.Neutral,
                                         DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, seed);
        }
    }
}
