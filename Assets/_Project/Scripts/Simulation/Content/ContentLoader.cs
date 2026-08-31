// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Content
//  GDD 0.2  secao 20.2
// -----------------------------------------------------------------------------

using System;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Serialization;

namespace FramedDrift.Simulation.Content
{
    /// <summary>
    /// Le os arquivos de Content/ e produz um <see cref="ContentDatabase"/>.
    ///
    /// Recebe um delegado de leitura em vez de tocar em disco: em Unity ele vem de
    /// <c>Resources.Load</c>, nos testes vem do sistema de arquivos, e o assembly
    /// continua puro (GDD 20.3).
    /// </summary>
    public static class ContentLoader
    {
        public const string BalanceFile = "balance";
        public const string CarsFile = "cars";
        public const string PartsFile = "parts";
        public const string AffixesFile = "affixes";
        public const string PassivesFile = "passives";
        public const string SetsFile = "sets";
        public const string ModulesFile = "modules";
        public const string TracksFile = "tracks";
        public const string RegionsFile = "regions";
        public const string TiersFile = "tiers";
        public const string RivalsFile = "rivals";

        public static readonly string[] AllFiles =
        {
            BalanceFile, CarsFile, PartsFile, AffixesFile, PassivesFile, SetsFile,
            ModulesFile, TracksFile, RegionsFile, TiersFile, RivalsFile,
        };

        /// <param name="read">Nome logico do arquivo (sem extensao) para o conteudo bruto.</param>
        public static ContentDatabase Load(Func<string, string> read)
        {
            if (read == null) throw new ArgumentNullException("read");

            var db = new ContentDatabase();
            db.Balance = ParseBalance(Doc(read, BalanceFile));

            ParseTiers(db, Doc(read, TiersFile));
            ParseAffixes(db, Doc(read, AffixesFile));
            ParsePassives(db, Doc(read, PassivesFile));
            ParseSets(db, Doc(read, SetsFile));
            ParseParts(db, Doc(read, PartsFile));
            ParseCars(db, Doc(read, CarsFile));
            ParseModules(db, Doc(read, ModulesFile));
            ParseTracks(db, Doc(read, TracksFile));
            ParseRegions(db, Doc(read, RegionsFile));
            ParseRivals(db, Doc(read, RivalsFile));

            ContentValidator.Validate(db);
            return db;
        }

        private static JsonValue Doc(Func<string, string> read, string name)
        {
            string text = read(name);
            if (string.IsNullOrEmpty(text))
                throw new ContentException("Arquivo de conteudo ausente ou vazio: " + name + ".json");
            try
            {
                return JsonValue.Parse(text);
            }
            catch (JsonException e)
            {
                throw new ContentException(name + ".json: " + e.Message);
            }
        }

        // --- balance -------------------------------------------------------------

