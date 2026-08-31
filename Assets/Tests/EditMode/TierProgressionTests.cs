// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 10.2, 14.2, 9.3, 9.4
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Progression;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// A escada de tier: promocao por reputacao, corte do loot por tier e a forma das
    /// bases de C e B.
    ///
    /// Antes destes testes o `TierIndex` do save nunca subia - `tiers.json` trazia
    /// `reputationRequired` desde sempre e nada lia o campo. Nao aparecia porque todo o
    /// conteudo tambem era tier D.
    /// </summary>
    public sealed class TierProgressionTests
    {
        private ContentDatabase _content;
        private SaveData _save;
        private ReputationSystem _reputation;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;
            _save = new SaveData();
            _reputation = new ReputationSystem(_save, _content);
        }

        // --- promocao ---------------------------------------------------------------

        [Test]
        public void ComecaNoTierD()
        {
            _save.Reputation = 0;
            _reputation.Evaluate();

            Assert.AreEqual((int)TierRank.D, _save.Progress.TierIndex);
        }

        [Test]
        public void ReputacaoPromoveAoTierC()
        {
            _save.Reputation = _content.Tier(TierRank.C).ReputationRequired;
            _reputation.Evaluate();

            Assert.AreEqual((int)TierRank.C, _save.Progress.TierIndex);
        }

        [Test]
        public void UmPontoAbaixoDoLimiarNaoPromove()
        {
            _save.Reputation = _content.Tier(TierRank.C).ReputationRequired - 1;
            _reputation.Evaluate();

            Assert.AreEqual((int)TierRank.D, _save.Progress.TierIndex);
        }

        [Test]
        public void PromocaoPulaVariosTiersDeUmaVez()
        {
            // Voltar de uma ausencia longa pode cruzar dois limiares de uma vez.
            _save.Reputation = _content.Tier(TierRank.B).ReputationRequired;
            List<UnlockGranted> granted = _reputation.Evaluate();

            Assert.AreEqual((int)TierRank.B, _save.Progress.TierIndex);

            int tiersGranted = 0;
            for (int i = 0; i < granted.Count; i++)
                if (granted[i].Kind == "tier") tiersGranted++;

            Assert.AreEqual(2, tiersGranted, "Cruzar C e B tem de anunciar os DOIS.");
        }

        [Test]
        public void PromocaoNaoSeRepete()
        {
            _save.Reputation = _content.Tier(TierRank.C).ReputationRequired;
            _reputation.Evaluate();

            List<UnlockGranted> again = _reputation.Evaluate();
            for (int i = 0; i < again.Count; i++)
                Assert.AreNotEqual("tier", again[i].Kind, "Tier nao pode ser anunciado duas vezes.");
        }

        [Test]
        public void TierNuncaCai()
        {
            _save.Reputation = _content.Tier(TierRank.B).ReputationRequired;
            _reputation.Evaluate();
            Assert.AreEqual((int)TierRank.B, _save.Progress.TierIndex);

            // Perder reputacao nao pode tirar da vitrine pecas ja compradas. A unica
            // descida e o prestigio, que reinicia a temporada de proposito (GDD 14.5).
            _save.Reputation = 0;
            _reputation.Evaluate();

            Assert.AreEqual((int)TierRank.B, _save.Progress.TierIndex);
        }

        // --- corte do loot por tier --------------------------------------------------

        [Test]
        public void CorridaTierDNuncaDropaPecaAcimaDeD()
        {
            AssertDropsRespectTier(TierRank.D);
        }

        [Test]
        public void CorridaTierCNuncaDropaPecaAcimaDeC()
        {
            AssertDropsRespectTier(TierRank.C);
        }

        [Test]
        public void CorridaTierBPodeDroparAsTresFaixas()
        {
            var seen = new HashSet<TierRank>();
            RaceInstance race = RaceAtTier(TierRank.B);

            for (ulong seed = 1UL; seed <= 4000UL; seed++)
            {
                PartDrop[] drops = RollDrops(race, seed);
                for (int i = 0; i < drops.Length; i++)
                    seen.Add(_content.Part(drops[i].BaseId).Tier);
            }

            Assert.IsTrue(seen.Contains(TierRank.D), "O tier B ainda dropa D.");
            Assert.IsTrue(seen.Contains(TierRank.C), "O tier B tem de dropar C.");
            Assert.IsTrue(seen.Contains(TierRank.B), "O tier B tem de dropar B.");
        }

        private void AssertDropsRespectTier(TierRank tier)
        {
            RaceInstance race = RaceAtTier(tier);

            for (ulong seed = 1UL; seed <= 4000UL; seed++)
            {
                PartDrop[] drops = RollDrops(race, seed);
                for (int i = 0; i < drops.Length; i++)
                {
                    PartDef def = _content.Part(drops[i].BaseId);
                    Assert.LessOrEqual((int)def.Tier, (int)tier,
                        "Corrida tier " + tier + " dropou " + def.Id + " (tier " + def.Tier + ").");
                }
            }
        }

        private PartDrop[] RollDrops(RaceInstance race, ulong seed)
        {
            race.Seed = seed;
            RaceResult result = TestWorld.Shared.Resolver.ResolveComplete(race);
            return result.Rewards.Drops;
        }

        private RaceInstance RaceAtTier(TierRank tier)
        {
            RaceInstance race = TestWorld.Shared.Race(
                "kite_130", "city_loop", TestWorld.Shared.FactoryBuild(), TuningSetup.Neutral,
                DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, 1UL);

            race.TierIndex = (int)tier;
            return race;
        }

        // --- a forma das bases novas -------------------------------------------------

        [Test]
        public void CadaSlotTemPecaEmDCEB()
        {
            for (int slot = 0; slot < 8; slot++)
            {
                AssertSlotHasTier((PartSlot)slot, TierRank.D);
                AssertSlotHasTier((PartSlot)slot, TierRank.C);
                AssertSlotHasTier((PartSlot)slot, TierRank.B);
            }
        }

        private void AssertSlotHasTier(PartSlot slot, TierRank tier)
        {
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef def = _content.PartList[i];
                if (def.Slot == slot && def.Tier == tier) return;
            }
            Assert.Fail("O slot " + slot + " nao tem nenhuma base tier " + tier + ".");
        }

        [Test]
        public void PecaDeTierMaiorCustaMaisQueTodaPecaDeTierMenor()
        {
            // A vitrine e ordenada por preco e rotulada por tier. Se um tier C fosse mais
            // barato que um tier D, a escada leria como desconto, nao como progressao.
            for (int slot = 0; slot < 8; slot++)
            {
                long maxD = MaxBuyCost((PartSlot)slot, TierRank.D);
                long minC = MinBuyCost((PartSlot)slot, TierRank.C);
                long maxC = MaxBuyCost((PartSlot)slot, TierRank.C);
                long minB = MinBuyCost((PartSlot)slot, TierRank.B);

                Assert.Greater(minC, maxD, "Slot " + (PartSlot)slot + ": tier C tem de custar mais que todo D.");
                Assert.Greater(minB, maxC, "Slot " + (PartSlot)slot + ": tier B tem de custar mais que todo C.");
            }
        }

        [Test]
        public void NenhumaPecaAcimaDeDPertenceASet()
        {
            // Os dois sets tem 4 pecas exatas, todas tier D. Uma peca de C ou B com set
            // mudaria bonus ja calibrados sem que ninguem percebesse.
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef def = _content.PartList[i];
                if (def.Tier == TierRank.D) continue;

                Assert.IsTrue(string.IsNullOrEmpty(def.SetId),
                    def.Id + " e tier " + def.Tier + " e pertence ao set " + def.SetId + ".");
            }
        }

        [Test]
        public void TodaPecaNovaEVendidaNaLoja()
        {
            // Peca de tier alto que nao esta na vitrine so viria de drop - e o drop dela
            // depende de pista daquele tier, que o MVP ainda nao tem.
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef def = _content.PartList[i];
                if (def.Tier == TierRank.D) continue;

                Assert.Greater(def.BuyCost, 0L, def.Id + " nao tem preco e ficaria inalcancavel.");
            }
        }

        [Test]
        public void APromocaoAbreAVitrineDoTierNovo()
        {
            var economy = new EconomyLedger(_save, _content.Balance);
            var inventory = new InventoryManager(_save, TestWorld.Shared.Resolver.Loot, economy);
            var shop = new Shop(_save, _content, inventory, economy, TestWorld.Shared.Resolver.Loot);

            int beforeCount = shop.Catalogue(PartSlot.Engine).Count;

            _save.Reputation = _content.Tier(TierRank.C).ReputationRequired;
            _reputation.Evaluate();

            List<PartDef> after = shop.Catalogue(PartSlot.Engine);
            Assert.Greater(after.Count, beforeCount,
                "Promover a C tem de colocar peca nova na vitrine - senao a promocao nao paga nada.");
        }

        private long MinBuyCost(PartSlot slot, TierRank tier)
        {
            long best = long.MaxValue;
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef d = _content.PartList[i];
                if (d.Slot == slot && d.Tier == tier && d.BuyCost > 0L && d.BuyCost < best) best = d.BuyCost;
            }
            return best;
        }

        private long MaxBuyCost(PartSlot slot, TierRank tier)
        {
            long best = 0L;
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef d = _content.PartList[i];
                if (d.Slot == slot && d.Tier == tier && d.BuyCost > best) best = d.BuyCost;
            }
            return best;
        }
    }
}
