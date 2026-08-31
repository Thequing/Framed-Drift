// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secoes 15.1, 15.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using UnityEngine;

namespace FramedDrift.Core
{
    /// <summary>Publicado quando qualquer moeda muda. A UI so redesenha nisto.</summary>
    public struct CurrencyChanged
    {
        public long Cash;
        public long Scrap;
        public int Reputation;
    }

    /// <summary>
    /// As cinco moedas do jogo inteiro (GDD 15.1) e os custos da 15.3.
    ///
    /// Um unico ponto de entrada para creditar e debitar. Nao e cerimonia: e o que
    /// permite que a tela de retorno da ausencia (GDD 17.4) mostre um extrato coerente,
    /// e que "cash negativo" seja impossivel por construcao e nao por disciplina.
    ///
    /// MORA EM `Core`, e nao em `Progression` como a GDD 20.1 sugere. Garage e
    /// Progression sao assemblies IRMAOS - nenhum ve o outro - entao um ledger em
    /// Progression seria invisivel para Crafting e InventoryManager, que sao justamente
    /// quem gasta. Enquanto ele morou la, os dois debitavam `SaveData` direto e o
    /// `CurrencyChanged` nunca era publicado: a regra do "unico ponto de entrada" acima
    /// era so um comentario. O ledger nao depende de Garage nem de Progression - so de
    /// SaveData, BalanceSettings e EventBus - entao Core e onde ele sempre coube.
    /// </summary>
    public sealed class EconomyLedger
    {
        private readonly SaveData _save;
        private readonly BalanceSettings _balance;

        public EconomyLedger(SaveData save, BalanceSettings balance)
        {
            _save = save;
            _balance = balance;
        }

        public long Cash { get { return _save.Cash; } }
        public long Scrap { get { return _save.Scrap; } }
        public int Reputation { get { return _save.Reputation; } }
        public long Fame { get { return _save.Prestige.Fame; } }

        // --- credito ------------------------------------------------------------

        public void AddCash(long amount)
        {
            if (amount == 0L) return;
            _save.Cash += amount;
            if (_save.Cash < 0L) _save.Cash = 0L;
            Publish();
        }

        public void AddScrap(long amount)
        {
            if (amount == 0L) return;
            _save.Scrap += amount;
            if (_save.Scrap < 0L) _save.Scrap = 0L;
            Publish();
        }

        public void AddReputation(int amount)
        {
            if (amount == 0) return;

            // Reputacao nao gasta: e um LIMIAR (GDD 15.1). Por isso nunca decresce por
            // compra - so por prestigio, que reinicia a temporada inteira.
            _save.Reputation += amount;
            if (_save.Reputation < 0) _save.Reputation = 0;
            Publish();
        }

        // --- debito ---------------------------------------------------------------

        public bool CanAfford(long cash)
        {
            return _save.Cash >= cash;
        }

        public bool SpendCash(long amount)
        {
            if (amount <= 0L) return true;
            if (_save.Cash < amount) return false;

            _save.Cash -= amount;
            Publish();
            return true;
        }

        public bool SpendScrap(long amount)
        {
            if (amount <= 0L) return true;
            if (_save.Scrap < amount) return false;

            _save.Scrap -= amount;
            Publish();
            return true;
        }

        // --- custos (15.3) -----------------------------------------------------------

        /// <summary>custoUpgrade(slot, tier, nivel) = 120 * tier^1.6 * 1.16^nivel.</summary>
        public long UpgradeCost(int tierIndex, int level)
        {
            var b = _balance;
            int tier = tierIndex < 1 ? 1 : tierIndex + 1;
            return (long)(b.UpgradeCostBase
                          * Mathf.Pow(tier, b.UpgradeCostTierExponent)
                          * Mathf.Pow(b.UpgradeCostLevelBase, level));
        }

        private void Publish()
        {
            EventBus.Publish(new CurrencyChanged
            {
                Cash = _save.Cash,
                Scrap = _save.Scrap,
                Reputation = _save.Reputation,
            });
        }
    }
}
