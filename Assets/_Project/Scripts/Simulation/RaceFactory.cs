// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 11, 14.2, 14.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Monta uma <see cref="RaceInstance"/> a partir do conteudo.
    ///
    /// Existe para que "qual pista, qual tier, qual stage, qual clima" seja resolvido
    /// UMA vez: a corrida visivel, a avaliacao do auto-equipar e a reconstrucao offline
    /// precisam montar a mesma instancia, e uma diferenca de um multiplicador entre elas
    /// e exatamente como o pilar P3 morre sem ninguem notar.
    /// </summary>
    public sealed class RaceFactory
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;

        public RaceFactory(ContentDatabase content)
        {
            _content = content;
            _balance = content.Balance;
        }

        public RaceInstance Build(TrackDef track, CarLoadout car, RaceConditions conditions,
                                  DriftStyle style, int stage, float prestigeMultiplier, ulong seed)
        {
            RegionDef region = _content.Region(track.RegionId);
            TierDef tier = _content.Tier(track.Tier);

            var race = new RaceInstance
            {
                Track = _content.BuildTrack(track),
                Car = car,
                Driver = DriverStats.Neutral,
                Conditions = conditions,
                Style = style,
                Seed = seed,

                TrackId = track.Id,
                TrackDisplayName = track.DisplayName,
                RegionId = region.Id,
                TierIndex = (int)track.Tier,
                Stage = MathUtil.Clamp(stage, 1, _balance.StagesPerTier),
                TrackBaseTimeSeconds = track.BaseTimeSeconds,
                TrackBaseCash = track.BaseCash,
                TrackBaseXp = track.BaseXp,
                PartPool = region.PartPool,
                PrestigeMultiplier = prestigeMultiplier,
                RegionIsUrban = region.IsUrban,
                RegionIsMountain = region.IsMountain,
            };

            race.DifficultyScale = DifficultyScale(tier, race.Stage);
            race.ScoreMultiplier = track.ScoreMultiplier * region.ScoreMultiplier;
            race.RewardMultiplier = region.CashMultiplier * StageRewardScale(race.Stage);

            return race;
        }

        /// <summary>
        /// Subir de stage aumenta ~4% na dificuldade (GDD 14.2). Subir de TIER muda a
        /// natureza - novos afixos, modulos, rivais e teto de raridade - e isso vem do
        /// conteudo, nao daqui.
        /// </summary>
        public float DifficultyScale(TierDef tier, int stage)
        {
            return tier.DifficultyScale * (1f + (stage - 1) * _balance.StageDifficultyStep);
        }

        /// <summary>
        /// Recompensa por stage, com o rendimento decrescente da secao 14.3.
        ///
        /// O sinal ao jogador precisa ser claro: SUBA DE TIER em vez de moer o mesmo tier.
        /// </summary>
        public float StageRewardScale(int stage)
        {
            var b = _balance;
            float raw = 1f + (stage - 1) * b.StageRewardStep;

            int over = stage - b.SoftCapStage;
            if (over <= 0) return raw;

            float decay = 1f - b.SoftCapStrength * MathUtil.Min(over, b.StagesPerTier) / (float)b.StagesPerTier;
            return raw * decay;
        }

        /// <summary>
        /// Sorteia o clima da regiao com os pesos declarados (GDD 11.5).
        ///
        /// Usa o fluxo de EVENTOS, nunca o de execucao: a previsao aparece com duas
        /// corridas de antecedencia e nao pode deslocar o resultado de nenhuma delas.
        /// </summary>
        public Weather RollWeather(RegionDef region, Rng.DeterministicRng eventsRng)
        {
            if (region.Weather == null || region.Weather.Length == 0) return Weather.Clear;

            var weights = new float[region.Weather.Length];
            for (int i = 0; i < weights.Length; i++) weights[i] = region.Weather[i].Weight;

            int index = eventsRng.WeightedIndex(weights);
            return region.Weather[index < 0 ? 0 : index].Weather;
        }
    }
}
