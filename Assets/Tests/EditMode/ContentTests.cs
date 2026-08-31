// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 10, 11.3, 20.2, 22.1
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O escopo do MVP da secao 22.1 e um CONTRATO (risco R3: "explosao de escopo").
    /// Estes testes falham quando o conteudo cresce alem dele sem uma decisao explicita.
    /// </summary>
    public class ContentTests
    {
        [Test]
        public void Content_LoadsAndValidates()
        {
            ContentDatabase db = TestWorld.Shared.Content;

            Assert.IsNotNull(db.Balance, "balance.json precisa produzir constantes.");
            Assert.Greater(db.CarList.Count, 0);
            Assert.Greater(db.TrackList.Count, 0);
        }

        [Test]
        public void MvpScope_MatchesTheContract()
        {
            ContentDatabase db = TestWorld.Shared.Content;

            Assert.AreEqual(3, db.CarList.Count, "GDD 22.1: o MVP tem 3 carros.");

            // A GDD 22.1 fecha o MVP em 20 bases, e essas 20 continuam sendo o tier D
            // inteiro - o contrato original esta preservado na primeira linha. As 16
            // seguintes sao a escada de tier (C e B), que o MVP nao previa porque nao
            // previa que `Progress.TierIndex` fosse subir. A 22.1 precisa ser atualizada.
            Assert.AreEqual(20, CountAtTier(db, TierRank.D), "GDD 22.1: 20 bases no tier D.");
            Assert.AreEqual(8, CountAtTier(db, TierRank.C), "Uma base tier C por slot.");
            Assert.AreEqual(8, CountAtTier(db, TierRank.B), "Uma base tier B por slot.");
            Assert.AreEqual(36, db.PartList.Count, "20 (D) + 8 (C) + 8 (B).");
            Assert.AreEqual(12, db.AffixList.Count, "GDD 22.1: o MVP tem 12 afixos.");
            Assert.AreEqual(6, db.Modules.Count, "GDD 22.1: o MVP tem 6 modulos.");
            // Mesma historia das pecas: as 3 pistas da GDD 22.1 continuam sendo o tier D
            // inteiro, e as 4 novas sao a escada de tier. Sem pista acima de D nenhuma
            // corrida saia do tier D, e rewardScale, itemLevelCap e o corte de tier do
            // loot ficavam todos inalcancaveis.
            Assert.AreEqual(3, CountTracksAtTier(db, TierRank.D), "GDD 22.1: 3 pistas no tier D.");
            Assert.AreEqual(2, CountTracksAtTier(db, TierRank.C), "2 pistas no tier C.");
            Assert.AreEqual(2, CountTracksAtTier(db, TierRank.B), "2 pistas no tier B.");
            Assert.AreEqual(7, db.TrackList.Count, "3 (D) + 2 (C) + 2 (B).");
            Assert.AreEqual(1, db.RegionList.Count, "GDD 22.1: o MVP tem 1 regiao.");
            Assert.AreEqual(1, db.RivalList.Count, "GDD 22.1: o MVP tem 1 rival.");
            Assert.AreEqual(8, System.Enum.GetValues(typeof(PartSlot)).Length, "GDD 9.1: oito slots.");
            Assert.AreEqual(5, System.Enum.GetValues(typeof(Rarity)).Length,
                "GDD 10.2: cinco raridades no lancamento. Prototype e Mythic ficam de fora.");
        }

        private static int CountAtTier(ContentDatabase db, TierRank tier)
        {
            int n = 0;
            for (int i = 0; i < db.PartList.Count; i++)
                if (db.PartList[i].Tier == tier) n++;
            return n;
        }

        private static int CountTracksAtTier(ContentDatabase db, TierRank tier)
        {
            int n = 0;
            for (int i = 0; i < db.TrackList.Count; i++)
                if (db.TrackList[i].Tier == tier) n++;
            return n;
        }

        [Test]
        public void EveryTrack_RespectsAdjacencyRules()
        {
            ContentDatabase db = TestWorld.Shared.Content;

            for (int t = 0; t < db.TrackList.Count; t++)
            {
                TrackDef track = db.TrackList[t];

                // GDD 5.1: uma corrida curta tem 18-30 segmentos.
                Assert.That(track.ModuleIds.Length, Is.InRange(18, 30),
                    "Pista " + track.Id + " tem " + track.ModuleIds.Length + " segmentos.");

                for (int i = 1; i < track.ModuleIds.Length; i++)
                {
                    ModuleDef previous = db.Module(track.ModuleIds[i - 1]);
                    if (previous.AllowedNext.Length == 0) continue;

                    CollectionAssert.Contains(previous.AllowedNext, track.ModuleIds[i],
                        "Pista " + track.Id + ": " + track.ModuleIds[i] + " nao pode seguir " + previous.Id);
                }
            }
        }

        [Test]
        public void Generator_Produces200ValidTracks()
        {
            TestWorld world = TestWorld.Shared;
            var generator = new TrackGenerator(world.Content);
            var skeletons = (TrackSkeleton[])System.Enum.GetValues(typeof(TrackSkeleton));

            for (int i = 0; i < 200; i++)
            {
                TrackDef track = generator.Generate(new TrackGenerator.Request
                {
                    RegionId = world.Content.RegionList[0].Id,
                    Tier = TierRank.D,
                    SegmentCount = 18 + (i % 13),
                    Skeleton = skeletons[i % skeletons.Length],
                    Seed = (ulong)i * 6364136223846793005UL + 1442695040888963407UL,
                });

                Assert.Greater(track.ModuleIds.Length, 11, "Pista gerada curta demais: " + track.Id);
                Assert.Greater(track.BaseTimeSeconds, 0f, "Tempo de referencia invalido: " + track.Id);

                for (int s = 1; s < track.ModuleIds.Length; s++)
                {
                    ModuleDef previous = world.Content.Module(track.ModuleIds[s - 1]);
                    if (previous.AllowedNext.Length == 0) continue;

                    CollectionAssert.Contains(previous.AllowedNext, track.ModuleIds[s],
                        "Pista gerada " + track.Id + " viola adjacencia no segmento " + s);
                }
            }
        }

        [Test]
        public void SameSeed_MaterializesSamePart()
        {
            TestWorld world = TestWorld.Shared;
            var drop = new PartDrop
            {
                BaseId = "dif_lsd2way",
                Rarity = Rarity.Rare,
                ItemLevel = 6,
                Seed = 8675309UL,
            };

            RolledPart a = world.Resolver.Loot.Materialize(drop);
            RolledPart b = world.Resolver.Loot.Materialize(drop);

            // O save guarda a SEED, nao os valores (GDD 10.1). Se a materializacao nao
            // fosse deterministica, o inventario mudaria a cada carga.
            Assert.AreEqual(a.Affixes.Length, b.Affixes.Length);
            for (int i = 0; i < a.Affixes.Length; i++)
            {
                Assert.AreEqual(a.Affixes[i].AffixId, b.Affixes[i].AffixId);
                Assert.AreEqual(a.Affixes[i].Value, b.Affixes[i].Value);
            }
            Assert.AreEqual(a.HasPassive, b.HasPassive);
        }

        [Test]
        public void RarityDeterminesAffixCount()
        {
            TestWorld world = TestWorld.Shared;
            var balance = world.Content.Balance;

            var rarities = (Rarity[])System.Enum.GetValues(typeof(Rarity));
            for (int r = 0; r < rarities.Length; r++)
            {
                RolledPart part = world.Resolver.Loot.Materialize(new PartDrop
                {
                    BaseId = "tur_sport",
                    Rarity = rarities[r],
                    ItemLevel = 5,
                    Seed = (ulong)(r * 7919 + 13),
                });

                Assert.AreEqual(balance.AffixCount(rarities[r]), part.Affixes.Length,
                    "GDD 10.2: " + rarities[r] + " deve rolar " + balance.AffixCount(rarities[r]) + " afixos.");
            }
        }

        [Test]
        public void PassivesOnlyAppearOnRareAndAbove()
        {
            TestWorld world = TestWorld.Shared;

            for (ulong seed = 0; seed < 300; seed++)
            {
                RolledPart common = world.Resolver.Loot.Materialize(new PartDrop
                { BaseId = "sus_coilover", Rarity = Rarity.Common, ItemLevel = 4, Seed = seed });
                RolledPart uncommon = world.Resolver.Loot.Materialize(new PartDrop
                { BaseId = "sus_coilover", Rarity = Rarity.Uncommon, ItemLevel = 4, Seed = seed });

                // GDD 10.2: passiva so a partir de Rare.
                Assert.IsFalse(common.HasPassive, "Common nao pode ter passiva (seed " + seed + ")");
                Assert.IsFalse(uncommon.HasPassive, "Uncommon nao pode ter passiva (seed " + seed + ")");
            }
        }

        [Test]
        public void SameBase_ProducesVariedAffixCombinations()
        {
            TestWorld world = TestWorld.Shared;
            var seen = new HashSet<string>();

            for (ulong seed = 0; seed < 200; seed++)
            {
                RolledPart part = world.Resolver.Loot.Materialize(new PartDrop
                { BaseId = "tur_sport", Rarity = Rarity.Rare, ItemLevel = 6, Seed = seed * 7919 + 13 });

                var key = "";
                for (int i = 0; i < part.Affixes.Length; i++) key += part.Affixes[i].AffixId + "|";
                seen.Add(key);
            }

            // "Duas pecas do mesmo tipo e raridade servindo a builds completamente
            // diferentes - este e o motor do loot chase" (GDD 10.3).
            Assert.Greater(seen.Count, 4,
                "A mesma base precisa gerar combinacoes variadas. Distintas: " + seen.Count);
        }

        [Test]
        public void SalvageScalesWithRarity()
        {
            TestWorld world = TestWorld.Shared;

            long previous = 0L;
            var rarities = (Rarity[])System.Enum.GetValues(typeof(Rarity));

            for (int i = 0; i < rarities.Length; i++)
            {
                long scrap = world.Resolver.Loot.SalvageValue(
                    new PartDrop { Rarity = rarities[i], ItemLevel = 1 });

                Assert.Greater(scrap, previous, "Salvage precisa crescer com a raridade (GDD 10.6).");
                previous = scrap;
            }
        }

        [Test]
        public void RaceFitsThePerformanceBudget()
        {
            TestWorld world = TestWorld.Shared;
            RaceInstance race = world.StarterRace(1UL);

            for (int i = 0; i < 500; i++) world.Resolver.ResolveComplete(race);   // aquece o JIT

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const int races = 10000;
            for (ulong seed = 0; seed < races; seed++)
            {
                race.Seed = seed;
                world.Resolver.ResolveComplete(race);
            }
            stopwatch.Stop();

            double msPerRace = stopwatch.Elapsed.TotalMilliseconds / races;

            Assert.Less(msPerRace, (double)world.Content.Balance.SimRaceBudgetMs,
                "GDD 20.4: uma corrida deve resolver em menos de 0,3 ms. Medido: "
                + msPerRace.ToString("0.0000") + " ms");

            // Criterio de saida da Fase 3: 10.000 corridas em menos de 5 s.
            Assert.Less(stopwatch.Elapsed.TotalSeconds, 5.0,
                "Criterio da Fase 3: 10.000 corridas em menos de 5 s. Medido: "
                + stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " s");
        }
    }
}
