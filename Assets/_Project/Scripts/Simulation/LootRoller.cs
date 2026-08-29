// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 10
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Rola raridade, afixos e passivas usando o fluxo de RNG de loot - separado do
    /// fluxo de execucao para que rebalancear drops nao altere corridas (GDD 20.3).
    /// </summary>
    public sealed class LootRoller
    {
        private readonly BalanceSettings _balance;

        public LootRoller(BalanceSettings balance)
        {
            _balance = balance;
        }

        // TODO(GDD 10): implementar.
    }
}