        private static BalanceSettings ParseBalance(JsonValue j)
        {
            var b = new BalanceSettings();

            b.GripBase = j["gripBase"].AsFloat();
            b.GripSpan = j["gripSpan"].AsFloat();
            b.WearGripPenalty = j["wearGripPenalty"].AsFloat();
            b.TireGrip = Table<TireProfile>(j["tireGrip"], 1f);
            b.WetTireOnWetGrip = j["wetTireOnWetGrip"].AsFloat();
            b.WetTireDryOverheatPenalty = j["wetTireDryOverheatPenalty"].AsFloat();
            b.WetTireOverheatAfterFraction = j["wetTireOverheatAfterFraction"].AsFloat();
            b.WeatherGrip = Table<Weather>(j["weatherGrip"], 1f);
            b.SurfaceGrip = Table<SurfaceType>(j["surfaceGrip"], 1f);

            b.ArcadeSpeedFactor = j["arcadeSpeedFactor"].AsFloat();
            b.Gravity = j["gravity"].AsFloat();
            b.MsToKmh = j["msToKmh"].AsFloat();
            b.StraightAccelPerIndex = j["straightAccelPerIndex"].AsFloat();
            b.BrakingTimeSavedPerPoint = j["brakingTimeSavedPerPoint"].AsFloat();

            b.DriftThresholdBase = j["driftThresholdBase"].AsFloat();
            b.DriftThresholdCurvature = j["driftThresholdCurvature"].AsFloat();
            b.InitiationPowerWeightGain = j["initiationPowerWeightGain"].AsFloat();
            b.DriftSpeedFloor = j["driftSpeedFloor"].AsFloat();
            b.DriftSpeedAngleSpan = j["driftSpeedAngleSpan"].AsFloat();
            b.TransitionSpeedBonus = j["transitionSpeedBonus"].AsFloat();
            b.StyleInitiationBias = Table<DriftStyle>(j["styleInitiationBias"], 0f);
            b.StyleRiskFactor = Table<DriftStyle>(j["styleRiskFactor"], 0f);
            b.CurvatureReferenceRadius = j["curvatureReferenceRadius"].AsFloat();

            b.SkillSteeringFactor = j["skillSteeringFactor"].AsFloat();
            b.SkillStabilityFactor = j["skillStabilityFactor"].AsFloat();
            b.SkillSigmoidDivisor = j["skillSigmoidDivisor"].AsFloat();
            b.PerfectCeiling = j["perfectCeiling"].AsFloat();
            b.PerfectMin = j["perfectMin"].AsFloat();
            b.PerfectMax = j["perfectMax"].AsFloat();
            b.BadBase = j["badBase"].AsFloat();
            b.BadSlope = j["badSlope"].AsFloat();
            b.BadMin = j["badMin"].AsFloat();
            b.BadMax = j["badMax"].AsFloat();
            b.QualityMult = Table<DriftQuality>(j["qualityMult"], 1f);
            b.QualityAngleFactor = Table<DriftQuality>(j["qualityAngleFactor"], 1f);
            b.BadExitSpeedPenalty = j["badExitSpeedPenalty"].AsFloat();
            b.CondModRain = j["condModRain"].AsFloat();
            b.CondModNight = j["condModNight"].AsFloat();
            b.CondModFog = j["condModFog"].AsFloat();
            b.CondModHeavyTraffic = j["condModHeavyTraffic"].AsFloat();

            b.TierTimeScale = j["tierTimeScale"].AsFloat();
            b.OpponentSigmaFraction = j["opponentSigmaFraction"].AsFloat();
            b.GridSize = j["gridSize"].AsInt();

            b.TireWearPerKm = j["tireWearPerKm"].AsFloat();
            b.TireWearStyleFactor = j["tireWearStyleFactor"].AsFloat();
            b.TireWearDriftFactor = j["tireWearDriftFactor"].AsFloat();
            b.TempRisePerKm = j["tempRisePerKm"].AsFloat();
            b.TempCoolingFactor = j["tempCoolingFactor"].AsFloat();
            b.TempOverheatThreshold = j["tempOverheatThreshold"].AsFloat();
            b.TempOverheatPowerPenalty = j["tempOverheatPowerPenalty"].AsFloat();
            b.TempOverheatFailFactor = j["tempOverheatFailFactor"].AsFloat();
            b.ComboResetStraightMeters = j["comboResetStraightMeters"].AsFloat();

            b.ScoreBasePerMeter = j["scoreBasePerMeter"].AsFloat();
            b.AngleMultBase = j["angleMultBase"].AsFloat();
            b.AngleMultSpan = j["angleMultSpan"].AsFloat();
            b.SpeedMultDivisor = j["speedMultDivisor"].AsFloat();
            b.SpeedMultMin = j["speedMultMin"].AsFloat();
            b.SpeedMultMax = j["speedMultMax"].AsFloat();
            b.ComboMultStep = j["comboMultStep"].AsFloat();
            b.ComboMultCap = j["comboMultCap"].AsInt();
            b.ProximityMultSpan = j["proximityMultSpan"].AsFloat();
            b.TransitionMult = j["transitionMult"].AsFloat();

            b.AccelBase = j["accelBase"].AsFloat();
            b.AccelPerPowerRatio = j["accelPerPowerRatio"].AsFloat();
            b.TopSpeedBase = j["topSpeedBase"].AsFloat();
            b.TopSpeedPerPower = j["topSpeedPerPower"].AsFloat();
            b.TopSpeedPerAero = j["topSpeedPerAero"].AsFloat();
            b.TopSpeedPerKg = j["topSpeedPerKg"].AsFloat();

            b.TimeOfDayGrip = Table<TimeOfDay>(j["timeOfDayGrip"], 1f);
            b.TimeOfDayReward = Table<TimeOfDay>(j["timeOfDayReward"], 1f);
            b.TimeOfDayRisk = Table<TimeOfDay>(j["timeOfDayRisk"], 0f);
            b.WeatherReward = Table<Weather>(j["weatherReward"], 1f);
            b.WeatherRisk = Table<Weather>(j["weatherRisk"], 0f);
            b.TrafficReward = Table<TrafficDensity>(j["trafficReward"], 1f);
            b.TrafficRisk = Table<TrafficDensity>(j["trafficRisk"], 0f);
            b.RewardMultCap = j["rewardMultCap"].AsFloat();

            b.RiskBandMedium = j["riskBandMedium"].AsFloat();
            b.RiskBandHigh = j["riskBandHigh"].AsFloat();
            b.RiskBandExtreme = j["riskBandExtreme"].AsFloat();
            b.RiskTierFactor = j["riskTierFactor"].AsFloat();
            b.RiskTrackFactor = j["riskTrackFactor"].AsFloat();
            b.StyleRiskBias = Table<DriftStyle>(j["styleRiskBias"], 0f);
            b.RiskDefenseDivisor = j["riskDefenseDivisor"].AsFloat();
            b.FailBasePerSegment = j["failBasePerSegment"].AsFloat();
            b.FailReliabilityDivisor = j["failReliabilityDivisor"].AsFloat();
            b.RiskFailExponent = j["riskFailExponent"].AsFloat();
            b.FailDamageMin = j["failDamageMin"].AsFloat();
            b.FailDamageMax = j["failDamageMax"].AsFloat();
            b.FailTimePenaltySeconds = j["failTimePenaltySeconds"].AsFloat();
            b.FailSpeedPenalty = j["failSpeedPenalty"].AsFloat();
            b.CollisionDamageMin = j["collisionDamageMin"].AsFloat();
            b.CollisionDamageMax = j["collisionDamageMax"].AsFloat();
            b.CollisionSpeedPenalty = j["collisionSpeedPenalty"].AsFloat();
            b.CollisionTimePenaltySeconds = j["collisionTimePenaltySeconds"].AsFloat();
            b.DamageStatPenaltyAt100 = j["damageStatPenaltyAt100"].AsFloat();
            b.ProximityCollisionBase = j["proximityCollisionBase"].AsFloat();
            b.WearOverdriveThreshold = j["wearOverdriveThreshold"].AsFloat();
            b.StartSpeedKmh = j["startSpeedKmh"].AsFloat();
            b.BrakingTimeSaveMax = j["brakingTimeSaveMax"].AsFloat();

            b.TuningLockAngle = j["tuningLockAngle"].AsFloat();
            b.TuningLockGrip = j["tuningLockGrip"].AsFloat();
            b.TuningAccelInitiation = j["tuningAccelInitiation"].AsFloat();
            b.TuningAccelStability = j["tuningAccelStability"].AsFloat();
            b.TuningDecelTransition = j["tuningDecelTransition"].AsFloat();
            b.TuningDecelBadRisk = j["tuningDecelBadRisk"].AsFloat();

            b.TraitLightChassisWeightFactor = j["traitLightChassisWeightFactor"].AsFloat();
            b.TraitLightChassisStability = j["traitLightChassisStability"].AsFloat();
            b.TraitRawTorqueInitiation = j["traitRawTorqueInitiation"].AsFloat();
            b.TraitRawTorqueGrip = j["traitRawTorqueGrip"].AsFloat();
            b.TraitAllWheelWeatherRelief = j["traitAllWheelWeatherRelief"].AsFloat();
            b.TraitAllWheelAngle = j["traitAllWheelAngle"].AsFloat();
            b.TraitMidEngineTransition = j["traitMidEngineTransition"].AsFloat();
            b.TraitMidEngineBadFactor = j["traitMidEngineBadFactor"].AsFloat();
            b.TraitFactoryCooling = j["traitFactoryCooling"].AsFloat();
            b.TraitStreetCarUrbanCash = j["traitStreetCarUrbanCash"].AsFloat();
            b.TraitStreetCarMountainCash = j["traitStreetCarMountainCash"].AsFloat();
            b.BothSidesCollisionFactor = j["bothSidesCollisionFactor"].AsFloat();
            b.DriftHeatFactor = j["driftHeatFactor"].AsFloat();

            b.StagesPerTier = j["stagesPerTier"].AsInt();
            b.StageDifficultyStep = j["stageDifficultyStep"].AsFloat();
            b.StageRewardStep = j["stageRewardStep"].AsFloat();
            b.SoftCapStage = j["softCapStage"].AsInt();
            b.SoftCapStrength = j["softCapStrength"].AsFloat();

            b.CashPerScore = j["cashPerScore"].AsFloat();
            b.XpPerScore = j["xpPerScore"].AsFloat();
            b.PositionMultipliers = j["positionMultipliers"].AsFloatArray();
            b.UpgradeCostBase = j["upgradeCostBase"].AsFloat();
            b.UpgradeCostTierExponent = j["upgradeCostTierExponent"].AsFloat();
            b.UpgradeCostLevelBase = j["upgradeCostLevelBase"].AsFloat();
            b.RepairCostPerDamage = j["repairCostPerDamage"].AsFloat();
            b.RepairCostTierFactor = j["repairCostTierFactor"].AsFloat();
            b.RepairCostReliabilityDivisor = j["repairCostReliabilityDivisor"].AsFloat();
            b.GarageSlotCostBase = j["garageSlotCostBase"].AsFloat();
            b.GarageSlotCostGrowth = j["garageSlotCostGrowth"].AsFloat();
            b.SalvageScrapBase = j["salvageScrapBase"].AsFloat();
            b.SalvageRarityGrowth = j["salvageRarityGrowth"].AsFloat();
            b.SalvageItemLevelFactor = j["salvageItemLevelFactor"].AsFloat();
            b.BlueprintFragmentChance = j["blueprintFragmentChance"].AsFloat();
            b.BlueprintFragmentsPerBlueprint = j["blueprintFragmentsPerBlueprint"].AsInt();
            b.ReputationPerWin = j["reputationPerWin"].AsInt();
            b.ReputationPerPodium = j["reputationPerPodium"].AsInt();

            b.DropChanceBase = j["dropChanceBase"].AsFloat();
            b.DropChancePerRisk = j["dropChancePerRisk"].AsFloat();
            b.DropChanceScoreDivisor = j["dropChanceScoreDivisor"].AsFloat();
            b.DropChanceMax = j["dropChanceMax"].AsFloat();
            b.AffixCountCommon = j["affixCountCommon"].AsInt();
            b.AffixCountUncommon = j["affixCountUncommon"].AsInt();
            b.AffixCountRare = j["affixCountRare"].AsInt();
            b.AffixCountEpic = j["affixCountEpic"].AsInt();
            b.AffixCountLegendary = j["affixCountLegendary"].AsInt();
            b.RarePassiveChance = j["rarePassiveChance"].AsFloat();
            b.NegativeAffixChance = j["negativeAffixChance"].AsFloat();
            b.NegativeAffixValueBonus = j["negativeAffixValueBonus"].AsFloat();
            b.RarityValueScale = j["rarityValueScale"].AsFloat();

            b.PerfectEntryWindowSeconds = j["perfectEntryWindowSeconds"].AsFloat();
            b.PromotableCurveFraction = j["promotableCurveFraction"].AsFloat();
            b.PresenceUpliftMin = j["presenceUpliftMin"].AsFloat();
            b.PresenceUpliftMax = j["presenceUpliftMax"].AsFloat();
            b.CollectibleDroneChance = j["collectibleDroneChance"].AsFloat();
            b.CollectibleHotLapChance = j["collectibleHotLapChance"].AsFloat();
            b.CollectibleRepChance = j["collectibleRepChance"].AsFloat();
            b.CollectibleDroneCashFactor = j["collectibleDroneCashFactor"].AsFloat();
            b.CollectibleHotLapDropBonus = j["collectibleHotLapDropBonus"].AsFloat();
            b.CollectibleRepFlat = j["collectibleRepFlat"].AsInt();

            b.OfflineCapHoursBase = j["offlineCapHoursBase"].AsFloat();
            b.OfflineCapHoursPerUpgrade = j["offlineCapHoursPerUpgrade"].AsFloat();
            b.OfflineCapHoursMax = j["offlineCapHoursMax"].AsFloat();
            b.OfflineSampleK = j["offlineSampleK"].AsInt();
            b.InterRaceDelaySeconds = j["interRaceDelaySeconds"].AsFloat();
            b.OfflineParityTolerance = j["offlineParityTolerance"].AsFloat();

            b.RealSecondsPerGameDay = j["realSecondsPerGameDay"].AsFloat();

            b.CameraHeightMultiplier = j["cameraHeightMultiplier"].AsFloat();
            b.CameraBaseHeight = j["cameraBaseHeight"].AsFloat();
            b.CameraBaseDistance = j["cameraBaseDistance"].AsFloat();
            b.CameraPitchDegrees = j["cameraPitchDegrees"].AsFloat();
            b.CameraSpeedPullback = j["cameraSpeedPullback"].AsFloat();
            b.CameraDriftLateralOffset = j["cameraDriftLateralOffset"].AsFloat();
            b.CameraFovBase = j["cameraFovBase"].AsFloat();
            b.CameraFovPerSpeed = j["cameraFovPerSpeed"].AsFloat();
            b.CameraFovPerAngle = j["cameraFovPerAngle"].AsFloat();
            b.CameraDamping = j["cameraDamping"].AsFloat();

            b.SimRaceBudgetMs = j["simRaceBudgetMs"].AsFloat();
            b.AutoEquipSampleRaces = j["autoEquipSampleRaces"].AsInt();

            return b;
        }

