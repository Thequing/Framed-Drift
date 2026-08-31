// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 15.1, 15.3
// -----------------------------------------------------------------------------

using FramedDrift.Automation;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using NUnit.Framework;
using UnityEngine;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O ledger e o UNICO ponto de entrada das moedas (GDD 15.1).
    ///
    /// Nao e um teste de aritmetica: e o teste de que nenhum sistema volta a debitar
    /// `SaveData` por fora. Enquanto Crafting e InventoryManager mexiam no save direto, o
    /// `CurrencyChanged` nunca era publicado e qualquer UI orientada a evento mostraria
    /// saldo velho. Estes asserts sao o que impede a regressao.
    /// </summary>
    public sealed class EconomyTests
    {
        private ContentDatabase _content;
        private SaveData _save;
        private EconomyLedger _economy;
        private InventoryManager _inventory;
        private Crafting _crafting;

        private int _publishCount;
        private CurrencyChanged _last;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;

            _save = new SaveData();
            _economy = new EconomyLedger(_save, _content.Balance);
            _inventory = new InventoryManager(_save, TestWorld.Shared.Resolver.Loot, _economy);
            _crafting = new Crafting(_save, _content, _inventory,
                                     TestWorld.Shared.Resolver.Loot, _economy);

            _publishCount = 0;
            EventBus.Subscribe<CurrencyChanged>(OnCurrency);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<CurrencyChanged>(OnCurrency);
        }

        private void OnCurrency(CurrencyChanged evt)
        {
            _publishCount++;
            _last = evt;
        }

        // --- o ledger em si -------------------------------------------------------

        [Test]
        public void CashNuncaFicaNegativo()
        {
            _economy.AddCash(100L);

            Assert.IsFalse(_economy.SpendCash(101L), "Gastar mais do que tem deve falhar.");
            Assert.AreEqual(100L, _economy.Cash, "Uma compra recusada nao pode cobrar nada.");

            Assert.IsTrue(_economy.SpendCash(100L));
            Assert.AreEqual(0L, _economy.Cash);
        }

        [Test]
        public void TodaMudancaDeMoedaPublicaOExtrato()
        {
            _economy.AddCash(500L);
            Assert.AreEqual(1, _publishCount);
            Assert.AreEqual(500L, _last.Cash);

            _economy.SpendCash(200L);
            Assert.AreEqual(2, _publishCount);
            Assert.AreEqual(300L, _last.Cash);
        }

        [Test]
        public void CompraRecusadaNaoPublicaNada()
        {
            _economy.AddCash(10L);
            _publishCount = 0;

            _economy.SpendCash(999L);
            Assert.AreEqual(0, _publishCount, "Nada mudou; nada a publicar.");
        }

        // --- os sistemas que gastam ------------------------------------------------

        [Test]
        public void ReparoCobraPeloLedgerEPublica()
        {
            CarInstance car = NewCar();
            car.ApplyRaceDamage(40f);

            long cost = _crafting.RepairCost(car, 0);
            Assert.Greater(cost, 0L, "Um carro danificado precisa custar algo para reparar.");

            _economy.AddCash(cost);
            _publishCount = 0;

            Assert.IsTrue(_crafting.Repair(car, 0));
            Assert.AreEqual(0L, _economy.Cash, "O reparo tem de debitar o custo exato.");
            Assert.AreEqual(1, _publishCount, "O reparo tem de publicar CurrencyChanged.");
        }

        [Test]
        public void ReparoSemCashNaoRepara()
        {
            CarInstance car = NewCar();
            car.ApplyRaceDamage(40f);
            float damaged = car.Damage;

            Assert.IsFalse(_crafting.Repair(car, 0), "Sem cash, o reparo tem de recusar.");
            Assert.AreEqual(damaged, car.Damage, 0.01f, "Reparo recusado nao pode consertar de graca.");
            Assert.AreEqual(0L, _economy.Cash);
        }

        [Test]
        public void DesmontarCreditaPeloLedgerEPublica()
        {
            PartInstance part = _inventory.Add(new Simulation.Outcomes.PartDrop
            {
                BaseId = "eng_stock",
                Rarity = Rarity.Common,
                ItemLevel = 1,
                Seed = 12345UL,
            });
            Assert.IsNotNull(part);

            _publishCount = 0;
            long scrap = _inventory.Salvage(part.Uid);

            Assert.Greater(scrap, 0L);
            Assert.AreEqual(scrap, _economy.Scrap, "O scrap tem de entrar pelo ledger.");
            Assert.AreEqual(1, _publishCount, "Desmontar tem de publicar CurrencyChanged.");
        }

        // --- custos da 15.3 --------------------------------------------------------

        [Test]
        public void PrimeiraVagaDeGaragemCustaABaseCheia()
        {
            // custoSlotGaragem(n) = base * growth^(n-2), n = vagas atuais. Saindo das 2
            // iniciais, o expoente e zero - a primeira compra custa exatamente a base.
            var fleet = new FleetManager(_save, _content.Balance);

            Assert.AreEqual(FleetManager.StartingSlots, fleet.SlotCount);
            Assert.AreEqual((long)_content.Balance.GarageSlotCostBase, fleet.NextSlotCost());
        }

        [Test]
        public void CadaVagaSeguinteMultiplicaPeloCrescimento()
        {
            var fleet = new FleetManager(_save, _content.Balance);
            var b = _content.Balance;

            long first = fleet.NextSlotCost();

            _economy.AddCash(first);
            Assert.IsTrue(fleet.BuySlot(_economy));
            Assert.AreEqual(0L, _economy.Cash, "Comprar vaga tem de debitar pelo ledger.");

            Assert.AreEqual((long)(b.GarageSlotCostBase * Mathf.Pow(b.GarageSlotCostGrowth, 1)),
                            fleet.NextSlotCost());
        }

        [Test]
        public void VagaSemCashNaoEComprada()
        {
            var fleet = new FleetManager(_save, _content.Balance);
            int before = fleet.SlotCount;

            Assert.IsFalse(fleet.BuySlot(_economy));
            Assert.AreEqual(before, fleet.SlotCount, "Compra recusada nao pode entregar a vaga.");
        }

        private CarInstance NewCar()
        {
            var saved = new SavedCar { CarId = "kite_130" };
            return new CarInstance(saved, _content.Car(saved.CarId),
                                   TestWorld.Shared.Loadouts, _inventory);
        }
    }
}
