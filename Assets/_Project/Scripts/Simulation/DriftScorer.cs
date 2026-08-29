// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 6, 3.6, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Reduz uma Timeline a um Drift Score.
    ///
    /// Deliberadamente separado do simulador (GDD 20.6): a mesma funcao, chamada com a
    /// timeline intocada, produz o resultado offline. E o que torna a paridade de D-02
    /// estrutural em vez de algo a testar.
    /// </summary>
    public sealed class DriftScorer
    {
        private readonly BalanceSettings _balance;

        public DriftScorer(BalanceSettings balance)
        {
            _balance = balance;
        }

        // TODO(GDD 6, 3.6, 20.6): implementar.
    }
}
