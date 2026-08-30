// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 6, 3.6, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>
    /// Reduz uma Timeline a um Drift Score.
    ///
    /// Deliberadamente separado do simulador (GDD 20.6): a mesma funcao, chamada com a
    /// timeline intocada, produz o resultado offline. E o que torna a paridade de D-02
    /// estrutural em vez de algo a testar.
    ///
    /// Nao consome RNG. Se consumisse, pontuar duas vezes a mesma timeline daria numeros
    /// diferentes e a medicao de uplift da secao 6.2 seria impossivel.
    /// </summary>
    public sealed class DriftScorer
    {
        private readonly BalanceSettings _balance;

        public DriftScorer(BalanceSettings balance)
        {
            _balance = balance;
        }

        /// <summary>Multiplicadores de corrida da secao 6.2, ja compostos pelo chamador.</summary>
        public struct Context
        {
            public float TrackMult;
            public float WeatherMult;
            public float TimeMult;
            public float EventMult;
            public float PrestigeMult;

            /// <summary>Teto de combo efetivo, ja incluindo a passiva de duracao. GDD 10.4.</summary>
            public int ComboCap;

            /// <summary>Fator multiplicativo vindo das passivas condicionais. GDD 10.4.</summary>
            public float PassiveScoreMult;

            /// <summary>Limiar de velocidade da passiva "score acima de X km/h".</summary>
            public float SpeedPassiveThreshold;
            public float SpeedPassiveBonus;

            public static Context Neutral(BalanceSettings b)
            {
                return new Context
                {
                    TrackMult = 1f,
                    WeatherMult = 1f,
                    TimeMult = 1f,
                    EventMult = 1f,
                    PrestigeMult = 1f,
                    ComboCap = b.ComboMultCap,
                    PassiveScoreMult = 1f,
                };
            }

            public float TotalMultiplier
            {
                get { return TrackMult * WeatherMult * TimeMult * EventMult * PrestigeMult * PassiveScoreMult; }
            }
        }

        /// <summary>
        /// Monta o contexto a partir da corrida e das passivas do carro. Nao le a
        /// timeline: os multiplicadores da corrida sao os mesmos com ou sem promocao.
        /// </summary>
        public Context BuildContext(RaceInstance race)
        {
            var b = _balance;
            CarLoadout car = race.Car;

            var ctx = Context.Neutral(b);
            ctx.TrackMult = race.ScoreMultiplier;
            ctx.WeatherMult = b.WeatherRewardOf(race.Conditions.Weather);
            ctx.TimeMult = b.TimeReward(race.Conditions.TimeOfDay);
            ctx.PrestigeMult = race.PrestigeMultiplier;

            float comboCapBonus = car.Passive(PassiveKind.ComboCapBonus);
            ctx.ComboCap = (int)MathUtil.Max(1f, b.ComboMultCap * (1f + comboCapBonus));

            float passiveMult = 1f + car.Passive(PassiveKind.ScoreBonus);

            if (race.Conditions.TimeOfDay == TimeOfDay.Night)
                passiveMult += car.Passive(PassiveKind.ScoreAtNight);

            if (race.Conditions.Weather == Weather.Rain || race.Conditions.Weather == Weather.HeavyRain)
                passiveMult += car.Passive(PassiveKind.ScoreInRain);

            ctx.PassiveScoreMult = MathUtil.Max(0.01f, passiveMult);
            ctx.SpeedPassiveBonus = car.Passive(PassiveKind.ScoreAboveSpeed);
            ctx.SpeedPassiveThreshold = car.PassiveThreshold(PassiveKind.ScoreAboveSpeed);

            return ctx;
        }

        /// <summary>
        /// O score de UM segmento, ja usando <see cref="SegmentOutcome.QualityFinal"/>.
        ///
        /// Toda a contribuicao da presenca do jogador entra por aqui: nao existe um
        /// multiplicador separado de "jogador presente", e por isso o teto de 20-30% da
        /// secao 3.6 e uma propriedade EMERGENTE, que precisa ser medida (ver
        /// <see cref="MeasureUplift"/>) e nao assumida.
        /// </summary>
        public double SegmentScore(in SegmentOutcome o, in Context ctx)
        {
            if (!o.Drift) return 0.0;

            var b = _balance;

            float baseScore = o.LengthM * b.ScoreBasePerMeter;

            // Angle_efetivo: o Angle bruto modulado pela QUALIDADE FINAL (GDD 6.1).
            // Perfect usa 100% do Angle, Good 85%, Bad 45% - e por isso uma promocao
            // move duas alavancas de uma vez, angleMult e qualityMult.
            float effectiveAngle = o.Angle * b.QualityAngle(o.QualityFinal);
            float angleMult = b.AngleMultBase + effectiveAngle / b.AngleMultSpan;

            float speedMult = MathUtil.Clamp(o.VDrift / b.SpeedMultDivisor, b.SpeedMultMin, b.SpeedMultMax);
            float qualityMult = b.Quality(o.QualityFinal);

            int combo = o.Combo < ctx.ComboCap ? o.Combo : ctx.ComboCap;
            float comboMult = 1f + b.ComboMultStep * combo;

            float proxMult = 1f + o.Proximity * b.ProximityMultSpan;
            float transMult = o.IsTransition ? b.TransitionMult : 1f;

            double seg = (double)baseScore * angleMult * speedMult * qualityMult * comboMult * proxMult * transMult;

            if (ctx.SpeedPassiveBonus > 0f && o.VDrift >= ctx.SpeedPassiveThreshold)
                seg *= 1f + ctx.SpeedPassiveBonus;

            return seg;
        }

        /// <summary>
        /// O total da corrida. GDD 6.2.
        ///
        /// Piso em double e chao no fim: um endgame de 12 milhoes acumulado em float
        /// perderia unidades inteiras no ultimo segmento e o mesmo replay daria numeros
        /// diferentes dependendo da ordem da soma.
        /// </summary>
        public long Score(SegmentOutcome[] timeline, in Context ctx)
        {
            if (timeline == null) return 0L;

            double sum = 0.0;
            for (int i = 0; i < timeline.Length; i++)
                sum += SegmentScore(in timeline[i], in ctx);

            double total = sum * ctx.TotalMultiplier;
            if (total <= 0.0) return 0L;
            return (long)System.Math.Floor(total);
        }

        /// <summary>Score de uma corrida offline: a timeline intocada. GDD 20.6.</summary>
        public long ScoreWithoutPromotions(RaceResult result, in Context ctx)
        {
            if (result == null || result.Timeline == null) return 0L;

            double sum = 0.0;
            for (int i = 0; i < result.Timeline.Length; i++)
            {
                SegmentOutcome o = result.Timeline[i];
                o.QualityFinal = o.Quality;
                sum += SegmentScore(in o, in ctx);
            }

            double total = sum * ctx.TotalMultiplier;
            return total <= 0.0 ? 0L : (long)System.Math.Floor(total);
        }

        /// <summary>Score com TODAS as janelas promovidas: o teto da presenca. GDD 6.2.</summary>
        public long ScoreWithAllPromotions(RaceResult result, in Context ctx)
        {
            if (result == null || result.Timeline == null) return 0L;

            double sum = 0.0;
            for (int i = 0; i < result.Timeline.Length; i++)
            {
                SegmentOutcome o = result.Timeline[i];
                o.QualityFinal = o.Promotable ? DriftQuality.Perfect : o.Quality;
                sum += SegmentScore(in o, in ctx);
            }

            double total = sum * ctx.TotalMultiplier;
            return total <= 0.0 ? 0L : (long)System.Math.Floor(total);
        }

        /// <summary>
        /// uplift = (score com todas as promocoes - score sem nenhuma) / score sem nenhuma.
        ///
        /// O teste de balanceamento obrigatorio da secao 6.2 mede isto sobre 1.000
        /// corridas e exige [0,20 - 0,30]. Fora disso, o parafuso e
        /// <c>promotableCurveFraction</c> ou a janela de input, no balance.json.
        /// </summary>
        public double MeasureUplift(RaceResult result, in Context ctx)
        {
            long without = ScoreWithoutPromotions(result, in ctx);
            if (without <= 0L) return 0.0;
            long with = ScoreWithAllPromotions(result, in ctx);
            return (with - without) / (double)without;
        }
    }
}
