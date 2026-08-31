// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 9.4, 15.1, 15.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>Publicado a cada compra. A UI e o extrato leem daqui.</summary>
    public struct PartPurchased
    {
        public PartInstance Part;
        public long Price;
    }

    /// <summary>Por que uma peca da vitrine nao pode ser comprada agora.</summary>
    public enum PurchaseBlock
    {
        None,
        TierLocked,
        NotForSale,
        CantAfford,
        InventoryFull,
    }

    /// <summary>
    /// A vitrine: catalogo FIXO por tier, precos autorais em `parts.json`.
    ///
    /// Existe porque cash era uma moeda sem dreno. Ate aqui a unica saida real era o
    /// reparo - alguns milhares por hora contra dezenas de milhares entrando - e um
    /// incremental cujo recurso principal so acumula perde a decisao que o faz andar
    /// (GDD 15.5).
    ///
    /// DUAS REGRAS a mantem honesta:
    ///
    /// 1. **A loja vende peca de FABRICA - sem afixos, sem passiva** (o que
    ///    <see cref="LootRoller.Factory"/> ja produzia para o carro inicial). A loja e o
    ///    PISO deterministico; o drop e a versao com afixos. Se a vitrine vendesse pecas
    ///    roladas, ela competiria com o loot, e a GDD 10.6 e explicita: o caminho
    ///    deterministico existe para cortar a cauda do RNG, nao para substitui-lo.
    ///
    /// 2. **Nada de "upgrade +1".** A GDD 9.4 proibe o botao que so aumenta numeros:
    ///    melhorar e TROCAR a peca. Por isso a vitrine vende bases, e a progressao dela e
    ///    o tier - nao um nivel por slot.
    ///
    /// Sobre a 15.3: `custoUpgrade(slot, tier, nivel)` NAO precifica esta vitrine. Ver o
    /// comentario em <see cref="Price"/>.
    /// </summary>
    public sealed class Shop
    {
        private readonly SaveData _save;
        private readonly ContentDatabase _content;
        private readonly InventoryManager _inventory;
        private readonly EconomyLedger _economy;
        private readonly LootRoller _loot;

        public Shop(SaveData save, ContentDatabase content, InventoryManager inventory,
                    EconomyLedger economy, LootRoller loot)
        {
            _save = save;
            _content = content;
            _inventory = inventory;
            _economy = economy;
            _loot = loot;
        }

        /// <summary>O tier que o jogador ja alcancou. O catalogo cresce com ele.</summary>
        public TierRank PlayerTier
        {
            get
            {
                int i = _save.Progress.TierIndex;
                if (i < 0) i = 0;
                if (i > (int)TierRank.S) i = (int)TierRank.S;
                return (TierRank)i;
            }
        }

        /// <summary>
        /// O catalogo de um slot: fixo, ordenado do mais barato ao mais caro.
        ///
        /// Fixo e a escolha de design. Um estoque rotativo transformaria a decisao "vale
        /// a pena?" em "esta disponivel?", e a GDD 2 vende uma sessao de relance - quem
        /// abre o jogo por 3 minutos nao pode descobrir que a peca que ele juntou cash
        /// para comprar sumiu da vitrine.
        /// </summary>
        public List<PartDef> Catalogue(PartSlot slot)
        {
            var page = new List<PartDef>();

            int maxTier = (int)PlayerTier;
            for (int i = 0; i < _content.PartList.Count; i++)
            {
                PartDef def = _content.PartList[i];
                if (def.Slot != slot) continue;
                if (def.BuyCost <= 0L) continue;          // peca de serie: vem com o carro
                if ((int)def.Tier > maxTier) continue;

                page.Add(def);
            }

            page.Sort(CheapestFirst);
            return page;
        }

        private static int CheapestFirst(PartDef a, PartDef b)
        {
            int byCost = a.BuyCost.CompareTo(b.BuyCost);
            return byCost != 0 ? byCost : string.CompareOrdinal(a.Id, b.Id);
        }

        /// <summary>
        /// O preco e o `buyCost` autoral de `parts.json`, nao a formula da GDD 15.3.
        ///
        /// A 15.3 define `custoUpgrade(slot, tier, nivel) = 120 * tier^1.6 * 1.16^nivel`,
        /// que precifica um NIVEL por slot - o mesmo botao "+1" que a 9.4 proibe. As duas
        /// secoes se contradizem, e o conteudo ja escolheu um lado: os buyCost autorais
        /// (900, 1.400, 2.400, 2.600...) vivem numa escala inteiramente diferente da
        /// formula (que daria 139 ou 161 no tier D). Precificar pela formula jogaria fora
        /// precos calibrados a mao para servir a uma secao que a 9.4 ja revogou.
        /// </summary>
        public long Price(PartDef def)
        {
            return def == null ? 0L : def.BuyCost;
        }

        /// <summary>Por que o botao esta cinza. A UI mostra o motivo, nunca so o cinza.</summary>
        public PurchaseBlock Evaluate(PartDef def)
        {
            if (def == null || def.BuyCost <= 0L) return PurchaseBlock.NotForSale;
            if ((int)def.Tier > (int)PlayerTier) return PurchaseBlock.TierLocked;
            if (_inventory.IsFull) return PurchaseBlock.InventoryFull;
            if (!_economy.CanAfford(def.BuyCost)) return PurchaseBlock.CantAfford;
            return PurchaseBlock.None;
        }

        public bool CanBuy(PartDef def)
        {
            return Evaluate(def) == PurchaseBlock.None;
        }

        /// <summary>
        /// Compra. Devolve a peca entregue, ou null quando nada foi cobrado.
        ///
        /// A ORDEM importa: o inventario e checado ANTES do debito. Cobrar e so entao
        /// descobrir que a peca nao cabe e o bug que o craft tinha.
        /// </summary>
        public PartInstance Buy(string partId)
        {
            PartDef def;
            if (partId == null || !_content.Parts.TryGetValue(partId, out def)) return null;
            if (Evaluate(def) != PurchaseBlock.None) return null;

            if (!_economy.SpendCash(def.BuyCost)) return null;

            PartInstance part = _inventory.AddFactory(def.Id);
            if (part == null)
            {
                // Inalcancavel pelo Evaluate acima, mas o estorno fica: um debito sem
                // entrega e a unica falha desta classe que o jogador nao consegue desfazer.
                _economy.AddCash(def.BuyCost);
                return null;
            }

            EventBus.Publish(new PartPurchased { Part = part, Price = def.BuyCost });
            return part;
        }

        /// <summary>A peca de fabrica que a vitrine entrega, para o delta antes da compra.</summary>
        public RolledPart Preview(PartDef def)
        {
            return def == null ? null : _loot.Factory(def.Id);
        }
    }
}
