// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 5.2 - 5.5
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Resolve UM segmento: grip efetivo, velocidade de curva, decisao de drift,
    /// qualidade de execucao e estado continuo resultante.
    ///
    /// E aqui que vivem as formulas de 5.2 a 5.5. Nenhum numero literal neste arquivo -
    /// tudo vem de BalanceSettings (GDD 20.2).
    /// </summary>
    public sealed class SegmentSolver
    {
        private readonly BalanceSettings _balance;

        public SegmentSolver(BalanceSettings balance)
        {
            _balance = balance;
        }

        // TODO(GDD 5.2 - 5.5): implementar.
    }
}
