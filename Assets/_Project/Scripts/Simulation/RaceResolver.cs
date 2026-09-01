// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 3.6, 11.6, 15.2, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// A fachada que junta simulacao, pontuacao e recompensa nas duas ordens em que o
    /// jogo precisa delas.
    ///
    /// <b>Online:</b> <see cref="Simulate"/> devolve a timeline, o visualizador toca,
    /// o jogador promove curvas, e so entao <see cref="Finalize"/> pontua.
    /// <b>Offline:</b> <see cref="ResolveComplete"/> faz as duas coisas em sequencia,
    /// sem promocao nenhuma.
    ///
    /// As duas passam pela MESMA funcao de score. E isso, e nao um teste, que faz a
    /// promessa de D-02 ("offline rende 100%") ser literalmente verdadeira.
    /// </summary>
    public sealed class RaceResolver
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;

        public readonly RaceSimulator Simulator;
        public readonly DriftScorer Scorer;
        public readonly LootRoller Loot;

        public RaceResolver(ContentDatabase content)
        {
            _content = content;
            _balance = content.Balance;
            Simulator = new RaceSimulator(_balance);
            Scorer = new DriftScorer(_balance);
            Loot = new LootRoller(content);
        }

        /// <summary>Fase 1: resolve a fisica. Nao pontua e nao gera recompensa.</summary>
        public RaceResult Simulate(RaceInstance race)
        {
            return Simulator.Simulate(race);
        }

        /// <summary>
        /// Fase 2: pontua a timeline (no estado em que ela estiver) e gera recompensa.
        ///
        /// Chamar isto com a timeline intocada devolve exatamente o resultado offline.
        /// </summary>
        public RaceRewards Finalize(RaceInstance race, RaceResult result, CollectibleHaul haul)
        {
            DriftScorer.Context scoreCtx = Scorer.BuildContext(race);
            long score = Scorer.Score(result.Timeline, in scoreCtx);

            var streams = new RngStreams(race.Seed);
            RaceRewards rewards = ComputeRewards(race, result, score, haul, streams.Loot, streams.Events);

            result.Rewards = rewards;
            result.CollectiblesTaken = haul.Count;
            return rewards;
        }

        /// <summary>Caminho offline e caminho de avaliacao: simula e finaliza sem presenca.</summary>
        public RaceResult ResolveComplete(RaceInstance race)
        {
            RaceResult result = Simulate(race);
            Finalize(race, result, CollectibleHaul.None);
            return result;
        }

        /// <summary>
        /// Score que a corrida renderia sem nenhuma promocao - o numero que o auto-equipar
        /// compara (GDD 16.3) e o que a agregacao offline usa (GDD 17.2).
        /// </summary>
        public long ScoreOffline(RaceInstance race, RaceResult result)
        {
            DriftScorer.Context ctx = Scorer.BuildContext(race);
            return Scorer.ScoreWithoutPromotions(result, in ctx);
        }

        // --- recompensa (15.2) ------------------------------------------------------

        private RaceRewards ComputeRewards(RaceInstance race, RaceResult result, long score,
                                           CollectibleHaul haul, DeterministicRng lootRng,
                                           DeterministicRng eventRng)
        {
            var b = _balance;

            float rewardMult = RewardMultiplier(race);
            float positionMult = b.PositionMultiplier(result.Position);
            float repBonus = 1f;

            double cash = (race.TrackBaseCash + score * (double)b.CashPerScore)
                          * positionMult * rewardMult * repBonus;

            // Carro de rua: +20% em pistas urbanas, -20% em montanha (GDD 8.3).
            if (race.Car.Trait == TraitId.StreetCar)
            {
                if (race.RegionIsUrban) cash *= b.TraitStreetCarUrbanCash;
                else if (race.RegionIsMountain) cash *= b.TraitStreetCarMountainCash;
            }

            cash *= 1f + race.Car.Passive(PassiveKind.CashBonus);

            // "Cada colisao evitada por proximidade concede +5% de cash" (GDD 10.4).
            float perProximity = race.Car.Passive(PassiveKind.CashPerProximity);
            if (perProximity > 0f) cash *= 1f + perProximity * CountProximitySaves(result);

            cash += haul.CashBonus;

            double xp = (race.TrackBaseXp + score * (double)b.XpPerScore) * rewardMult;

            int reputation = 0;
            if (result.Position == 1) reputation += b.ReputationPerWin;
            else if (result.Position <= 3) reputation += b.ReputationPerPodium;
            reputation += haul.ReputationBonus;

            PartDrop[] drops = Loot.RollDrops(race, result, score, haul.DropChanceBonus, lootRng);

            string fragment = Loot.RollBlueprintFragment(race, eventRng);
            string[] blueprints = fragment == null
                ? RaceRewards.NoBlueprints
                : new[] { fragment };

            return new RaceRewards
            {
                Cash = (long)System.Math.Floor(cash),
                Xp = (int)System.Math.Floor(xp),
                Reputation = reputation,
                Drops = drops,
                BlueprintIds = blueprints,
                DriftScore = score,
            };
        }

        /// <summary>
        /// rewardMult = timeMult * weatherMult * trafficMult * riskMult, GRAMPEADO no teto
        /// da secao 11.6, e SO ENTAO multiplicado pelo tierMult.
        ///
        /// A 11.6 escreve o tier dentro do produto, mas os exemplos que DEFINEM o teto de
        /// 4,5 sao todos de condicao - "Montanha, 02h, tempestade" - com o tier parado.
        /// O teto existe para impedir que horario, clima, trafego e regiao se empilhem
        /// sem limite; e um teto de CONDICAO.
        ///
        /// Grampear o tier junto quebra a progressao no topo: o rewardScale de S e 6,20 e
        /// estoura 4,5 sozinho, em pista seca, as duas da tarde, sem trafego. Tier A e S
        /// pagariam o MESMO que um tier B em tempestade, e a 14.2 - "suba de tier em vez
        /// de moer o mesmo tier" - passaria a mentir exatamente onde mais importa.
        ///
        /// Entao o teto continua valendo inteiro para o que ele foi escrito, e o tier
        /// multiplica por fora.
        /// </summary>
        public float RewardMultiplier(RaceInstance race)
        {
            var b = _balance;
            TierDef tier = _content.Tier((TierRank)MathUtil.Clamp(race.TierIndex, 0, 4));

            float conditions = b.TimeReward(race.Conditions.TimeOfDay)
                               * b.WeatherRewardOf(race.Conditions.Weather)
                               * b.TrafficRewardOf(race.Conditions.Traffic)
                               * race.RewardMultiplier;

            return MathUtil.Min(conditions, b.RewardMultCap) * tier.RewardScale;
        }

        private static int CountProximitySaves(RaceResult result)
        {
            if (result.Timeline == null) return 0;
            int n = 0;
            for (int i = 0; i < result.Timeline.Length; i++)
                if (result.Timeline[i].Proximity > 0f && !result.Timeline[i].Collision) n++;
            return n;
        }
    }

    /// <summary>
    /// O que o jogador apanhou clicando durante a corrida visivel (GDD 3.6.2).
    ///
    /// Coletaveis NAO existem na agregacao offline. Nao sao "perdidos" na ausencia -
    /// eles simplesmente nao sao gerados. E isso que mantem a promessa de D-02
    /// literalmente verdadeira, e nao apenas tecnicamente verdadeira.
    /// </summary>
    public struct CollectibleHaul
    {
        public long CashBonus;
        public float DropChanceBonus;
        public int ReputationBonus;
        public int Count;

        public static readonly CollectibleHaul None = new CollectibleHaul();
    }
}
