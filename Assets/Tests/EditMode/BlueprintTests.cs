// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 10.2, 10.6, 15.1, 17.2, 20.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// Plantas (GDD 10.6): caem em pedacos, fecham em 5, e a bancada troca planta + scrap
    /// por uma peca ROLADA da raridade escolhida.
    ///
    /// E isso que separa a bancada da loja: a loja vende o piso de fabrica, sem afixo; a
    /// planta e o caminho deterministico para a peca que o RNG nao deu.
    /// </summary>
    public sealed class BlueprintTests
    {
        private ContentDatabase _content;
        private SaveData _save;
        private EconomyLedger _economy;
        private InventoryManager _inventory;
        private Crafting _crafting;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;
            _save = new SaveData();
            _economy = new EconomyLedger(_save, _content.Balance);
            _inventory = new InventoryManager(_save, TestWorld.Shared.Resolver.Loot, _economy);
            _crafting = new Crafting(_save, _content, _inventory,
                                     TestWorld.Shared.Resolver.Loot, _economy);
        }

        // --- pedacos ---------------------------------------------------------------

        [Test]
        public void CincoPedacosFechamUmaPlanta()
        {
            int need = _crafting.FragmentsPerBlueprint;
            Assert.AreEqual(5, need, "GDD 10.6 fala em quintos: 1/5, 2/5...");

            for (int i = 1; i < need; i++)
            {
                Assert.IsFalse(_crafting.AddBlueprintFragment("eng_sport"),
                    "O pedaco " + i + " nao pode fechar a planta.");
                Assert.AreEqual(i, _crafting.FragmentsOf("eng_sport"));
                Assert.IsFalse(_crafting.HasBlueprint("eng_sport"));
            }

            Assert.IsTrue(_crafting.AddBlueprintFragment("eng_sport"), "O quinto fecha.");
            Assert.IsTrue(_crafting.HasBlueprint("eng_sport"));
            Assert.AreEqual(0, _crafting.FragmentsOf("eng_sport"), "Fechou; nao sobra pedaco.");
        }

        [Test]
        public void PedacosDePecasDiferentesNaoSeMisturam()
        {
            _crafting.AddBlueprintFragment("eng_sport");
            _crafting.AddBlueprintFragment("brk_sport");

            Assert.AreEqual(1, _crafting.FragmentsOf("eng_sport"));
            Assert.AreEqual(1, _crafting.FragmentsOf("brk_sport"));
        }

        [Test]
        public void PlantaCompletaNaoAcumulaUmSegundoLote()
        {
            Complete("eng_sport");

            for (int i = 0; i < 10; i++) _crafting.AddBlueprintFragment("eng_sport");
            Assert.AreEqual(0, _crafting.FragmentsOf("eng_sport"),
                "Planta pendente nao pode virar moeda acumulavel.");
        }

        [Test]
        public void PedacoDePecaInexistenteEIgnorado()
        {
            Assert.IsFalse(_crafting.AddBlueprintFragment("nao_existe"));
            Assert.IsFalse(_crafting.AddBlueprintFragment(null));
            Assert.AreEqual(0, _save.Progress.BlueprintFragments.Count);
        }

        [Test]
        public void FecharUmaPlantaPublicaOAviso()
        {
            string completed = null;
            System.Action<BlueprintCompleted> handler = e => completed = e.PartId;
            EventBus.Subscribe(handler);

            try
            {
                Complete("eng_sport");
                Assert.AreEqual("eng_sport", completed);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        // --- a bancada -------------------------------------------------------------

        [Test]
        public void CraftConsomeAPlantaEEntregaPecaRolada()
        {
            Complete("eng_sport");
            _economy.AddScrap(_crafting.CraftCost("eng_sport", Rarity.Rare));

            PartInstance made = _crafting.Craft("eng_sport", Rarity.Rare, 5);

            Assert.IsNotNull(made);
            Assert.AreEqual("eng_sport", made.Rolled.BaseId);
            Assert.IsFalse(_crafting.HasBlueprint("eng_sport"), "A planta e consumida.");
            Assert.AreNotEqual(0UL, made.Rolled.Seed,
                "Craft entrega peca ROLADA - seed 0 e a peca de fabrica que a LOJA vende.");
        }

        [Test]
        public void CraftSemPlantaNaoCobra()
        {
            _economy.AddScrap(99999L);
            Assert.IsNull(_crafting.Craft("eng_sport", Rarity.Rare, 5));
            Assert.AreEqual(99999L, _economy.Scrap);
        }

        [Test]
        public void CraftSemScrapNaoConsomeAPlanta()
        {
            Complete("eng_sport");

            Assert.IsNull(_crafting.Craft("eng_sport", Rarity.Rare, 5));
            Assert.IsTrue(_crafting.HasBlueprint("eng_sport"),
                "Craft recusado nao pode queimar a planta - ela custou 5 corridas premiadas.");
        }

        [Test]
        public void CraftNaoProduzRaridadeAcimaDoTier()
        {
            // No tier D, rarityWeights zera Epic e Legendary (tiers.json). A bancada era o
            // unico caminho capaz de furar isso.
            _save.Progress.TierIndex = (int)TierRank.D;
            Assert.AreEqual(Rarity.Rare, _crafting.MaxCraftableRarity());

            Complete("eng_sport");
            _economy.AddScrap(999999L);

            PartInstance made = _crafting.Craft("eng_sport", Rarity.Legendary, 999);

            Assert.IsNotNull(made);
            Assert.AreEqual(Rarity.Rare, made.Rarity, "A raridade tem de ser grampeada ao tier.");
            Assert.LessOrEqual(made.Rolled.ItemLevel, _content.Tier(TierRank.D).ItemLevelCap,
                "O iLvl tem de ser grampeado ao teto do tier.");
        }

        [Test]
        public void OTetoDeRaridadeSobeComOTier()
        {
            _save.Progress.TierIndex = (int)TierRank.D;
            Rarity atD = _crafting.MaxCraftableRarity();

            _save.Progress.TierIndex = (int)TierRank.B;
            Rarity atB = _crafting.MaxCraftableRarity();

            Assert.Greater((int)atB, (int)atD, "Subir de tier tem de abrir raridade nova na bancada.");
        }

        // --- o drop ----------------------------------------------------------------

        [Test]
        public void PedacosCaemCorrendo()
        {
            int fragments = CountFragmentsOverRaces(1500);
            Assert.Greater(fragments, 0, "Nenhum pedaco caiu em 1500 corridas.");
        }

        [Test]
        public void OAlvoDoPedacoRespeitaOTierDaCorrida()
        {
            RaceInstance race = StarterRace(1UL);
            race.TierIndex = (int)TierRank.D;

            for (ulong seed = 1UL; seed <= 1500UL; seed++)
            {
                race.Seed = seed;
                string[] ids = TestWorld.Shared.Resolver.ResolveComplete(race).Rewards.BlueprintIds;

                for (int i = 0; i < ids.Length; i++)
                    Assert.AreEqual(TierRank.D, _content.Part(ids[i]).Tier,
                        "Corrida tier D deu planta de " + ids[i] + ".");
            }
        }

        /// <summary>
        /// A planta existe para cortar a cauda do RNG (10.6). Se ela apontasse para a peca
        /// comum, repetiria o que o loot ja da e nao cortaria cauda nenhuma.
        /// </summary>
        [Test]
        public void OAlvoPrefereAPecaQueRaramenteCai()
        {
            var hits = new Dictionary<string, int>();
            RaceInstance race = StarterRace(1UL);

            for (ulong seed = 1UL; seed <= 3000UL; seed++)
            {
                race.Seed = seed;
                string[] ids = TestWorld.Shared.Resolver.ResolveComplete(race).Rewards.BlueprintIds;

                for (int i = 0; i < ids.Length; i++)
                {
                    int n;
                    hits[ids[i]] = hits.TryGetValue(ids[i], out n) ? n + 1 : 1;
                }
            }

            Assert.Greater(hits.Count, 0, "Nenhum pedaco caiu.");

            // Ambas sao vendaveis; o que as separa e so o dropWeight: 0,5 contra 1,0.
            int rare;
            int common;
            if (!hits.TryGetValue("dif_lsd2way", out rare)) rare = 0;
            if (!hits.TryGetValue("eng_street", out common)) common = 0;

            Assert.Greater(rare, common,
                "A planta do LSD 2-Way (dropWeight 0,5) tem de cair mais que a do Comando de Rua (1,0).");

            // Peca de serie e GRATIS e ja vem montada no carro: uma planta dela seria um
            // pedaco de progresso gasto em nada.
            foreach (KeyValuePair<string, int> hit in hits)
                Assert.Greater(_content.Part(hit.Key).BuyCost, 0L,
                    "Caiu planta da peca de serie " + hit.Key + ".");
        }

        /// <summary>
        /// O fluxo de EVENTOS, nunca o de loot (GDD 20.3). Se a planta consumisse o fluxo
        /// de loot, ligar esta feature teria mudado toda a sequencia de drops ja existente.
        /// </summary>
        [Test]
        public void APlantaNaoDeslocaASequenciaDeDrops()
        {
            RaceInstance race = StarterRace(4242UL);
            RaceResult result = TestWorld.Shared.Resolver.ResolveComplete(race);

            var lootOnly = new RngStreams(4242UL).Loot;
            PartDrop[] expected = TestWorld.Shared.Resolver.Loot.RollDrops(
                race, result, result.Rewards.DriftScore, 0f, lootOnly);

            Assert.AreEqual(expected.Length, result.Rewards.Drops.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i].BaseId, result.Rewards.Drops[i].BaseId,
                    "O drop mudou: a planta esta consumindo o fluxo de loot.");
        }

        /// <summary>
        /// D-02 vale para plantas tambem: a ausencia junta pedacos na MESMA taxa por
        /// corrida que o jogo acordado. Um jogador que deixa o jogo rodando a noite nao
        /// pode acordar mais longe da planta do que se tivesse assistido.
        /// </summary>
        [Test]
        public void OfflineJuntaPedacosNaMesmaTaxaQueOnline()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            const int races = 2000;
            double onlineRate = CountFragmentsOverRaces(races) / (double)races;

            OfflineRules rules = OfflineRules.Default;
            rules.AutoRepair = true;

            int offlineFragments = 0;
            int offlineRaces = 0;

            for (int i = 0; i < 30; i++)
            {
                OfflineReport report = aggregator.Aggregate(
                    world.StarterRace(9001UL), 4f * 3600f, 0, rules, 0f, (ulong)(i * 977 + 5));

                offlineFragments += report.BlueprintFragments.Count;
                offlineRaces += report.Races;
            }

            Assert.Greater(offlineRaces, 0);
            double offlineRate = offlineFragments / (double)offlineRaces;

            Assert.That(offlineRate, Is.EqualTo(onlineRate).Within(0.08 * onlineRate),
                "Pedacos por corrida: online " + onlineRate.ToString("0.0000")
                + " x offline " + offlineRate.ToString("0.0000"));
        }

        [Test]
        public void OfflineNaoJuntaPlantaAcimaDoTier()
        {
            TestWorld world = TestWorld.Shared;
            var aggregator = new OfflineAggregator(world.Content, world.Resolver);

            OfflineRules rules = OfflineRules.Default;
            rules.AutoRepair = true;

            OfflineReport report = aggregator.Aggregate(
                world.StarterRace(4242UL), 6f * 3600f, 0, rules, 0f, 777UL);

            for (int i = 0; i < report.BlueprintFragments.Count; i++)
                Assert.AreEqual(TierRank.D, _content.Part(report.BlueprintFragments[i]).Tier,
                    "A ausencia numa pista tier D juntou planta acima do tier.");
        }

        /// <summary>
        /// O ritmo. A GDD 10.6 pede um "objetivo de longo prazo", e isso e uma faixa, nao
        /// um numero: rapido demais e a bancada substitui o loot; devagar demais e ela
        /// nunca acontece e vira codigo morto - que foi exatamente o estado anterior.
        ///
        /// Medido, nao estimado. O numero atual esta no README.
        /// </summary>
        [Test]
        public void APrimeiraPlantaFechaNumRitmoDeObjetivoDeLongoPrazo()
        {
            RaceInstance race = StarterRace(1UL);
            int racesUntilFirst = 0;

            for (int i = 1; i <= 4000; i++)
            {
                race.Seed = (ulong)i;
                string[] ids = TestWorld.Shared.Resolver.ResolveComplete(race).Rewards.BlueprintIds;

                bool done = false;
                for (int k = 0; k < ids.Length; k++)
                    if (_crafting.AddBlueprintFragment(ids[k])) done = true;

                if (done) { racesUntilFirst = i; break; }
            }

            Assert.Greater(racesUntilFirst, 0, "Nenhuma planta fechou em 4000 corridas.");

            float minutes = racesUntilFirst * (TestWorld.Shared.Content.Balance.InterRaceDelaySeconds + 70f) / 60f;
            TestContext.WriteLine("Primeira planta: " + racesUntilFirst
                                  + " corridas (~" + minutes.ToString("0") + " min de jogo ativo)");

            Assert.That(racesUntilFirst, Is.InRange(40, 300),
                "Primeira planta em " + racesUntilFirst + " corridas.");
        }

        /// <summary>
        /// As assinaturas de rival (GDD 13.1) sao so-bancada: nao tem preco, entao a
        /// planta e o UNICO caminho ate elas. Se o sorteio nunca as escolhesse, quatro
        /// pecas ficariam no JSON sem jamais chegar a um jogador.
        /// </summary>
        [Test]
        public void ACorridaDeTierAltoSorteiaPlantaDeAssinatura()
        {
            TrackDef top = null;
            for (int i = 0; i < _content.TrackList.Count; i++)
                if (_content.TrackList[i].Tier == TierRank.S) { top = _content.TrackList[i]; break; }

            Assert.IsNotNull(top, "Precisa de uma pista tier S.");

            RaceInstance race = TestWorld.Shared.Race(
                "kite_130", top.Id, TestWorld.Shared.FactoryBuild(), TuningSetup.Neutral,
                DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, 1UL);

            bool sawSignature = false;
            for (ulong seed = 1UL; seed <= 3000UL && !sawSignature; seed++)
            {
                race.Seed = seed;
                string[] ids = TestWorld.Shared.Resolver.ResolveComplete(race).Rewards.BlueprintIds;

                for (int i = 0; i < ids.Length; i++)
                    if (ids[i].Contains("_sig_")) sawSignature = true;
            }

            Assert.IsTrue(sawSignature,
                "Nenhuma planta de assinatura em 3000 corridas tier S - elas seriam inalcancaveis.");
        }

        private int CountFragmentsOverRaces(int races)
        {
            RaceInstance race = StarterRace(1UL);
            int n = 0;

            for (int i = 0; i < races; i++)
            {
                race.Seed = (ulong)(i + 1);
                n += TestWorld.Shared.Resolver.ResolveComplete(race).Rewards.BlueprintIds.Length;
            }
            return n;
        }

        private static RaceInstance StarterRace(ulong seed)
        {
            return TestWorld.Shared.StarterRace(seed);
        }

        private void Complete(string partId)
        {
            for (int i = 0; i < _crafting.FragmentsPerBlueprint; i++)
                _crafting.AddBlueprintFragment(partId);

            Assert.IsTrue(_crafting.HasBlueprint(partId));
        }
    }
}
