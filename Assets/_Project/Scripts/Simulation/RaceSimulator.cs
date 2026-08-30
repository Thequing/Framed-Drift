// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 5, 20.6
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Resolve uma corrida inteira, segmento a segmento, carregando estado continuo
    /// (velocidade, temperatura, desgaste, combo, dano).
    ///
    /// Devolve um <see cref="RaceResult"/> SEM Drift Score. Isso e proposital (GDD 20.6):
    /// o score sai de <c>DriftScorer.Score(timeline)</c> depois que a corrida termina de
    /// tocar, porque a timeline ainda pode ser editada pela Entrada Perfeita. Chamar o
    /// mesmo scorer com a timeline intocada devolve exatamente o resultado offline - e
    /// por isso a paridade de D-02 e estrutural, e nao algo a testar.
    ///
    /// Alvo de desempenho: uma corrida de 25 segmentos em menos de 0,3 ms (GDD 20.4),
    /// porque o auto-equipar roda 200 corridas por avaliacao (GDD 16.3).
    /// </summary>
    public sealed class RaceSimulator
    {
        private readonly BalanceSettings _balance;
        private readonly SegmentSolver _solver;

        public RaceSimulator(BalanceSettings balance)
        {
            _balance = balance;
            _solver = new SegmentSolver(balance);
        }

        public RaceResult Simulate(RaceInstance race)
        {
            var streams = new RngStreams(race.Seed);
            return Simulate(race, streams);
        }

        public RaceResult Simulate(RaceInstance race, RngStreams streams)
        {
            var b = _balance;
            SegmentSolver.Context ctx = _solver.BuildContext(race);

            var timeline = new SegmentOutcome[race.Track.Length];
            RaceState state = RaceState.Start(b.StartSpeedKmh);

            var windows = new List<InputWindow>();

            for (int i = 0; i < race.Track.Length; i++)
            {
                SegmentOutcome outcome = _solver.Solve(i, in race.Track[i], ctx, ref state, streams.Execution);
                timeline[i] = outcome;

                if (outcome.Promotable)
                {
                    // A timeline inteira e conhecida antes de a reproducao comecar, entao
                    // a UI pode telegrafar a curva com antecedencia (GDD 20.6).
                    windows.Add(new InputWindow
                    {
                        SegmentIndex = i,
                        OpensAt = outcome.TimeOffset,
                        ClosesAt = outcome.TimeOffset + b.PerfectEntryWindowSeconds,
                    });
                }
            }

            var result = new RaceResult
            {
                TotalTime = state.TimeSeconds,
                MaxCombo = state.MaxCombo,
                DistanceM = state.DistanceM,
                Damage = state.Damage,
                Timeline = timeline,
                InputWindows = windows.ToArray(),
                RiskIndex = ctx.RiskIndex,
                Failures = state.Failures,
                Collisions = state.Collisions,
                DriftSegments = state.DriftSegments,
                PerfectSegments = state.PerfectSegments,
                BadSegments = state.BadSegments,
                FinalTireWear = state.TireWear,
                FinalTemperature = state.Temperature,
                TrackDisplayName = race.TrackDisplayName,
                CarId = race.Car.CarId,
                Seed = race.Seed,
            };

            result.Position = ResolvePosition(race, state.TimeSeconds, streams.Opponents, result);
            return result;
        }

        /// <summary>
        /// Grid de N carros por ~zero custo de CPU (GDD 5.6).
        ///
        /// Os adversarios NAO sao simulados individualmente no MVP: a pista define uma
        /// distribuicao de tempos por tier e a posicao sai de quantos ficaram abaixo do
        /// tempo do jogador. Rivais nomeados (GDD 13) sao a excecao - esses sao resolvidos
        /// pelo mesmo simulador, com carro e build proprios.
        /// </summary>
        private int ResolvePosition(RaceInstance race, float totalTime, DeterministicRng rng, RaceResult result)
        {
            var b = _balance;

            float tRef = race.TrackBaseTimeSeconds * (1f - race.TierIndex * b.TierTimeScale);
            float sigma = tRef * b.OpponentSigmaFraction;

            int opponents = MathUtil.Clamp(b.GridSize - 1, 0, 32);
            var times = new float[opponents];
            int ahead = 0;

            for (int i = 0; i < opponents; i++)
            {
                times[i] = (float)rng.NextGaussian(tRef, sigma);
                if (times[i] < totalTime) ahead++;
            }

            result.OpponentTimes = times;
            result.ReferenceTime = tRef;
            return 1 + ahead;
        }
    }
}
