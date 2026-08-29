// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 5
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Resolve uma corrida inteira, segmento a segmento, carregando estado continuo
    /// (velocidade, temperatura, desgaste, combo, dano).
    ///
    /// Alvo de desempenho: uma corrida de 25 segmentos em menos de 0,3 ms (GDD 20.4),
    /// porque o auto-equipar roda 200 corridas por avaliacao (GDD 16.3).
    /// </summary>
    public sealed class RaceSimulator
    {
        private readonly BalanceSettings _balance;

        public RaceSimulator(BalanceSettings balance)
        {
            _balance = balance;
        }

        // TODO(GDD 5): implementar.
    }
}
