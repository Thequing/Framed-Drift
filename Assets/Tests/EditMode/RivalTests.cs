// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 13.1, 3.5, 20.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// Rivais nomeados (GDD 13.1).
    ///
    /// O simulador ja dizia, em comentario, o que estes testes agora exigem: "os
    /// adversarios NAO sao simulados individualmente no MVP (...) rivais nomeados sao a
    /// excecao - esses sao resolvidos pelo MESMO simulador, com carro e build proprios".
    /// Um rival que fosse um numero sorteado seria mais barato e destruiria a premissa:
    /// a build dele precisa ser legivel e contra-atacavel (13.1), e isso so vale se ela
    /// passar pelo mesmo solucionador que a do jogador.
    /// </summary>
    public sealed class RivalTests
    {
        private ContentDatabase _content;
        private RivalResolver _rivals;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;
            _rivals = new RivalResolver(_content, TestWorld.Shared.Resolver);
        }

        // --- o encontro (13.1) --------------------------------------------------------

        [Test]
        public void OEncontroSoOfereceRivalDoTierDaCorrida()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(1UL);

            for (ulong seed = 1UL; seed <= 500UL; seed++)
            {
                race.Seed = seed;
                RivalDef rival = _rivals.RollEncounter(race, new RngStreams(seed).Rival);
                if (rival == null) continue;

                Assert.AreEqual(race.TierIndex, rival.TierIndex,
                    "O rival " + rival.Id + " nao pertence ao tier da corrida.");
            }
        }

        [Test]
        public void OEncontroAcontecePeloMenosUmaVezEmCemCorridas()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(1UL);
            int encounters = 0;

            for (ulong seed = 1UL; seed <= 100UL; seed++)
            {
                race.Seed = seed;
                if (_rivals.RollEncounter(race, new RngStreams(seed).Rival) != null) encounters++;
            }

            Assert.Greater(encounters, 0, "Um rival que nunca aparece nao existe para o jogador.");
        }

        // --- a resolucao pelo mesmo simulador (13.1) ---------------------------------

        [Test]
        public void ORivalCorreDeVerdadeENaoUmNumeroSorteado()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(7UL);
            RaceResult player = TestWorld.Shared.Resolver.ResolveComplete(race);

            RivalOutcome outcome = _rivals.Resolve(race, _content.Rival("akira"), player);

            Assert.Greater(outcome.RivalTime, 0f, "O rival precisa ter um tempo de corrida.");
            Assert.Greater(outcome.RivalScore, 0L, "O rival precisa ter um Drift Score proprio.");
        }

        [Test]
        public void ORivalEDeterministicoPorSeed()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(42UL);
            RivalDef akira = _content.Rival("akira");

            RaceResult first = TestWorld.Shared.Resolver.ResolveComplete(race);
            RivalOutcome a = _rivals.Resolve(race, akira, first);

            RaceResult second = TestWorld.Shared.Resolver.ResolveComplete(race);
            RivalOutcome b = _rivals.Resolve(race, akira, second);

            Assert.AreEqual(a.RivalTime, b.RivalTime, "O tempo do rival mudou entre duas resolucoes.");
            Assert.AreEqual(a.RivalScore, b.RivalScore, "O score do rival mudou entre duas resolucoes.");
        }

        /// <summary>
        /// O guarda do fluxo de RNG (GDD 20.3).
        ///
        /// Se a simulacao do rival consumisse o fluxo Execution do jogador, adicionar um
        /// rival deslocaria as proprias curvas do jogador - toda seed salva mudaria de
        /// resultado e o replay quebraria em silencio. E o mesmo motivo pelo qual loot e
        /// execucao ja moram em fluxos separados.
        /// </summary>
        [Test]
        public void SimularORivalNaoMudaOResultadoDoJogador()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(99UL);

            RaceResult withoutRival = TestWorld.Shared.Resolver.ResolveComplete(race);
            float time = withoutRival.TotalTime;
            long score = withoutRival.Rewards.DriftScore;
            int segments = withoutRival.Timeline.Length;
            var qualities = new DriftQuality[segments];
            for (int i = 0; i < segments; i++) qualities[i] = withoutRival.Timeline[i].Quality;

            RaceResult withRival = TestWorld.Shared.Resolver.ResolveComplete(race);
            _rivals.Resolve(race, _content.Rival("akira"), withRival);

            Assert.AreEqual(time, withRival.TotalTime, "O rival deslocou o tempo do jogador.");
            Assert.AreEqual(score, withRival.Rewards.DriftScore, "O rival deslocou o score do jogador.");

            for (int i = 0; i < segments; i++)
                Assert.AreEqual(qualities[i], withRival.Timeline[i].Quality,
                    "O rival deslocou a qualidade da curva " + i + ".");
        }

        /// <summary>
        /// O rival OCUPA uma vaga do grid, nao acrescenta uma. Ele sempre esteve entre os
        /// N carros da secao 5.6; a diferenca e que agora um deles e resolvido de verdade.
        /// </summary>
        [Test]
        public void ORivalOcupaUmaVagaDoGridEmVezDeAumentaLo()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(11UL);

            RaceResult solo = TestWorld.Shared.Resolver.ResolveComplete(race);
            int gridSize = solo.OpponentTimes.Length;

            RaceResult withRival = TestWorld.Shared.Resolver.ResolveComplete(race);
            RivalOutcome outcome = _rivals.Resolve(race, _content.Rival("akira"), withRival);

            Assert.AreEqual(gridSize, withRival.OpponentTimes.Length,
                "O grid mudou de tamanho ao incluir o rival.");

            bool rivalIsOnTheGrid = false;
            for (int i = 0; i < withRival.OpponentTimes.Length; i++)
                if (withRival.OpponentTimes[i] == outcome.RivalTime) rivalIsOnTheGrid = true;

            Assert.IsTrue(rivalIsOnTheGrid, "O tempo do rival nao entrou no grid.");
        }

        /// <summary>
        /// Derrota se mede em Drift Score, nunca em posicao (D-03, GDD 3.5). E a regra
        /// que torna a licao do AKIRA possivel: ele cruza a linha na frente e perde.
        /// </summary>
        [Test]
        public void DerrotarERPorScoreENaoPorPosicao()
        {
            RaceInstance race = TestWorld.Shared.StarterRace(3UL);
            RaceResult player = TestWorld.Shared.Resolver.ResolveComplete(race);
            RivalOutcome outcome = _rivals.Resolve(race, _content.Rival("akira"), player);

            Assert.AreEqual(outcome.PlayerScore > outcome.RivalScore, outcome.PlayerWon,
                "A derrota do rival precisa sair da comparacao de score.");
        }

        // --- o elenco (13.1, 14.2) ----------------------------------------------------

        /// <summary>
        /// "Subir de tier muda a natureza: novos afixos, novos modulos, NOVOS RIVAIS"
        /// (GDD 14.2). Um tier sem rival e um tier onde a feature nao existe - e C e B
        /// juntos sao horas de jogo.
        /// </summary>
        [Test]
        public void CadaTierTemPeloMenosUmRival()
        {
            for (int tier = 0; tier <= (int)TierRank.S; tier++)
                Assert.Greater(_rivals.EligibleFor(tier).Count, 0,
                    "O tier " + (TierRank)tier + " nao tem rival nenhum.");
        }

        /// <summary>
        /// A assinatura dele e do TIER dele.
        ///
        /// Um rival tier C que dropasse uma peca tier A entregaria, numa unica corrida, o
        /// que a escada inteira da secao 14.2 existe para racionar. O drop e a peca que
        /// ele estava usando (13.1), entao a unica forma consistente de impedir isso e o
        /// rival e a assinatura viverem no mesmo tier.
        /// </summary>
        [Test]
        public void AAssinaturaDoRivalEDoTierDele()
        {
            for (int i = 0; i < _content.RivalList.Count; i++)
            {
                RivalDef rival = _content.RivalList[i];
                PartDef sig = _content.Part(rival.SignaturePartId);

                Assert.AreEqual(rival.TierIndex, (int)sig.Tier,
                    "O rival " + rival.Id + " (tier " + (TierRank)rival.TierIndex + ") dropa "
                    + sig.Id + ", que e tier " + sig.Tier + ".");
            }
        }

        [Test]
        public void CadaRivalEnsinaUmaLicao()
        {
            for (int i = 0; i < _content.RivalList.Count; i++)
                Assert.IsFalse(string.IsNullOrEmpty(_content.RivalList[i].LessonText),
                    "O rival " + _content.RivalList[i].Id + " nao ensina nada.");
        }

        /// <summary>
        /// A assinatura e SO do rival (13.1).
        ///
        /// Enquanto ela tambem caisse do loot da regiao, derrotar o dono era o caminho
        /// mais lento para a mesma peca - e a derrota deixava de ser o evento que a secao
        /// 13.1 descreve para virar um atalho opcional.
        /// </summary>
        [Test]
        public void AAssinaturaNaoCaiNoLootAleatorio()
        {
            for (int r = 0; r < _content.RivalList.Count; r++)
            {
                string sig = _content.RivalList[r].SignaturePartId;

                for (int g = 0; g < _content.RegionList.Count; g++)
                {
                    RegionDef region = _content.RegionList[g];
                    for (int p = 0; p < region.PartPool.Length; p++)
                        Assert.AreNotEqual(sig, region.PartPool[p],
                            "A regiao " + region.Id + " dropa " + sig + ", que e assinatura de "
                            + _content.RivalList[r].Id + ".");
                }
            }
        }

        // --- a licao de cada rival, medida (13.1, 3.5, 21.2) --------------------------

        /// <summary>
        /// A licao do AKIRA, em numeros.
        ///
        /// O JSON dele diz "ele chega na frente e perde no Drift Score" desde sempre. Ate
        /// aqui isso era uma frase que nada verificava. Sao duas metades e as duas
        /// precisam valer: ele ganha a CORRIDA na maioria das vezes, e mesmo assim o
        /// jogador leva o SCORE com frequencia. Se ele so ganhasse, a frase seria falsa;
        /// se so perdesse, ele nao ensinaria nada.
        /// </summary>
        [Test]
        public void AKIRAVenceACorridaEPerdeNoScore()
        {
            Duel duel = Measure(_content.Rival("akira"), "kite_130", Samples);

            Assert.Greater(duel.AheadOnTime, 0.60f,
                "AKIRA precisa cruzar a linha na frente na maioria das corridas. " + duel);
            Assert.Greater(duel.PlayerWins, 0.35f,
                "...e mesmo assim perder o score com frequencia, ou ele nao ensina nada. " + duel);
        }

        /// <summary>
        /// RYU e o espelho do AKIRA, quatro tiers depois: ele PERDE a corrida e ganha no
        /// placar. E a mesma licao lida de tras para frente, para quem ja aprendeu a
        /// primeira.
        /// </summary>
        [Test]
        public void RYUPerdeACorridaEGanhaNoScore()
        {
            Duel duel = Measure(_content.Rival("ryu"), "kanto_ae", Samples);

            Assert.Less(duel.AheadOnTime, 0.40f,
                "RYU roda com angulo extremo: ele nao deveria vencer no tempo. " + duel);
            Assert.Less(duel.PlayerWins, 0.65f,
                "...e ainda assim ser dificil de bater no score. " + duel);
        }

        /// <summary>
        /// ZERO as vezes se destroi sozinho (GDD 13.1). Isto NAO e um caso especial no
        /// codigo: sai de pFail_seg (12.3) sobre a build dele, resolvida pelo mesmo
        /// solucionador. O teste existe para provar que sai.
        /// </summary>
        [Test]
        public void ZEROAsVezesSeDestroiSozinho()
        {
            Duel duel = Measure(_content.Rival("zero"), "brute_v8", Samples);

            Assert.That(duel.RivalFailureRate, Is.InRange(0.05f, 0.60f),
                "ZERO precisa se destruir AS VEZES: nem nunca, nem sempre. " + duel);
        }

        /// <summary>
        /// Todo rival tem de ser uma BRIGA no tier dele.
        ///
        /// A faixa e larga de proposito, e a largura e a propria feature. Os rivais sao
        /// assimetricos: a licao do Ken E perder no score, entao exigir dele os mesmos
        /// 50% que se exige do AKIRA apagaria justamente o que o diferencia. O que a
        /// faixa proibe sao os dois extremos, e os dois quebram a mesma coisa - um rival
        /// que sempre perde nao vale o prompt de desafio, e um que nunca perde faz da
        /// planta de carro (tres derrotas, 13.1) uma parede em vez de um objetivo.
        ///
        /// Quem carrega a especificidade de cada um sao os testes de licao acima.
        /// </summary>
        [Test]
        public void TodoRivalEUmaBrigaNoTierDele()
        {
            string[] cars = { "kite_130", "kanto_ae", "kanto_ae", "brute_v8", "brute_v8" };

            for (int i = 0; i < _content.RivalList.Count; i++)
            {
                RivalDef rival = _content.RivalList[i];
                Duel duel = Measure(rival, cars[rival.TierIndex], Samples);

                Assert.That(duel.PlayerWins, Is.InRange(0.20f, 0.85f),
                    "O rival " + rival.Id + " esta fora da faixa de briga. " + duel);
            }
        }

        /// <summary>
        /// E a regra acima vale como ERRO DE CONTEUDO, nao como disciplina: sem isto,
        /// a proxima pessoa a editar regions.json devolve a assinatura ao pool sem que
        /// nada reclame.
        /// </summary>
        [Test]
        public void OValidadorRecusaAssinaturaNoPoolDaRegiao()
        {
            ContentDatabase db = FramedDrift.Data.GameContent.Load();
            RegionDef region = db.RegionList[0];
            string sig = db.RivalList[0].SignaturePartId;

            var pool = new string[region.PartPool.Length + 1];
            System.Array.Copy(region.PartPool, pool, region.PartPool.Length);
            pool[pool.Length - 1] = sig;
            region.PartPool = pool;

            Assert.Throws<ContentException>(() => ContentValidator.Validate(db));
        }

        // --- a ausencia (D-06, D-02) --------------------------------------------------

        /// <summary>
        /// Rival tem de acontecer TAMBEM na ausencia (D-06: nenhuma mecanica exige o
        /// jogador presente). Se o encontro so existisse online, quem farma 8 horas
        /// nunca desbloquearia o carro do rival - e a 13.2 exige o oposto: "sempre
        /// resolviveis pela automacao".
        /// </summary>
        [Test]
        public void AAusenciaEncontraRivais()
        {
            OfflineReport report = Aggregate(8f * 3600f, 12345UL);

            Assert.Greater(report.RivalEncounters, 0,
                "Oito horas de operacao sem encontrar rival nenhum.");
        }

        /// <summary>
        /// E na MESMA taxa que online, dentro dos +-8% da secao 21.2. Uma taxa offline
        /// menor seria a punicao por ausencia que D-02 proibe, escondida num sistema
        /// que ninguem pensaria em medir.
        /// </summary>
        [Test]
        public void ATaxaDeDerrotaOfflineBateComAOnline()
        {
            Duel online = Measure(_content.Rival("akira"), "kite_130", Samples);

            long encounters = 0, defeats = 0;
            for (ulong seed = 1UL; seed <= 40UL; seed++)
            {
                OfflineReport report = Aggregate(8f * 3600f, seed);
                encounters += report.RivalEncounters;
                defeats += report.RivalDefeats;
            }

            Assert.Greater(encounters, 0L, "Sem encontros nao ha o que comparar.");

            double offlineRate = defeats / (double)encounters;
            double delta = System.Math.Abs(offlineRate - online.PlayerWins);

            Assert.Less(delta, 0.08,
                "Derrota offline " + (offlineRate * 100).ToString("0.0")
                + "% x online " + (online.PlayerWins * 100).ToString("0.0") + "%.");
        }

        /// <summary>A assinatura dele tambem cai na ausencia - uma por derrota.</summary>
        [Test]
        public void AAusenciaEntregaAAssinaturaDeQuemDerrotou()
        {
            for (ulong seed = 1UL; seed <= 20UL; seed++)
            {
                OfflineReport report = Aggregate(8f * 3600f, seed);
                if (report.RivalDefeats <= 0) continue;

                Assert.AreEqual(report.RivalDefeats, report.RivalSignatureDrops.Count,
                    "Uma assinatura por derrota, tambem offline.");
                return;
            }

            Assert.Fail("Nenhuma derrota de rival em 20 ausencias de 8 h.");
        }

        private OfflineReport Aggregate(float seconds, ulong seed)
        {
            RaceInstance prototype = TestWorld.Shared.StarterRace(seed);
            var offline = new OfflineAggregator(_content, TestWorld.Shared.Resolver);

            return offline.Aggregate(prototype, seconds, 0, OfflineRules.Default, 0f, seed);
        }

        // --- medicao ------------------------------------------------------------------

        private const int Samples = 400;

        /// <summary>
        /// As mesmas builds por tier do TrackLadderTests: e contra ELAS que a escada de
        /// pista foi calibrada, entao e contra elas que o rival tem de ser medido. Medir
        /// com outra build mediria outro jogo.
        /// </summary>
        private static readonly string[][] TierBuilds =
        {
            new[] { "eng_stock", "tur_stock", "trn_stock", "dif_street",
                    "sus_stock", "tir_street", "brk_stock", "aer_stock" },
            new[] { "eng_forged", "tur_twin", "trn_dogbox", "dif_clutch",
                    "sus_adjustable", "tir_semislick", "brk_slotted", "aer_widebody" },
            new[] { "eng_stroker", "tur_bigframe", "trn_sequential", "dif_comp",
                    "sus_pillowball", "tir_racing", "brk_bigbrake", "aer_carbon" },
            new[] { "eng_billet", "tur_compound", "trn_dct", "dif_spool",
                    "sus_multilink", "tir_racing", "brk_carbon", "aer_active" },
            new[] { "eng_race", "tur_antilag", "trn_straightcut", "dif_active",
                    "sus_damper", "tir_slick", "brk_race", "aer_ground" },
        };

        private struct Duel
        {
            public float AheadOnTime;
            public float PlayerWins;
            public float RivalFailureRate;
            public double MeanPlayerScore;
            public double MeanRivalScore;

            public override string ToString()
            {
                return "\n  na frente no tempo " + (AheadOnTime * 100f).ToString("0.0")
                       + "%   jogador vence no score " + (PlayerWins * 100f).ToString("0.0")
                       + "%   falhas do rival " + (RivalFailureRate * 100f).ToString("0.0")
                       + "%\n  score medio: jogador " + MeanPlayerScore.ToString("N0")
                       + "   rival " + MeanRivalScore.ToString("N0");
            }
        }

        private Duel Measure(RivalDef rival, string carId, int samples)
        {
            TrackDef track = EntryTrack(rival.TierIndex);
            RolledPart[] build = BuildFor(rival.TierIndex);

            int ahead = 0, wins = 0, failures = 0;
            double playerScore = 0.0, rivalScore = 0.0;

            for (int i = 0; i < samples; i++)
            {
                RaceInstance race = TestWorld.Shared.Race(
                    carId, track.Id, build, TuningSetup.Neutral, DriftStyle.Balanced,
                    TimeOfDay.Day, Weather.Clear, (ulong)(i + 1));

                RaceResult player = TestWorld.Shared.Resolver.ResolveComplete(race);
                RivalOutcome outcome = _rivals.Resolve(race, rival, player);

                if (outcome.RivalFinishedAhead) ahead++;
                if (outcome.PlayerWon) wins++;
                if (outcome.RivalFailures > 0) failures++;

                playerScore += outcome.PlayerScore;
                rivalScore += outcome.RivalScore;
            }

            return new Duel
            {
                AheadOnTime = ahead / (float)samples,
                PlayerWins = wins / (float)samples,
                RivalFailureRate = failures / (float)samples,
                MeanPlayerScore = playerScore / samples,
                MeanRivalScore = rivalScore / samples,
            };
        }

        private static RolledPart[] BuildFor(int tier)
        {
            string[] ids = TierBuilds[tier];
            var parts = new RolledPart[8];
            for (int i = 0; i < ids.Length; i++)
            {
                RolledPart p = TestWorld.Shared.Part(ids[i]);
                parts[(int)p.Slot] = p;
            }
            return parts;
        }

        private static TrackDef EntryTrack(int tier)
        {
            TrackDef best = null;
            for (int i = 0; i < TestWorld.Shared.Content.TrackList.Count; i++)
            {
                TrackDef track = TestWorld.Shared.Content.TrackList[i];
                if ((int)track.Tier != tier) continue;
                if (best == null || track.ReputationRequired < best.ReputationRequired) best = track;
            }
            return best;
        }
    }
}
