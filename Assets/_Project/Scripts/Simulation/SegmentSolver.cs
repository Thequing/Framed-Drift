// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 5.2 - 5.7, 12.3
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
    /// Resolve UM segmento: grip efetivo, velocidade de curva, decisao de drift,
    /// qualidade de execucao e estado continuo resultante.
    ///
    /// E aqui que vivem as formulas de 5.2 a 5.5. Nenhum numero literal neste arquivo -
    /// tudo vem de <see cref="BalanceSettings"/> (GDD 20.2).
    ///
    /// A ORDEM das chamadas ao RNG e parte do contrato: qualidade, depois proximidade,
    /// depois falha. Trocar a ordem muda o resultado de toda seed ja salva e invalida
    /// os replays (GDD 20.3).
    /// </summary>
    public sealed class SegmentSolver
    {
        private readonly BalanceSettings _balance;

        public SegmentSolver(BalanceSettings balance)
        {
            _balance = balance;
        }

        /// <summary>
        /// Contexto imutavel de uma corrida. Montado uma vez, lido por todos os segmentos.
        /// </summary>
        public sealed class Context
        {
            public ResolvedStats Stats;
            public CarLoadout Car;
            public RaceConditions Conditions;
            public DriftStyle Style;
            public float DriverSkill;
            public float DifficultyScale;
            public float TopSpeed;
            public float Acceleration;

            /// <summary>1 + soma dos modificadores de condicao. GDD 5.5.</summary>
            public float CondMod;

            /// <summary>estiloRisco. GDD 5.5.</summary>
            public float StyleRisk;

            /// <summary>estiloBias. GDD 5.4.</summary>
            public float StyleBias;

            /// <summary>RiskIndex 0-100 da corrida inteira. GDD 12.1.</summary>
            public float RiskIndex;

            /// <summary>Comprimento total, para saber a fracao percorrida.</summary>
            public float TotalDistanceM;

            /// <summary>+1 ou +2 de combo por Perfect. GDD 8.3 / 10.4.</summary>
            public int ComboPerPerfect;

            /// <summary>Bonus plano na chance de Perfect, vindo de passiva. GDD 10.4.</summary>
            public float PerfectChanceBonus;

            /// <summary>Fracao inicial da corrida em que a chuva nao reduz o grip. GDD 10.4.</summary>
            public float RainGraceFraction;

            public bool WeatherIsWet;
            public bool WeatherIsFoggy;
        }

        /// <summary>Monta o contexto de uma corrida a partir da instancia.</summary>
        public Context BuildContext(RaceInstance race)
        {
            var b = _balance;
            // O dano persistente ja foi aplicado pelo LoadoutResolver; aqui as stats sao finais.
            ResolvedStats stats = race.Car.Stats;

            bool wet = race.Conditions.Weather == Weather.Rain
                       || race.Conditions.Weather == Weather.HeavyRain
                       || race.Conditions.Weather == Weather.Snow;
            bool fog = race.Conditions.Weather == Weather.Fog;

            float condMod = 1f;
            if (wet) condMod += b.CondModRain;
            if (race.Conditions.TimeOfDay == TimeOfDay.Night) condMod += b.CondModNight;
            if (fog) condMod += b.CondModFog;
            if (race.Conditions.Traffic == TrafficDensity.Heavy) condMod += b.CondModHeavyTraffic;

            float totalDistance = 0f;
            float difficultySum = 0f;
            for (int i = 0; i < race.Track.Length; i++)
            {
                totalDistance += race.Track[i].LengthM;
                difficultySum += race.Track[i].Difficulty;
            }
            float avgDifficulty = race.Track.Length > 0 ? difficultySum / race.Track.Length : 0f;

            int comboPerPerfect = 1;
            if (race.Car.Trait == TraitId.RacingHeritage) comboPerPerfect = 2;
            float passiveCombo = race.Car.Passive(PassiveKind.ComboOnPerfect);
            if (passiveCombo > comboPerPerfect) comboPerPerfect = (int)passiveCombo;

            var ctx = new Context
            {
                Stats = stats,
                Car = race.Car,
                Conditions = race.Conditions,
                Style = race.Style,
                DriverSkill = race.Driver.DriverSkill,
                DifficultyScale = race.DifficultyScale,
                TopSpeed = Derived.TopSpeed(in stats, b) * (1f + race.Car.Passive(PassiveKind.TopSpeedBonus)),
                Acceleration = Derived.Acceleration(in stats, b) * (1f + race.Car.Passive(PassiveKind.AccelerationBonus)),
                CondMod = condMod,
                StyleRisk = b.StyleRisk(race.Style),
                StyleBias = b.StyleBias(race.Style),
                TotalDistanceM = totalDistance,
                ComboPerPerfect = comboPerPerfect,
                PerfectChanceBonus = race.Car.Passive(PassiveKind.PerfectChanceBonus),
                RainGraceFraction = race.Car.Passive(PassiveKind.RainGraceFraction),
                WeatherIsWet = wet,
                WeatherIsFoggy = fog,
            };

            ctx.RiskIndex = RiskIndex(race, avgDifficulty);
            return ctx;
        }

        /// <summary>
        /// Indice de risco, 0-100, exibido sempre na UI em quatro faixas. GDD 12.1.
        ///
        /// Risco alto aumenta recompensa, chance de drop raro e chance de falha - nessa
        /// ordem de destaque, porque e nessa ordem que o jogador precisa entende-los.
        /// </summary>
        public float RiskIndex(RaceInstance race, float avgDifficulty)
        {
            var b = _balance;
            ResolvedStats s = race.Car.Stats;

            float risk = race.TierIndex * b.RiskTierFactor
                         + avgDifficulty * b.RiskTrackFactor
                         + b.WeatherRiskOf(race.Conditions.Weather)
                         + b.TimeRisk(race.Conditions.TimeOfDay)
                         + b.TrafficRiskOf(race.Conditions.Traffic)
                         + b.StyleRiskIndex(race.Style)
                         - (s.Stability + s.Reliability) / b.RiskDefenseDivisor;

            return MathUtil.Clamp(risk, 0f, 100f);
        }

        // --- resolucao de um segmento -------------------------------------------------

        public SegmentOutcome Solve(int index, in Segment seg, Context ctx, ref RaceState state, DeterministicRng rng)
        {
            var b = _balance;

            var outcome = new SegmentOutcome
            {
                SegmentIndex = index,
                TimeOffset = state.TimeSeconds,
                LengthM = seg.LengthM,
                Quality = DriftQuality.Good,
                QualityFinal = DriftQuality.Good,
            };

            float progress = ctx.TotalDistanceM > 0f ? state.DistanceM / ctx.TotalDistanceM : 0f;
            float mu = EffectiveGrip(ctx, seg, state.TireWear, progress);

            float vEnter = state.SpeedKmh;
            float vUsed;
            float vExit;
            float duration;

            if (!seg.IsCurve)
            {
                ResolveStraight(ctx, seg, vEnter, out vUsed, out vExit, out duration);

                state.StraightRunM += seg.LengthM;
                if (state.StraightRunM >= b.ComboResetStraightMeters)
                {
                    // Reta longa zera o combo (GDD 5.7). E o que impede uma pista de
                    // retas de virar um multiplicador infinito de graca.
                    state.Combo = 0;
                    state.StraightRunM = 0f;
                }

                state.LastSegmentWasDriftCurve = false;
                outcome.Drift = false;
                outcome.Angle = 0f;
            }
            else
            {
                state.StraightRunM = 0f;

                float vGrip = CornerSpeed(ctx, mu, seg.Radius);
                float curvature = MathUtil.Clamp01(b.CurvatureReferenceRadius / seg.Radius);

                bool drift = WillDrift(ctx, curvature);
                bool isTransition = drift && IsTransition(seg, state);

                outcome.Drift = drift;
                outcome.IsTransition = isTransition;

                if (drift)
                {
                    outcome.Angle = ctx.Stats.Angle;
                    vUsed = DriftSpeed(ctx, vGrip, isTransition);

                    DriftQuality quality = RollQuality(ctx, seg, rng);
                    outcome.Quality = quality;
                    outcome.QualityFinal = quality;

                    // Promotable so quando o simulador rolou Good. Bad continua sendo erro
                    // do simulador e nunca vira Good por input (GDD 3.6.1).
                    outcome.Promotable = quality == DriftQuality.Good
                                         && rng.NextFloat() < b.PromotableCurveFraction;

                    switch (quality)
                    {
                        case DriftQuality.Perfect:
                            state.Combo += ctx.ComboPerPerfect;
                            state.PerfectSegments++;
                            vExit = vUsed;
                            break;
                        case DriftQuality.Bad:
                            state.Combo = 0;
                            state.BadSegments++;
                            vExit = vUsed * (1f - b.BadExitSpeedPenalty);
                            break;
                        default:
                            state.Combo++;
                            state.GoodSegments++;
                            vExit = vUsed;
                            break;
                    }

                    state.DriftSegments++;
                    if (seg.Walls != WallConfig.None)
                        outcome.Proximity = ResolveProximity(ctx, seg, rng, ref state, ref outcome, ref vExit);
                }
                else
                {
                    outcome.Angle = 0f;
                    vUsed = vGrip;
                    vExit = vGrip;
                }

                duration = CornerDuration(ctx, seg, vEnter, vUsed);
                state.LastCurveDirection = seg.Direction;
                state.LastSegmentWasDriftCurve = drift;
            }

            ResolveFailure(ctx, rng, ref state, ref outcome, ref vExit, ref duration);

            if (state.Combo > state.MaxCombo) state.MaxCombo = state.Combo;

            UpdateContinuousState(ctx, seg, outcome.Drift, ref state);

            outcome.VDrift = MathUtil.Max(1f, vUsed);
            outcome.VExit = MathUtil.Max(1f, vExit);
            outcome.Duration = duration;
            outcome.Combo = state.Combo;

            state.SpeedKmh = outcome.VExit;
            state.TimeSeconds += duration;
            state.DistanceM += seg.LengthM;

            return outcome;
        }

        // --- 5.2 grip efetivo ---------------------------------------------------------

        private float EffectiveGrip(Context ctx, in Segment seg, float wear, float progress)
        {
            var b = _balance;
            ResolvedStats s = ctx.Stats;

            float muBase = b.GripBase + (s.Grip / 100f) * b.GripSpan;

            float kTire = b.Tire(ctx.Car.Tire);
            if (ctx.Car.Tire == TireProfile.Wet)
            {
                if (ctx.WeatherIsWet) kTire = b.WetTireOnWetGrip;
                else if (progress > b.WetTireOverheatAfterFraction) kTire -= b.WetTireDryOverheatPenalty;
            }

            float kWeather = b.WeatherGripOf(ctx.Conditions.Weather);

            // Tracao integral ignora metade da penalidade de clima (GDD 8.3).
            if (ctx.Car.Trait == TraitId.AllWheelGrip && kWeather < 1f)
                kWeather = 1f - (1f - kWeather) * b.TraitAllWheelWeatherRelief;

            // "Chuva nao reduz o grip nos primeiros X% da corrida" (GDD 10.4).
            if (ctx.WeatherIsWet && ctx.RainGraceFraction > 0f && progress < ctx.RainGraceFraction)
                kWeather = 1f;

            float kSurface = b.SurfaceGripOf(seg.Surface);
            float kTime = b.TimeGrip(ctx.Conditions.TimeOfDay);
            float kWear = 1f - MathUtil.Clamp01(wear) * b.WearGripPenalty;

            return MathUtil.Max(0.05f, muBase * kTire * kWeather * kSurface * kTime * kWear);
        }

        // --- 5.3 velocidade de curva ---------------------------------------------------

        private float CornerSpeed(Context ctx, float mu, float radius)
        {
            var b = _balance;
            float v = b.MsToKmh * MathUtil.Sqrt(mu * b.Gravity * radius) * b.ArcadeSpeedFactor;
            return MathUtil.Min(v, ctx.TopSpeed);
        }

        // --- 5.4 grip x drift -----------------------------------------------------------

        private bool WillDrift(Context ctx, float curvature)
        {
            var b = _balance;
            float initiationScore = ctx.Stats.Initiation
                                    + Derived.PowerRatio(in ctx.Stats) * b.InitiationPowerWeightGain
                                    + ctx.StyleBias;
            float threshold = b.DriftThresholdBase - curvature * b.DriftThresholdCurvature;
            return initiationScore >= threshold;
        }

        private float DriftSpeed(Context ctx, float vGrip, bool isTransition)
        {
            var b = _balance;

            // Drift custa tempo: Angle 100 perde ~4%, Angle 30 perde 17% (GDD 5.4).
            float v = vGrip * (b.DriftSpeedFloor + ctx.Stats.Angle / b.DriftSpeedAngleSpan);

            if (isTransition)
                v += vGrip * (ctx.Stats.Transition / 100f) * b.TransitionSpeedBonus;

            return MathUtil.Min(v, ctx.TopSpeed);
        }

        private static bool IsTransition(in Segment seg, in RaceState state)
        {
            if (seg.Type == SegmentType.SCurve) return true;
            if (!state.LastSegmentWasDriftCurve) return false;
            if (state.LastCurveDirection == TurnDirection.None) return false;
            return seg.Direction != TurnDirection.None && seg.Direction != state.LastCurveDirection;
        }

        // --- 5.5 qualidade de execucao ---------------------------------------------------

        private DriftQuality RollQuality(Context ctx, in Segment seg, DeterministicRng rng)
        {
            var b = _balance;
            ResolvedStats s = ctx.Stats;

            float skill = s.DriftControl
                          + s.Steering * b.SkillSteeringFactor
                          + s.Stability * b.SkillStabilityFactor
                          + ctx.DriverSkill;

            float challenge = seg.Difficulty * ctx.CondMod * (1f + ctx.StyleRisk) * ctx.DifficultyScale;

            float x = (skill - challenge) / b.SkillSigmoidDivisor;
            float sig = MathUtil.Sigmoid(x);

            float pPerfect = MathUtil.Clamp(sig * b.PerfectCeiling + ctx.PerfectChanceBonus, b.PerfectMin, b.PerfectMax);
            float pBad = MathUtil.Clamp(b.BadBase - sig * b.BadSlope + ctx.Car.BadRiskBonus, b.BadMin, b.BadMax);

            // Motor central: +30% de chance de Bad (GDD 8.3).
            if (ctx.Car.Trait == TraitId.MidEngine)
                pBad = MathUtil.Clamp(pBad * b.TraitMidEngineBadFactor, b.BadMin, b.BadMax);

            // Os dois tetos podem se cruzar em extremos; Good nunca pode ficar negativo.
            if (pPerfect + pBad > 1f)
            {
                float excess = pPerfect + pBad - 1f;
                pBad = MathUtil.Max(0f, pBad - excess);
            }

            float roll = rng.NextFloat();
            if (roll < pPerfect) return DriftQuality.Perfect;
            if (roll < pPerfect + pBad) return DriftQuality.Bad;
            return DriftQuality.Good;
        }

        // --- 6.1 proximidade / 12.3 colisao -------------------------------------------------

        private float ResolveProximity(Context ctx, in Segment seg, DeterministicRng rng,
                                       ref RaceState state, ref SegmentOutcome outcome, ref float vExit)
        {
            var b = _balance;
            float p = MathUtil.Clamp01((ctx.Stats.DriftControl - seg.Difficulty) / 100f);

            float collisionChance = (1f - p) * b.ProximityCollisionBase * (1f + ctx.StyleRisk);
            if (seg.Walls == WallConfig.BothSides) collisionChance *= b.BothSidesCollisionFactor;

            if (rng.Chance(collisionChance))
            {
                outcome.Collision = true;
                state.Collisions++;
                state.Combo = 0;
                state.Damage += rng.Range(b.CollisionDamageMin, b.CollisionDamageMax);
                vExit *= 1f - b.CollisionSpeedPenalty;
                return 0f;
            }

            return p;
        }

        // --- 12.3 falha mecanica ----------------------------------------------------------

        private void ResolveFailure(Context ctx, DeterministicRng rng, ref RaceState state,
                                    ref SegmentOutcome outcome, ref float vExit, ref float duration)
        {
            var b = _balance;

            float overdrive = 0f;
            if (state.Temperature > b.TempOverheatThreshold)
                overdrive += (state.Temperature - b.TempOverheatThreshold) / MathUtil.Max(0.01f, 1f - b.TempOverheatThreshold);
            if (state.TireWear > b.WearOverdriveThreshold)
                overdrive += (state.TireWear - b.WearOverdriveThreshold) / MathUtil.Max(0.01f, 1f - b.WearOverdriveThreshold);

            // riskMult cresce com EXPOENTE, nao linearmente.
            //
            // A secao 12.3 fixa dois pontos: ~4% de chance de pelo menos uma falha numa
            // corrida de 25 segmentos em risco medio, e ~28% em risco extremo com build
            // fragil. Sao 8x de diferenca; um multiplicador linear em (1 + risco/100) so
            // consegue 2x entre as duas pontas, e o EXTREMO deixaria de assustar.
            float riskMult = MathUtil.Pow(1f + ctx.RiskIndex / 100f, b.RiskFailExponent);
            float reliabilityFactor = MathUtil.Max(0.05f, 1f - ctx.Stats.Reliability / b.FailReliabilityDivisor);

            float pFail = b.FailBasePerSegment * riskMult * (1f + overdrive) * reliabilityFactor;
            if (state.Temperature > b.TempOverheatThreshold) pFail *= b.TempOverheatFailFactor;

            if (!rng.Chance(pFail)) return;

            // Eventos negativos custam tempo, combo e dano - nunca terminam a corrida no
            // modo normal (GDD 12.2). So o Endless termina por falha.
            outcome.Failure = true;
            state.Failures++;
            state.Combo = 0;
            state.Damage += rng.Range(b.FailDamageMin, b.FailDamageMax);
            vExit *= 1f - b.FailSpeedPenalty;
            duration += b.FailTimePenaltySeconds;
        }

        // --- movimento ---------------------------------------------------------------------

        private void ResolveStraight(Context ctx, in Segment seg, float vEnterKmh,
                                     out float vUsed, out float vExit, out float duration)
        {
            var b = _balance;

            float accelKmhPerSec = b.StraightAccelPerIndex * ctx.Acceleration;
            float a = accelKmhPerSec / b.MsToKmh;                 // m/s^2
            float vIn = vEnterKmh / b.MsToKmh;
            float vMax = ctx.TopSpeed / b.MsToKmh;

            if (a <= 0.001f)
            {
                vExit = MathUtil.Min(vEnterKmh, ctx.TopSpeed);
                vUsed = vExit;
                duration = seg.LengthM / MathUtil.Max(1f, vExit / b.MsToKmh);
                return;
            }

            float vOut = MathUtil.Sqrt(vIn * vIn + 2f * a * seg.LengthM);
            if (vOut > vMax) vOut = vMax;

            float accelDistance = MathUtil.Clamp((vOut * vOut - vIn * vIn) / (2f * a), 0f, seg.LengthM);
            float t = (vOut - vIn) / a;
            float cruise = seg.LengthM - accelDistance;
            if (cruise > 0f) t += cruise / MathUtil.Max(1f, vOut);

            vExit = vOut * b.MsToKmh;
            vUsed = (vEnterKmh + vExit) * 0.5f;
            duration = MathUtil.Max(0.05f, t);
        }

        private float CornerDuration(Context ctx, in Segment seg, float vEnterKmh, float vThroughKmh)
        {
            var b = _balance;

            // O carro chega mais rapido que a velocidade de curva e freia na entrada; a
            // media entre as duas e o suficiente para a escala arcade da secao 5.6.
            float vAvg = vEnterKmh > vThroughKmh
                ? (vEnterKmh + vThroughKmh) * 0.5f
                : vThroughKmh;

            float t = seg.LengthM / MathUtil.Max(1f, vAvg / b.MsToKmh);

            // Freio: entra mais tarde na curva, encurta o tempo do segmento (GDD 7.1).
            float saved = MathUtil.Min(ctx.Stats.Braking * b.BrakingTimeSavedPerPoint, b.BrakingTimeSaveMax);
            return MathUtil.Max(0.05f, t * (1f - saved));
        }

        // --- 5.7 estado continuo ------------------------------------------------------------

        private void UpdateContinuousState(Context ctx, in Segment seg, bool drifted, ref RaceState state)
        {
            var b = _balance;
            float km = seg.LengthM / 1000f;

            float wearRate = b.TireWearPerKm * (1f + b.TireWearStyleFactor * ctx.StyleRisk);
            if (drifted) wearRate *= b.TireWearDriftFactor;
            state.TireWear = MathUtil.Clamp01(state.TireWear + wearRate * km);

            float heat = b.TempRisePerKm * km * (drifted ? b.DriftHeatFactor : 1f);
            float cool = b.TempCoolingFactor * ctx.Stats.Cooling * km;
            state.Temperature = MathUtil.Clamp01(state.Temperature + heat - cool);
        }
    }
}
