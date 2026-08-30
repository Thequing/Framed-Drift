// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 9.5, 16.3, 18.5
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>A funcao objetivo do auto-equipar, escolhida pelo jogador. GDD 16.3.</summary>
    public enum EquipObjective
    {
        DriftScore,
        CashPerHour,
        LapTime,
        Reliability,
        ActiveBuild,
    }

    /// <summary>O que muda ao trocar uma peca. Alimenta o tooltip da GDD 18.5.</summary>
    public struct BuildDelta
    {
        public ResolvedStats Before;
        public ResolvedStats After;
        public double EstimatedScoreBefore;
        public double EstimatedScoreAfter;

        public double ScoreDelta { get { return EstimatedScoreAfter - EstimatedScoreBefore; } }

        public float Stat(StatId id)
        {
            return StatOps.Get(in After, id) - StatOps.Get(in Before, id);
        }
    }

    /// <summary>
    /// Presets nomeados e a avaliacao de "peca melhor".
    ///
    /// A avaliacao roda o SIMULADOR em modo rapido com a peca candidata, N corridas, e
    /// compara a media (GDD 16.3). Isso e barato - 200 corridas custam ~1 ms (GDD 20.4) -
    /// e sobretudo e HONESTO: usa o mesmo solucionador do jogo, entao nunca sugere algo
    /// que na pratica piora.
    /// </summary>
    public sealed class BuildManager
    {
        private readonly RaceResolver _resolver;
        private readonly RaceFactory _factory;
        private readonly ContentDatabase _content;

        public BuildManager(ContentDatabase content, RaceResolver resolver, RaceFactory factory)
        {
            _content = content;
            _resolver = resolver;
            _factory = factory;
        }

        // --- presets (9.5) ---------------------------------------------------------

        public SavedBuild SaveAs(CarInstance car, string name)
        {
            SavedBuild current = car.ActiveBuild;
            var copy = new SavedBuild
            {
                Name = name,
                SlotUids = (int[])current.SlotUids.Clone(),
                TuneLock = current.TuneLock,
                TuneAccel = current.TuneAccel,
                TuneDecel = current.TuneDecel,
                Style = current.Style,
            };
            car.Saved.Builds.Add(copy);
            return copy;
        }

        public void Activate(CarInstance car, int index)
        {
            if (index < 0 || index >= car.Saved.Builds.Count) return;
            car.Saved.ActiveBuild = index;
            car.MarkDirty();
        }

        // --- avaliacao (16.3) --------------------------------------------------------

        /// <summary>
        /// Media de uma metrica sobre N corridas com a build dada.
        ///
        /// N vem do BalanceConfig (autoEquipSampleRaces). Menos amostras deixariam o
        /// ruido do RNG decidir qual peca "e melhor", e o jogador veria a recomendacao
        /// mudar sozinha entre duas aberturas da garagem.
        /// </summary>
        public double Evaluate(CarInstance car, SavedBuild build, TrackDef track,
                               RaceConditions conditions, EquipObjective objective, int stage)
        {
            CarLoadout loadout = car.Resolve(build);
            RaceInstance race = _factory.Build(track, loadout, conditions, build.Style, stage, 1f, 1UL);

            int samples = _content.Balance.AutoEquipSampleRaces;
            if (samples < 1) samples = 1;

            double total = 0.0;
            double totalTime = 0.0;

            for (int i = 0; i < samples; i++)
            {
                race.Seed = 0x51ED0000UL + (ulong)i * 0x9E3779B97F4A7C15UL;
                var result = _resolver.ResolveComplete(race);
                totalTime += result.TotalTime;

                switch (objective)
                {
                    case EquipObjective.DriftScore: total += result.Rewards.DriftScore; break;
                    case EquipObjective.CashPerHour: total += result.Rewards.Cash; break;
                    case EquipObjective.LapTime: total -= result.TotalTime; break;
                    case EquipObjective.Reliability: total -= result.Damage; break;
                    default: total += result.Rewards.DriftScore; break;
                }
            }

            if (objective == EquipObjective.CashPerHour && totalTime > 0.0)
                return total / (totalTime / 3600.0);

            return total / samples;
        }

        /// <summary>
        /// A comparacao que a GDD 18.5 exige estar SEMPRE visivel: delta de stats e de
        /// Drift Score estimado, contra a peca atualmente equipada.
        /// </summary>
        public BuildDelta Compare(CarInstance car, PartInstance candidate, TrackDef track,
                                  RaceConditions conditions, int stage)
        {
            CarLoadout before = car.Loadout;
            CarLoadout after = car.ResolveWith(candidate);

            return new BuildDelta
            {
                Before = before.Stats,
                After = after.Stats,
                EstimatedScoreBefore = EstimateScore(before, car.Style, track, conditions, stage),
                EstimatedScoreAfter = EstimateScore(after, car.Style, track, conditions, stage),
            };
        }

        /// <summary>
        /// Estimativa rapida para tooltip: poucas corridas, o suficiente para o SINAL do
        /// delta ser confiavel. A avaliacao completa do auto-equipar usa N do config.
        /// </summary>
        public double EstimateScore(CarLoadout loadout, DriftStyle style, TrackDef track,
                                    RaceConditions conditions, int stage)
        {
            RaceInstance race = _factory.Build(track, loadout, conditions, style, stage, 1f, 1UL);

            const int quickSamples = 24;
            double total = 0.0;
            for (int i = 0; i < quickSamples; i++)
            {
                race.Seed = 0x700171F0UL + (ulong)i;
                total += _resolver.ResolveComplete(race).Rewards.DriftScore;
            }
            return total / quickSamples;
        }

        /// <summary>
        /// A melhor peca do inventario para um slot, segundo o objetivo escolhido.
        /// Devolve null quando nenhuma bate a equipada - "nao mexer" e uma resposta valida.
        /// </summary>
        public PartInstance BestFor(CarInstance car, InventoryManager inventory, PartSlot slot,
                                    TrackDef track, RaceConditions conditions,
                                    EquipObjective objective, int stage)
        {
            if (!car.AcceptsSlot(slot)) return null;

            double best = Evaluate(car, car.ActiveBuild, track, conditions, objective, stage);
            PartInstance winner = null;

            List<PartInstance> candidates = inventory.InSlot(slot);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Uid == car.ActiveBuild.SlotUids[(int)slot]) continue;

                SavedBuild hypothetical = Clone(car.ActiveBuild);
                hypothetical.SlotUids[(int)slot] = candidates[i].Uid;

                double value = Evaluate(car, hypothetical, track, conditions, objective, stage);
                if (value <= best) continue;

                best = value;
                winner = candidates[i];
            }

            return winner;
        }

        private static SavedBuild Clone(SavedBuild source)
        {
            return new SavedBuild
            {
                Name = source.Name,
                SlotUids = (int[])source.SlotUids.Clone(),
                TuneLock = source.TuneLock,
                TuneAccel = source.TuneAccel,
                TuneDecel = source.TuneDecel,
                Style = source.Style,
            };
        }
    }
}