        /// <summary>
        /// Tabela indexada por enum, escrita no JSON como objeto com os NOMES do enum.
        /// Um array posicional economizaria bytes e custaria a primeira reordenacao.
        /// </summary>
        private static float[] Table<T>(JsonValue j, float fallback) where T : struct
        {
            string[] names = Enum.GetNames(typeof(T));
            float[] result = new float[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                JsonValue v = j[names[i]];
                result[i] = v.Exists ? v.AsFloat(fallback) : fallback;
            }
            return result;
        }

        // --- stats ----------------------------------------------------------------

        /// <summary>
        /// Bloco de stats escrito como objeto parcial: so as stats presentes sao lidas.
        /// Um afixo que mexe em Power nao precisa listar as outras doze.
        /// </summary>
        public static ResolvedStats Stats(JsonValue j)
        {
            var s = new ResolvedStats();
            for (int i = 0; i < StatOps.Count; i++)
            {
                StatId id = (StatId)i;
                JsonValue v = j[id.ToString()];
                if (v.Exists) StatOps.Add(ref s, id, v.AsFloat());
            }
            return s;
        }

        // --- tiers ------------------------------------------------------------------

        private static void ParseTiers(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["tiers"].Items)
            {
                var t = new TierDef
                {
                    Rank = j["rank"].AsEnum(TierRank.D),
                    DifficultyScale = j["difficultyScale"].AsFloat(1f),
                    RewardScale = j["rewardScale"].AsFloat(1f),
                    RarityWeights = j["rarityWeights"].AsFloatArray(),
                    ItemLevelCap = j["itemLevelCap"].AsInt(),
                    ReputationRequired = j["reputationRequired"].AsInt(),
                };
                db.Tiers[(int)t.Rank] = t;
            }
        }

