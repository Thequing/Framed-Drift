// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 9.4, 10.6, 15.5, 18.5
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// A vitrine e o primeiro dreno de cash de verdade (GDD 15.5). Estes testes fixam as
    /// regras que a impedem de virar um botao de "upgrade +1", que a GDD 9.4 proibe.
    /// </summary>
    public sealed class ShopTests
    {
        private ContentDatabase _content;
        private SaveData _save;
        private EconomyLedger _economy;
        private InventoryManager _inventory;
        private Shop _shop;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;

            _save = new SaveData();
            _economy = new EconomyLedger(_save, _content.Balance);
            _inventory = new InventoryManager(_save, TestWorld.Shared.Resolver.Loot, _economy);
            _shop = new Shop(_save, _content, _inventory, _economy, TestWorld.Shared.Resolver.Loot);
        }

        // --- catalogo --------------------------------------------------------------

        [Test]
        public void CatalogoSoMostraOSlotPedido()
        {
            List<PartDef> engines = _shop.Catalogue(PartSlot.Engine);

            Assert.Greater(engines.Count, 0, "O tier inicial precisa ter algo a venda no motor.");
            for (int i = 0; i < engines.Count; i++)
                Assert.AreEqual(PartSlot.Engine, engines[i].Slot);
        }

        [Test]
        public void CatalogoNaoVendePecaDeSerie()
        {
            // buyCost 0 = peca que vem montada no carro. Vende-la seria cobrar por algo
            // que o jogador ja tem.
            for (int slot = 0; slot < 8; slot++)
            {
                List<PartDef> page = _shop.Catalogue((PartSlot)slot);
                for (int i = 0; i < page.Count; i++)
                    Assert.Greater(page[i].BuyCost, 0L,
                        page[i].Id + " tem buyCost 0 e nao devia estar na vitrine.");
            }
        }

        [Test]
        public void CatalogoVemDoMaisBaratoAoMaisCaro()
        {
            List<PartDef> engines = _shop.Catalogue(PartSlot.Engine);

            for (int i = 1; i < engines.Count; i++)
                Assert.LessOrEqual(engines[i - 1].BuyCost, engines[i].BuyCost,
                    "A vitrine e uma escada: o preco tem de subir junto com a peca.");
        }

        [Test]
        public void CatalogoNuncaMostraTierAcimaDoJogador()
        {
            _save.Progress.TierIndex = 0;

            for (int slot = 0; slot < 8; slot++)
            {
                List<PartDef> page = _shop.Catalogue((PartSlot)slot);
                for (int i = 0; i < page.Count; i++)
                    Assert.AreEqual(TierRank.D, page[i].Tier,
                        "No tier D a vitrine so pode ofertar tier D.");
            }
        }

        [Test]
        public void CatalogoCresceComOTier()
        {
            _save.Progress.TierIndex = 0;
            int atD = TotalOnSale();

            _save.Progress.TierIndex = (int)TierRank.S;
            int atS = TotalOnSale();

            Assert.GreaterOrEqual(atS, atD, "Subir de tier nunca pode ENCOLHER a vitrine.");
        }

        // --- compra ----------------------------------------------------------------

        [Test]
        public void ComprarCobraOPrecoEEntregaAPeca()
        {
            PartDef def = FirstOnSale();
            _economy.AddCash(def.BuyCost);

            PartInstance bought = _shop.Buy(def.Id);

            Assert.IsNotNull(bought, "A compra tinha cash e espaco; devia entregar.");
            Assert.AreEqual(def.Id, bought.Rolled.BaseId);
            Assert.AreEqual(0L, _economy.Cash, "Tem de cobrar o preco exato.");
            Assert.AreEqual(1, _inventory.Count);
        }

        [Test]
        public void ComprarSemCashNaoCobraNemEntrega()
        {
            PartDef def = FirstOnSale();
            _economy.AddCash(def.BuyCost - 1L);

            Assert.IsNull(_shop.Buy(def.Id));
            Assert.AreEqual(def.BuyCost - 1L, _economy.Cash, "Compra recusada nao pode cobrar.");
            Assert.AreEqual(0, _inventory.Count);
        }

        [Test]
        public void ComprarPecaDeTierBloqueadoERecusado()
        {
            PartDef locked = null;
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef def = _content.PartList[i];
                if (def.BuyCost > 0L && def.Tier > TierRank.D) { locked = def; break; }
            }

            if (locked == null)
                Assert.Ignore("O conteudo do MVP so tem pecas tier D a venda.");

            _save.Progress.TierIndex = 0;
            _economy.AddCash(999999L);

            Assert.AreEqual(PurchaseBlock.TierLocked, _shop.Evaluate(locked));
            Assert.IsNull(_shop.Buy(locked.Id));
            Assert.AreEqual(999999L, _economy.Cash);
        }

        [Test]
        public void ComprarComInventarioCheioNaoCobra()
        {
            PartDef def = FirstOnSale();
            _save.InventoryCap = 0;
            _economy.AddCash(def.BuyCost);

            Assert.AreEqual(PurchaseBlock.InventoryFull, _shop.Evaluate(def));
            Assert.IsNull(_shop.Buy(def.Id));
            Assert.AreEqual(def.BuyCost, _economy.Cash,
                "Cobrar e so entao descobrir que a peca nao cabe foi o bug do craft.");
        }

        [Test]
        public void PecaDesconhecidaNaoQuebraNemCobra()
        {
            _economy.AddCash(50000L);

            Assert.IsNull(_shop.Buy("nao_existe"));
            Assert.IsNull(_shop.Buy(null));
            Assert.AreEqual(50000L, _economy.Cash);
        }

        // --- as duas regras de design ----------------------------------------------

        [Test]
        public void AVitrineVendePecaDeFabricaSemAfixos()
        {
            // A loja e o PISO deterministico; o drop e a versao com afixos (GDD 10.6). Se
            // ela vendesse pecas roladas, competiria com o loot em vez de cobrir sua cauda.
            PartDef def = FirstOnSale();
            _economy.AddCash(def.BuyCost);

            PartInstance bought = _shop.Buy(def.Id);

            Assert.AreEqual(0, bought.Rolled.Affixes.Length, "Peca de loja nao tem afixo.");
            Assert.IsFalse(bought.Rolled.HasPassive, "Peca de loja nao tem passiva.");
            Assert.AreEqual(Rarity.Common, bought.Rarity);
        }

        [Test]
        public void CompraPublicaOExtrato()
        {
            PartDef def = FirstOnSale();
            _economy.AddCash(def.BuyCost);

            int currencyEvents = 0;
            System.Action<CurrencyChanged> handler = _ => currencyEvents++;
            EventBus.Subscribe(handler);

            try
            {
                _shop.Buy(def.Id);
                Assert.AreEqual(1, currencyEvents, "Comprar tem de publicar CurrencyChanged.");
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        [Test]
        public void PreviewDescreveAPecaSemColocaNoInventario()
        {
            // O delta da 18.5 tem de existir ANTES da compra - e sem efeito colateral.
            PartDef def = FirstOnSale();

            RolledPart preview = _shop.Preview(def);

            Assert.IsNotNull(preview);
            Assert.AreEqual(def.Id, preview.BaseId);
            Assert.AreEqual(0, _inventory.Count, "Espiar nao pode dar a peca de graca.");
            Assert.AreEqual(0L, _economy.Cash);
        }

        private int TotalOnSale()
        {
            int n = 0;
            for (int slot = 0; slot < 8; slot++) n += _shop.Catalogue((PartSlot)slot).Count;
            return n;
        }

        private PartDef FirstOnSale()
        {
            List<PartDef> page = _shop.Catalogue(PartSlot.Engine);
            Assert.Greater(page.Count, 0, "Precisa de ao menos uma peca a venda para testar.");
            return page[0];
        }
    }
}
