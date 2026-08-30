// -----------------------------------------------------------------------------
//  Framed Drift  -  Progression
//  GDD 0.2  secao 14.7
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Progression
{
    public enum MissionScope { Daily, Weekly, Lifetime }

    public enum MissionMetric
    {
        RacesFinished,
        Wins,
        NightWins,
        DriftDistanceMeters,
        DriftScoreTotal,
        RarePartsFound,
        RivalsDefeated,
        PerfectCurves,
    }

    [Serializable]
    public sealed class Mission
    {
        public string Id;
        public string Description;
        public MissionScope Scope;
        public MissionMetric Metric;
        public long Target;
        public long Progress;
        public long RewardCash;
        public int RewardReputation;
        public bool Claimed;

        public bool Complete { get { return Progress >= Target; } }
        public float Fraction { get { return Target <= 0 ? 1f : UnityEngine.Mathf.Clamp01(Progress / (float)Target); } }
    }

    /// <summary>
    /// Missoes diarias, semanais e vitalicias.
    ///
    /// A regra que governa esta classe: TODA missao precisa ser cumprivel PASSIVAMENTE
    /// pela automacao (GDD 14.7). Uma missao que exige presenca e banida pelo pilar P2 -
    /// e por isso nenhuma metrica aqui envolve clique, Entrada Perfeita ou coletavel.
    /// Todas medem o que o carro fez, esteja o jogador olhando ou nao.
    /// </summary>
    public sealed class MissionSystem
    {
        private readonly List<Mission> _missions = new List<Mission>();

        public IReadOnlyList<Mission> Missions { get { return _missions; } }

        public MissionSystem()
        {
            SeedDefaults();
        }

        private void SeedDefaults()
        {
            Add("d_races", "Complete 20 corridas", MissionScope.Daily, MissionMetric.RacesFinished, 20, 1200, 2);
            Add("d_drift", "Acumule 5 km em drift", MissionScope.Daily, MissionMetric.DriftDistanceMeters, 5000, 1500, 2);
            Add("d_night", "Venca 5 corridas noturnas", MissionScope.Daily, MissionMetric.NightWins, 5, 1800, 3);

            Add("w_races", "Complete 100 corridas", MissionScope.Weekly, MissionMetric.RacesFinished, 100, 9000, 8);
            Add("w_rare", "Encontre 3 pecas raras", MissionScope.Weekly, MissionMetric.RarePartsFound, 3, 12000, 10);
            Add("w_rival", "Derrote um rival", MissionScope.Weekly, MissionMetric.RivalsDefeated, 1, 15000, 12);

            Add("l_score", "1.000.000 de Drift Score", MissionScope.Lifetime, MissionMetric.DriftScoreTotal, 1000000, 25000, 15);
            Add("l_races", "1.000 corridas", MissionScope.Lifetime, MissionMetric.RacesFinished, 1000, 40000, 20);
            Add("l_perfect", "5.000 curvas Perfect", MissionScope.Lifetime, MissionMetric.PerfectCurves, 5000, 60000, 25);
        }

        private void Add(string id, string description, MissionScope scope, MissionMetric metric,
                         long target, long cash, int reputation)
        {
            _missions.Add(new Mission
            {
                Id = id,
                Description = description,
                Scope = scope,
                Metric = metric,
                Target = target,
                RewardCash = cash,
                RewardReputation = reputation,
            });
        }

        /// <summary>Contabiliza uma corrida terminada. Online e offline chamam o mesmo metodo.</summary>
        public void RecordRace(RaceResult result, bool atNight)
        {
            Advance(MissionMetric.RacesFinished, 1);
            if (result.Won)
            {
                Advance(MissionMetric.Wins, 1);
                if (atNight) Advance(MissionMetric.NightWins, 1);
            }

            Advance(MissionMetric.DriftScoreTotal, result.Rewards != null ? result.Rewards.DriftScore : 0L);
            Advance(MissionMetric.PerfectCurves, result.PerfectSegments);

            long driftMeters = 0;
            if (result.Timeline != null)
                for (int i = 0; i < result.Timeline.Length; i++)
                    if (result.Timeline[i].Drift) driftMeters += (long)result.Timeline[i].LengthM;
            Advance(MissionMetric.DriftDistanceMeters, driftMeters);

            if (result.Rewards != null)
                for (int i = 0; i < result.Rewards.Drops.Length; i++)
                    if (result.Rewards.Drops[i].Rarity >= Simulation.Model.Rarity.Rare)
                        Advance(MissionMetric.RarePartsFound, 1);
        }

        /// <summary>Agrega uma ausencia inteira de uma vez. GDD 17.2.</summary>
        public void RecordOffline(OfflineReport report)
        {
            Advance(MissionMetric.RacesFinished, report.Races);
            Advance(MissionMetric.Wins, report.Wins);
            Advance(MissionMetric.DriftScoreTotal, report.DriftScore);

            for (int r = (int)Simulation.Model.Rarity.Rare; r < report.DropsByRarity.Length; r++)
                Advance(MissionMetric.RarePartsFound, report.DropsByRarity[r]);
        }

        public void Advance(MissionMetric metric, long amount)
        {
            if (amount <= 0) return;

            for (int i = 0; i < _missions.Count; i++)
            {
                Mission m = _missions[i];
                if (m.Metric != metric || m.Claimed) continue;

                m.Progress += amount;
                if (m.Progress > m.Target) m.Progress = m.Target;
            }
        }

        /// <summary>Coleta uma missao concluida. Devolve false quando nao ha o que coletar.</summary>
        public bool Claim(Mission mission, EconomyLedger ledger)
        {
            if (mission == null || !mission.Complete || mission.Claimed) return false;

            mission.Claimed = true;
            ledger.AddCash(mission.RewardCash);
            ledger.AddReputation(mission.RewardReputation);
            EventBus.Publish(new UnlockGranted
            {
                Kind = "missao",
                Id = mission.Id,
                DisplayName = mission.Description,
            });
            return true;
        }

        /// <summary>Reseta um escopo. As diarias giram com o relogio real, nao com o do jogo.</summary>
        public void Reset(MissionScope scope)
        {
            for (int i = 0; i < _missions.Count; i++)
            {
                if (_missions[i].Scope != scope) continue;
                _missions[i].Progress = 0;
                _missions[i].Claimed = false;
            }
        }
    }
}