        // --- afixos e passivas --------------------------------------------------------

        private static void ParseAffixes(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["affixes"].Items)
            {
                var a = new AffixDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Stat = j["stat"].AsEnum(StatId.Power),
                    MinValue = j["min"].AsFloat(),
                    MaxValue = j["max"].AsFloat(),
                    PerItemLevel = j["perItemLevel"].AsFloat(),
                    Sign = j["sign"].AsInt(1),
                    IsDrawback = j["drawback"].AsBool(),
                    Weight = j["weight"].AsFloat(1f),
                    Slots = Slots(j["slots"]),
                };
                db.Affixes[a.Id] = a;
                db.AffixList.Add(a);
            }
        }

        private static void ParsePassives(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["passives"].Items)
            {
                var p = new PassiveDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Kind = j["kind"].AsEnum(PassiveKind.None),
                    Magnitude = j["magnitude"].AsFloat(),
                    Magnitude2 = j["magnitude2"].AsFloat(),
                    Stat = j["stat"].AsEnum(StatId.Power),
                    MinRarity = j["minRarity"].AsEnum(Rarity.Rare),
                    Weight = j["weight"].AsFloat(1f),
                    Slots = Slots(j["slots"]),
                };
                db.Passives[p.Id] = p;
                db.PassiveList.Add(p);
            }
        }

        private static void ParseSets(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["sets"].Items)
            {
                JsonValue bonuses = j["bonuses"];
                var effects = new PassiveEffect[bonuses.Count];
                var texts = new string[bonuses.Count];
                for (int i = 0; i < bonuses.Count; i++)
                {
                    effects[i] = new PassiveEffect
                    {
                        Kind = bonuses[i]["kind"].AsEnum(PassiveKind.None),
                        Magnitude = bonuses[i]["magnitude"].AsFloat(),
                        Magnitude2 = bonuses[i]["magnitude2"].AsFloat(),
                        Stat = bonuses[i]["stat"].AsEnum(StatId.Power),
                    };
                    texts[i] = bonuses[i]["text"].AsString();
                }

                var s = new SetDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Bonuses = effects,
                    BonusText = texts,
                };
                db.Sets[s.Id] = s;
            }
        }

        // --- pecas e carros -------------------------------------------------------------

        private static void ParseParts(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["parts"].Items)
            {
                var p = new PartDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Slot = j["slot"].AsEnum(PartSlot.Engine),
                    Tier = j["tier"].AsEnum(TierRank.D),
                    BaseStats = Stats(j["stats"]),
                    SellValue = j["sellValue"].AsLong(),
                    BuyCost = j["buyCost"].AsLong(),
                    DropWeight = j["dropWeight"].AsFloat(1f),
                    SetId = j["set"].AsString(null),
                };

                if (j["tireProfile"].Exists)
                {
                    p.IsTire = true;
                    p.TireProfile = j["tireProfile"].AsEnum(TireProfile.Street);
                }

                if (j["differentialType"].Exists)
                {
                    p.IsDifferential = true;
                    p.DifferentialType = j["differentialType"].AsEnum(DifferentialType.StreetLsd);
                }

                db.Parts[p.Id] = p;
                db.PartList.Add(p);
            }
        }

        private static void ParseCars(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["cars"].Items)
            {
                JsonValue tr = j["tuningRange"];
                var range = TuningRange.Default;
                if (tr.Exists)
                {
                    range.LockMin = tr["lockMin"].AsFloat(0f);
                    range.LockMax = tr["lockMax"].AsFloat(1f);
                    range.AccelMin = tr["accelMin"].AsFloat(0f);
                    range.AccelMax = tr["accelMax"].AsFloat(1f);
                    range.DecelMin = tr["decelMin"].AsFloat(0f);
                    range.DecelMax = tr["decelMax"].AsFloat(1f);
                }

                JsonValue un = j["unlock"];
                var unlock = new UnlockRule
                {
                    ReputationRequired = un["reputation"].AsInt(),
                    BlueprintId = un["blueprint"].AsString(null),
                    RivalDefeatedId = un["rivalDefeated"].AsString(null),
                };

                var c = new CarDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Layout = j["layout"].AsEnum(DriveLayout.FR),
                    Tier = j["tier"].AsEnum(TierRank.D),
                    BaseStats = Stats(j["stats"]),
                    SlotProfile = SlotProfile(j["slots"]),
                    Trait = j["trait"].AsEnum(TraitId.None),
                    TuningRange = range,
                    Unlock = unlock,
                    BuyCost = j["buyCost"].AsLong(),
                    PlaceholderColor = j["placeholderColor"].AsString("CCCCCC"),
                };
                db.Cars[c.Id] = c;
                db.CarList.Add(c);
            }
        }

        // --- pistas e regioes -------------------------------------------------------------

        private static void ParseModules(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["modules"].Items)
            {
                JsonValue s = j["segment"];
                var seg = new Segment
                {
                    Type = s["type"].AsEnum(SegmentType.Straight),
                    LengthM = s["length"].AsFloat(),
                    Radius = s.Has("radius") ? s["radius"].AsFloat() : float.PositiveInfinity,
                    Direction = s["direction"].AsEnum(TurnDirection.None),
                    Surface = s["surface"].AsEnum(SurfaceType.Asphalt),
                    Walls = s["walls"].AsEnum(WallConfig.None),
                    Difficulty = s["difficulty"].AsFloat(),
                };

                var m = new ModuleDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    Segment = seg,
                    AllowedNext = j["allowedNext"].AsStringArray(),
                    Weight = j["weight"].AsFloat(1f),
                };
                db.Modules[m.Id] = m;
            }
        }

        private static void ParseTracks(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["tracks"].Items)
            {
                var t = new TrackDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    RegionId = j["region"].AsString(),
                    Tier = j["tier"].AsEnum(TierRank.D),
                    ModuleIds = j["modules"].AsStringArray(),
                    BaseTimeSeconds = j["baseTimeSeconds"].AsFloat(),
                    ScoreMultiplier = j["scoreMultiplier"].AsFloat(1f),
                    BaseCash = j["baseCash"].AsLong(),
                    BaseXp = j["baseXp"].AsInt(),
                    ReputationRequired = j["reputationRequired"].AsInt(),
                };
                db.Tracks[t.Id] = t;
                db.TrackList.Add(t);
            }
        }

        private static void ParseRegions(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["regions"].Items)
            {
                JsonValue w = j["weather"];
                var weights = new WeatherWeight[w.Count];
                for (int i = 0; i < w.Count; i++)
                {
                    weights[i] = new WeatherWeight
                    {
                        Weather = w[i]["type"].AsEnum(Weather.Clear),
                        Weight = w[i]["weight"].AsFloat(1f),
                    };
                }

                var r = new RegionDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    ModuleIds = j["modules"].AsStringArray(),
                    Weather = weights,
                    PartPool = j["partPool"].AsStringArray(),
                    CashMultiplier = j["cashMultiplier"].AsFloat(1f),
                    ScoreMultiplier = j["scoreMultiplier"].AsFloat(1f),
                    IsUrban = j["urban"].AsBool(),
                    IsMountain = j["mountain"].AsBool(),
                    Neighbors = j["neighbors"].AsStringArray(),
                    ReputationRequired = j["reputationRequired"].AsInt(),
                };
                db.Regions[r.Id] = r;
                db.RegionList.Add(r);
            }
        }

        private static void ParseRivals(ContentDatabase db, JsonValue doc)
        {
            foreach (JsonValue j in doc["rivals"].Items)
            {
                var r = new RivalDef
                {
                    Id = j["id"].AsString(),
                    DisplayName = j["name"].AsString(),
                    CarId = j["car"].AsString(),
                    Style = j["style"].AsEnum(DriftStyle.Balanced),
                    StatBonus = Stats(j["statBonus"]),
                    SignaturePartId = j["signaturePart"].AsString(null),
                    LessonText = j["lesson"].AsString(),
                    TierIndex = j["tierIndex"].AsInt(),
                };
                db.Rivals[r.Id] = r;
                db.RivalList.Add(r);
            }
        }

        // --- utilitarios ---------------------------------------------------------------

        private static PartSlot[] Slots(JsonValue j)
        {
            if (!j.Exists || j.Count == 0) return new PartSlot[0];
            var result = new PartSlot[j.Count];
            for (int i = 0; i < j.Count; i++) result[i] = j[i].AsEnum(PartSlot.Engine);
            return result;
        }

        private static PartSlot[] SlotProfile(JsonValue j)
        {
            PartSlot[] declared = Slots(j);
            if (declared.Length > 0) return declared;

            // Sem declaracao explicita o carro aceita os 8 slots da secao 9.1.
            var all = (PartSlot[])Enum.GetValues(typeof(PartSlot));
            return all;
        }
    }
}
