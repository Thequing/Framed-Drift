// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Balance
//  GDD 0.2  secoes 5, 6, 7.2, 11.4, 12, 14, 15, 17, 19.2, 20.2, apendice B
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Balance
{
    /// <summary>
    /// Todas as constantes das formulas da GDD, em C# puro.
    ///
    /// NENHUM numero de balanceamento aparece em codigo de gameplay (GDD 20.2). Os
    /// valores vivem em Content/balance.json e sao carregados por
    /// <c>FramedDrift.Simulation.Content.ContentLoader</c>. Os campos aqui ficam sem
    /// inicializador de proposito: a fonte da verdade e o arquivo, nao este arquivo.
    ///
    /// O comentario ao lado de cada campo e o valor do apendice B - documentacao do que
    /// o arquivo DEVE conter, nao um default silencioso.
    /// </summary>
    [System.Serializable]
    public class BalanceSettings
    {
        // --- grip efetivo (5.2) ------------------------------------------------
        public float GripBase;                  // 0.60
        public float GripSpan;                  // 0.60
        public float WearGripPenalty;           // 0.35

        /// <summary>k_tire, indexado por <see cref="TireProfile"/>.</summary>
        public float[] TireGrip;                // Street .92 Sport 1.00 SemiSlick 1.08 Drift .95 Racing 1.15 Wet .88

        /// <summary>k_tire do pneu Wet quando a pista esta molhada.</summary>
        public float WetTireOnWetGrip;          // 1.30

        /// <summary>Penalidade extra do pneu Wet superaquecendo em pista seca. GDD 5.2.</summary>
        public float WetTireDryOverheatPenalty; // 0.15
        public float WetTireOverheatAfterFraction; // 0.40

        /// <summary>k_weather, indexado por <see cref="Weather"/>.</summary>
        public float[] WeatherGrip;             // Clear 1.00 Cloudy 1.00 Rain .78 HeavyRain .66 Fog .95 Snow .55

        /// <summary>k_surface, indexado por <see cref="SurfaceType"/>.</summary>
        public float[] SurfaceGrip;             // Asphalt 1.00 Worn .94 Concrete .97 Gravel .72 Snow .58

        // --- velocidade de curva (5.3) ----------------------------------------
        public float ArcadeSpeedFactor;         // k_arcade = 1.25
        public float Gravity;                   // 9.81
        public float MsToKmh;                   // 3.6

        /// <summary>Aceleracao em reta, em km/h por segundo por ponto de indice.</summary>
        public float StraightAccelPerIndex;     // 0.9
        public float BrakingTimeSavedPerPoint;  // 0.0016 - fracao de tempo do segmento por ponto de Braking

        // --- grip x drift (5.4) ------------------------------------------------
        public float DriftThresholdBase;        // 55
        public float DriftThresholdCurvature;   // 20
        public float InitiationPowerWeightGain; // 0.05
        public float DriftSpeedFloor;           // 0.78
        public float DriftSpeedAngleSpan;       // 600
        public float TransitionSpeedBonus;      // 0.10

        /// <summary>estiloBias, indexado por <see cref="DriftStyle"/>.</summary>
        public float[] StyleInitiationBias;     // Safe -25 Balanced 0 Aggressive +18 Reckless +35

        /// <summary>estiloRisco, indexado por <see cref="DriftStyle"/>.</summary>
        public float[] StyleRiskFactor;         // Safe -0.20 Balanced 0 Aggressive +0.15 Reckless +0.35

        /// <summary>Curvatura de referencia: raio abaixo do qual a curvatura vale 1.</summary>
        public float CurvatureReferenceRadius;  // 30

        // --- qualidade de execucao (5.5) --------------------------------------
        public float SkillSteeringFactor;       // 0.4
        public float SkillStabilityFactor;      // 0.3
        public float SkillSigmoidDivisor;       // 18
        public float PerfectCeiling;            // 0.68
        public float PerfectMin;                // 0.02
        public float PerfectMax;                // 0.75
        public float BadBase;                   // 0.38
        public float BadSlope;                  // 0.34
        public float BadMin;                    // 0.03
        public float BadMax;                    // 0.48

        /// <summary>qualityMult, indexado por <see cref="DriftQuality"/>.</summary>
        public float[] QualityMult;             // Bad 0.35 Good 1.00 Perfect 1.60

        /// <summary>Fracao do Angle usada por qualidade. GDD 6.1.</summary>
        public float[] QualityAngleFactor;      // Bad 0.45 Good 0.85 Perfect 1.00

        public float BadExitSpeedPenalty;       // 0.12

        // condMod (5.5), cumulativos
        public float CondModRain;               // 0.12
        public float CondModNight;              // 0.08
        public float CondModFog;                // 0.15
        public float CondModHeavyTraffic;       // 0.10

        // --- tempo e posicao (5.6) ---------------------------------------------
        public float TierTimeScale;             // 0.018
        public float OpponentSigmaFraction;     // 0.06
        public int GridSize;                    // 6

        // --- estado continuo (5.7) ---------------------------------------------
        public float TireWearPerKm;             // 0.16
        public float TireWearStyleFactor;       // 0.55 - quanto o estilo agressivo acelera o desgaste
        public float TireWearDriftFactor;       // 1.8
        public float TempRisePerKm;             // 0.12
        public float TempCoolingFactor;         // 0.006 - por ponto de Cooling
        public float TempOverheatThreshold;     // 0.80
        public float TempOverheatPowerPenalty;  // 0.10
        public float TempOverheatFailFactor;    // 2.0
        public float ComboResetStraightMeters;  // 220 - reta longa zera o combo (GDD 5.7)

        // --- drift score (6.1) -------------------------------------------------
        public float ScoreBasePerMeter;         // 10
        public float AngleMultBase;             // 0.50
        public float AngleMultSpan;             // 90
        public float SpeedMultDivisor;          // 120
        public float SpeedMultMin;              // 0.40
        public float SpeedMultMax;              // 2.50
        public float ComboMultStep;             // 0.08
        public int ComboMultCap;                // 25
        public float ProximityMultSpan;         // 0.30
        public float TransitionMult;            // 1.25

        // --- stats derivadas (7.2) ---------------------------------------------
        public float AccelBase;                 // 40
        public float AccelPerPowerRatio;        // 0.09
        public float TopSpeedBase;              // 130
        public float TopSpeedPerPower;          // 0.20
        public float TopSpeedPerAero;           // 0.15
        public float TopSpeedPerKg;             // 0.010

        // --- horario e clima (11.4 / 11.6) -------------------------------------
        /// <summary>Indexado por <see cref="TimeOfDay"/>: Day, Night.</summary>
        public float[] TimeOfDayGrip;           // Day 1.00 Night 0.97
        public float[] TimeOfDayReward;         // Day 1.00 Night 1.60
        public float[] TimeOfDayRisk;           // Day 5 Night 25

        /// <summary>Indexado por <see cref="Weather"/>.</summary>
        public float[] WeatherReward;           // Clear 1.00 ... HeavyRain 1.80
        public float[] WeatherRisk;             // Clear 0 ... HeavyRain 30

        /// <summary>Indexado por <see cref="TrafficDensity"/>.</summary>
        public float[] TrafficReward;           // None 1.00 Light 1.05 Medium 1.12 Heavy 1.22
        public float[] TrafficRisk;             // None 0 Light 4 Medium 10 Heavy 18

        /// <summary>Teto pratico de rewardMult. GDD 11.6 - "o teto pratico: x4.5".</summary>
        public float RewardMultCap;             // 4.5

        // --- risco e falha (12) -------------------------------------------------
        public float RiskTierFactor;            // 6 - por indice de tier
        public float RiskTrackFactor;           // 0.35 - por ponto de dificuldade media
        public float[] StyleRiskBias;           // Safe -20 Balanced 0 Aggressive +15 Reckless +35
        public float RiskDefenseDivisor;        // 4 - (Stability + Reliability) / 4
        public float FailBasePerSegment;        // 0.004
        public float FailReliabilityDivisor;    // 200

        /// <summary>Expoente de riskMult na chance de falha. Ver SegmentSolver. GDD 12.3.</summary>
        public float RiskFailExponent;          // 3.5
        public float FailDamageMin;             // 8
        public float FailDamageMax;             // 25
        public float FailTimePenaltySeconds;    // 2.5
        public float FailSpeedPenalty;          // 0.45
        public float CollisionDamageMin;        // 3
        public float CollisionDamageMax;        // 9
        public float CollisionSpeedPenalty;     // 0.25
        public float CollisionTimePenaltySeconds; // 0.8
        public float DamageStatPenaltyAt100;    // 0.35 - dano 100 tira 35% de todas as stats

        /// <summary>Chance de colisao ao raspar a parede com proximidade zero. GDD 12.3.</summary>
        public float ProximityCollisionBase;    // 0.13

        /// <summary>Desgaste acima do qual o pneu entra em overdrive e a falha sobe. GDD 12.3.</summary>
        public float WearOverdriveThreshold;    // 0.85

        /// <summary>Velocidade na largada, em km/h.</summary>
        public float StartSpeedKmh;             // 30

        /// <summary>Teto da economia de tempo por Braking, para o freio nao virar teleporte.</summary>
        public float BrakingTimeSaveMax;        // 0.18

        // --- traits (8.3) -------------------------------------------------------
        // Cada trait tem UMA frase na tabela 8.3; os numeros dessa frase vivem aqui,
        // e nao no codigo, pela mesma regra que vale para todo o resto (GDD 20.2).
        public float TraitLightChassisWeightFactor;  // 0.92  (-8% Weight efetivo)
        public float TraitLightChassisStability;     // -10
        public float TraitRawTorqueInitiation;       // +20
        public float TraitRawTorqueGrip;             // -8
        public float TraitAllWheelWeatherRelief;     // 0.50  (ignora metade da penalidade)
        public float TraitAllWheelAngle;             // -15
        public float TraitMidEngineTransition;       // +25
        public float TraitMidEngineBadFactor;        // 1.30
        public float TraitFactoryCooling;            // +30
        public float TraitStreetCarUrbanCash;        // 1.20
        public float TraitStreetCarMountainCash;     // 0.80

        // --- ajuste fino do diferencial (9.2) -------------------------------------
        // Amplitude de cada eixo no extremo do slider. Cada linha e um trade-off: o
        // ganho e a perda vivem juntos porque nenhum eixo pode ser "mais e melhor".
        public float TuningLockAngle;                // +20 Angle no extremo
        public float TuningLockGrip;                 // -12 Grip no extremo
        public float TuningAccelInitiation;          // +18
        public float TuningAccelStability;           // -14
        public float TuningDecelTransition;          // +20
        public float TuningDecelBadRisk;             // +0.06 de chance de Bad no extremo

        // --- fatores de segmento -------------------------------------------------
        public float BothSidesCollisionFactor;       // 1.35
        public float DriftHeatFactor;                // 1.30

        // --- progressao (14) ----------------------------------------------------
        public int StagesPerTier;               // 20
        public float StageDifficultyStep;       // 0.04
        public float StageRewardStep;           // 0.06
        public int SoftCapStage;                // 12
        public float SoftCapStrength;           // 0.5

        // --- economia (15) ------------------------------------------------------
        public float CashPerScore;              // 0.02
        public float XpPerScore;                // 0.001

        /// <summary>positionMult, indice 0 = 1o lugar. GDD 15.2.</summary>
        public float[] PositionMultipliers;     // 1.00 .82 .68 .55 .45 .38

        public float UpgradeCostBase;           // 120
        public float UpgradeCostTierExponent;   // 1.6
        public float UpgradeCostLevelBase;      // 1.16
        public float RepairCostPerDamage;       // 6
        public float RepairCostTierFactor;      // 0.3
        public float RepairCostReliabilityDivisor; // 300
        public float GarageSlotCostBase;        // 5000
        public float GarageSlotCostGrowth;      // 2.4
        public float SalvageScrapBase;          // 4
        public float SalvageRarityGrowth;       // 2.2
        public float SalvageItemLevelFactor;    // 0.35
        public int ReputationPerWin;            // 3
        public int ReputationPerPodium;         // 1

        // --- loot (10) ----------------------------------------------------------
        public float DropChanceBase;            // 0.55
        public float DropChancePerRisk;         // 0.004
        public float DropChanceScoreDivisor;    // 400000 - o score empurra o drop, com retorno decrescente
        public float DropChanceMax;             // 0.95
        public int AffixCountCommon;            // 1
        public int AffixCountUncommon;          // 2
        public int AffixCountRare;              // 3
        public int AffixCountEpic;              // 4
        public int AffixCountLegendary;         // 4
        public float RarePassiveChance;         // 0.40
        public float NegativeAffixChance;       // 0.22
        public float NegativeAffixValueBonus;   // 0.35 - o trade-off paga em valor absoluto
        public float RarityValueScale;          // 0.18 - por degrau de raridade

        // --- interacao presente (3.6) ------------------------------------------
        /// <summary>Duracao da janela de Entrada Perfeita, em segundos. GDD 3.6.1.</summary>
        public float PerfectEntryWindowSeconds; // 0.45

        /// <summary>
        /// Fracao das curvas Good que abrem janela. E o parafuso de ajuste do uplift
        /// (GDD 6.2) - mexer nele e a acao corretiva quando o teste de 3.6 falha.
        /// </summary>
        public float PromotableCurveFraction;   // 1.00

        public float PresenceUpliftMin;         // 0.20
        public float PresenceUpliftMax;         // 0.30

        public float CollectibleDroneChance;    // 0.25 - ~1 a cada 4 corridas
        public float CollectibleHotLapChance;   // 0.167 - ~1 a cada 6
        public float CollectibleRepChance;      // 0.10 - ~1 a cada 10
        public float CollectibleDroneCashFactor;// 0.35 - do cash base da corrida
        public float CollectibleHotLapDropBonus;// 0.25
        public int CollectibleRepFlat;          // 2

        // --- offline (17) -------------------------------------------------------
        public float OfflineCapHoursBase;       // 8
        public float OfflineCapHoursPerUpgrade; // 2
        public float OfflineCapHoursMax;        // 24
        public int OfflineSampleK;              // 300
        public float InterRaceDelaySeconds;     // 4
        public float OfflineParityTolerance;    // 0.08 - o teste que protege o pilar P3

        // --- tempo de jogo (11.4) -----------------------------------------------
        /// <summary>1 dia de jogo = 2 horas reais. GDD 11.4.</summary>
        public float RealSecondsPerGameDay;     // 7200

        // --- camera (19.2) -------------------------------------------------------
        /// <summary>Altura da camera relativa a linha de base do Horizon Chase. GDD 19.2.</summary>
        public float CameraHeightMultiplier;    // 1.35
        public float CameraBaseHeight;          // 3.2
        public float CameraBaseDistance;        // 8.0
        public float CameraPitchDegrees;        // 14
        public float CameraSpeedPullback;       // 0.02
        public float CameraDriftLateralOffset;  // 0.05
        public float CameraFovBase;             // 60
        public float CameraFovPerSpeed;         // 0.06
        public float CameraFovPerAngle;         // 0.10
        public float CameraDamping;             // 6.0

        // --- desempenho (20.4) ----------------------------------------------------
        public float SimRaceBudgetMs;           // 0.3
        public int AutoEquipSampleRaces;        // 200

        // --- faixas de risco (12.1) -----------------------------------------------
        // O indice de risco e exibido SEMPRE, em quatro faixas. Sao os limiares dessas
        // faixas - o jogador le a palavra, nunca o numero cru.
        public float RiskBandMedium;            // 25
        public float RiskBandHigh;              // 50
        public float RiskBandExtreme;           // 75

        // --- acesso indexado seguro ------------------------------------------------

        public RiskBand Band(float riskIndex)
        {
            if (riskIndex >= RiskBandExtreme) return RiskBand.Extreme;
            if (riskIndex >= RiskBandHigh) return RiskBand.High;
            if (riskIndex >= RiskBandMedium) return RiskBand.Medium;
            return RiskBand.Low;
        }


        public float Tire(TireProfile p) { return Pick(TireGrip, (int)p, 1f); }
        public float WeatherGripOf(Weather w) { return Pick(WeatherGrip, (int)w, 1f); }
        public float SurfaceGripOf(SurfaceType s) { return Pick(SurfaceGrip, (int)s, 1f); }
        public float StyleBias(DriftStyle s) { return Pick(StyleInitiationBias, (int)s, 0f); }
        public float StyleRisk(DriftStyle s) { return Pick(StyleRiskFactor, (int)s, 0f); }
        public float StyleRiskIndex(DriftStyle s) { return Pick(StyleRiskBias, (int)s, 0f); }
        public float Quality(DriftQuality q) { return Pick(QualityMult, (int)q, 1f); }
        public float QualityAngle(DriftQuality q) { return Pick(QualityAngleFactor, (int)q, 1f); }
        public float TimeGrip(TimeOfDay t) { return Pick(TimeOfDayGrip, (int)t, 1f); }
        public float TimeReward(TimeOfDay t) { return Pick(TimeOfDayReward, (int)t, 1f); }
        public float TimeRisk(TimeOfDay t) { return Pick(TimeOfDayRisk, (int)t, 0f); }
        public float WeatherRewardOf(Weather w) { return Pick(WeatherReward, (int)w, 1f); }
        public float WeatherRiskOf(Weather w) { return Pick(WeatherRisk, (int)w, 0f); }
        public float TrafficRewardOf(TrafficDensity t) { return Pick(TrafficReward, (int)t, 1f); }
        public float TrafficRiskOf(TrafficDensity t) { return Pick(TrafficRisk, (int)t, 0f); }

        /// <summary>positionMult. Posicao e 1-based; fora do grid usa o pior valor.</summary>
        public float PositionMultiplier(int position)
        {
            if (PositionMultipliers == null || PositionMultipliers.Length == 0) return 1f;
            int i = position - 1;
            if (i < 0) i = 0;
            if (i >= PositionMultipliers.Length) i = PositionMultipliers.Length - 1;
            return PositionMultipliers[i];
        }

        public int AffixCount(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return AffixCountCommon;
                case Rarity.Uncommon: return AffixCountUncommon;
                case Rarity.Rare: return AffixCountRare;
                case Rarity.Epic: return AffixCountEpic;
                default: return AffixCountLegendary;
            }
        }

        private static float Pick(float[] table, int index, float fallback)
        {
            if (table == null || index < 0 || index >= table.Length) return fallback;
            return table[index];
        }
    }
}
