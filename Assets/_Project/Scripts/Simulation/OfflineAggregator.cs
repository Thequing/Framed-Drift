// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 17
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Agrega N corridas de uma ausencia num unico resultado.
    ///
    /// Roda o MESMO simulador (D-01) - offline rende 100% do que renderia online,
    /// limitado pelo teto de horas (D-02). Coletaveis da secao 3.6.2 NAO sao gerados
    /// aqui: eles nao sao "perdidos" na ausencia, simplesmente nao existem offline.
    /// </summary>
    public sealed class OfflineAggregator
    {
        private readonly BalanceSettings _balance;

        public OfflineAggregator(BalanceSettings balance)
        {
            _balance = balance;
        }

        // TODO(GDD 17): implementar.
    }
}
