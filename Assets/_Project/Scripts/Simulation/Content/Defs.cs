// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Content
//  GDD 0.2  secoes 8, 9, 10, 11, 13, 14, 20.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Content
{
    /// <summary>
    /// A peculiaridade unica de cada carro (GDD 8.3). Uma trait por carro, sempre.
    ///
    /// Enum e nao script: a trait precisa ser legivel pelo simulador puro, e o conjunto
    /// e pequeno e artesanal por decisao (D-04). Cada valor tem uma frase na tabela 8.3.
    /// </summary>
    public enum TraitId
    {
        None,

        /// <summary>-8% Weight efetivo, -10 Stability.</summary>
        LightChassis,

        /// <summary>Initiation +20, Grip -8.</summary>
        RawTorque,

        /// <summary>Ignora 50% da penalidade de clima, Angle maximo -15.</summary>
        AllWheelGrip,

        /// <summary>Transition +25, chance de Bad +30%.</summary>
        MidEngine,

        /// <summary>Cooling +30; Endurance e Endless ganham +15% de recompensa.</summary>
        FactoryCooling,

        /// <summary>+20% cash em pistas urbanas, -20% em montanha.</summary>
        StreetCar,

        /// <summary>Perfect concede combo +2 em vez de +1.</summary>
        RacingHeritage,
    }

    /// <summary>
    /// Efeito de uma passiva de peca (GDD 10.4) ou de um bonus de set (GDD 10.5).
    ///
    /// A lista e fechada de proposito: cada valor precisa ser lido pelo simulador em
    /// C# puro, e uma linguagem de script aqui viraria uma superficie de nao-determinismo
    /// exatamente no lugar onde D-01 nao pode ceder.
    /// </summary>
    public enum PassiveKind
    {
        None,

        /// <summary>+X% de Drift Score acima de 120 km/h (o limiar e Magnitude2).</summary>
        ScoreAboveSpeed,

        /// <summary>+X% de Drift Score em pistas noturnas.</summary>
        ScoreAtNight,

        /// <summary>+X% de Drift Score na chuva.</summary>
        ScoreInRain,

        /// <summary>Perfect concede +X de combo em vez de +1.</summary>
        ComboOnPerfect,

        /// <summary>+X% no teto de combo efetivo (duracao de combo, GDD 10.3).</summary>
        ComboCapBonus,

        /// <summary>Chuva nao reduz o grip nos primeiros X% da corrida.</summary>
        RainGraceFraction,

        /// <summary>Cada colisao evitada por proximidade concede +X% de cash.</summary>
        CashPerProximity,

        /// <summary>Abaixo de X% de Reliability efetiva, +Magnitude2% Power.</summary>
        PowerWhenFragile,

        /// <summary>+X pontos percentuais na chance de Perfect.</summary>
        PerfectChanceBonus,

        /// <summary>+X% de chance de drop na corrida.</summary>
        DropChanceBonus,

        /// <summary>+X% de cash.</summary>
        CashBonus,

        /// <summary>+X% de TopSpeed.</summary>
        TopSpeedBonus,

        /// <summary>+X% de aceleracao efetiva.</summary>
        AccelerationBonus,

        /// <summary>+X% de Drift Score, sem condicao.</summary>
        ScoreBonus,

        /// <summary>+X pontos planos numa stat primaria. Usa o campo Stat.</summary>
        FlatStat,
    }

    /// <summary>Um efeito condicional ja resolvido, pronto para o simulador consultar.</summary>
    public struct PassiveEffect
    {
        public PassiveKind Kind;
        public float Magnitude;

        /// <summary>Segundo parametro, quando a passiva precisa de um limiar.</summary>
        public float Magnitude2;

        /// <summary>Usado apenas por <see cref="PassiveKind.FlatStat"/>.</summary>
        public StatId Stat;
    }

    /// <summary>Definicao artesanal de uma passiva sorteavel. GDD 10.4.</summary>
    public sealed class PassiveDef
    {
        public string Id;
        public string DisplayName;
        public PassiveKind Kind;
        public float Magnitude;
        public float Magnitude2;
        public StatId Stat;

        /// <summary>Raridade minima que pode rolar esta passiva. Rare+ por regra (GDD 10.2).</summary>
        public Rarity MinRarity;

        public float Weight;

        /// <summary>Vazio significa "qualquer slot".</summary>
        public PartSlot[] Slots;

        public PassiveEffect ToEffect()
        {
            return new PassiveEffect { Kind = Kind, Magnitude = Magnitude, Magnitude2 = Magnitude2, Stat = Stat };
        }
    }

    /// <summary>
    /// Um afixo sorteavel. GDD 10.3.
    ///
    /// Afixos negativos existem e sao desejaveis: uma peca com trade-off rola valores
    /// absolutos maiores, o que cria peca de alto risco em vez de peca ruim.
    /// </summary>
    public sealed class AffixDef
    {
        public string Id;
        public string DisplayName;
        public StatId Stat;

        /// <summary>Faixa de rolagem no item level 1.</summary>
        public float MinValue;
        public float MaxValue;

        /// <summary>Quanto a faixa cresce por item level.</summary>
        public float PerItemLevel;

        /// <summary>+1 ou -1. O sinal do delta aplicado a stat.</summary>
        public int Sign;

        /// <summary>
        /// Este afixo PREJUDICA a build?
        ///
        /// Nao e o mesmo que sinal negativo: "-25 kg" tem sinal negativo e e um ganho.
        /// Um afixo que prejudica paga: os outros afixos da mesma peca rolam com
        /// negativeAffixValueBonus a mais, o que e o que cria a peca de alto risco em
        /// vez da peca ruim (GDD 10.3).
        /// </summary>
        public bool IsDrawback;

        public float Weight;

        /// <summary>Vazio significa "qualquer slot".</summary>
        public PartSlot[] Slots;
    }

    /// <summary>Base artesanal sobre a qual os afixos sao rolados. GDD 10.1 / D-04.</summary>
    public sealed class PartDef
    {
        public string Id;
        public string DisplayName;
        public PartSlot Slot;
        public TierRank Tier;

        /// <summary>Contribuicao base, antes dos afixos.</summary>
        public ResolvedStats BaseStats;

        public long SellValue;
        public long BuyCost;

        /// <summary>Peso relativo dentro do pool de drop do slot.</summary>
        public float DropWeight;

        /// <summary>Preenchido apenas no slot Tires. Alimenta k_tire (GDD 5.2 / 9.3).</summary>
        public TireProfile TireProfile;
        public bool IsTire;

        /// <summary>Preenchido apenas no slot Differential. GDD 9.2.</summary>
        public DifferentialType DifferentialType;
        public bool IsDifferential;

        /// <summary>Vazio quando a peca nao pertence a nenhum set. GDD 10.5.</summary>
        public string SetId;
    }

    /// <summary>Conjunto com bonus escalonados em 2/3/4 pecas. GDD 10.5.</summary>
    public sealed class SetDef
    {
        public string Id;
        public string DisplayName;

        /// <summary>Indexado por quantidade de pecas: [0] = 2 pcs, [1] = 3 pcs, [2] = 4 pcs.</summary>
        public PassiveEffect[] Bonuses;
        public string[] BonusText;
    }

    /// <summary>Limites do ajuste fino do diferencial, por carro. GDD 9.2.</summary>
    public struct TuningRange
    {
        public float LockMin, LockMax;
        public float AccelMin, AccelMax;
        public float DecelMin, DecelMax;

        public static TuningRange Default
        {
            get
            {
                return new TuningRange
                {
                    LockMin = 0f, LockMax = 1f,
                    AccelMin = 0f, AccelMax = 1f,
                    DecelMin = 0f, DecelMax = 1f,
                };
            }
        }
    }

    /// <summary>Como o carro e destravado. GDD 8.2.</summary>
    public sealed class UnlockRule
    {
        public int ReputationRequired;
        public string BlueprintId;
        public string RivalDefeatedId;

        public bool IsFree
        {
            get
            {
                return ReputationRequired <= 0
                    && string.IsNullOrEmpty(BlueprintId)
                    && string.IsNullOrEmpty(RivalDefeatedId);
            }
        }
    }

    /// <summary>Carro artesanal. Poucos e desenhados a mao, por D-04. GDD 8.2.</summary>
    public sealed class CarDef
    {
        public string Id;
        public string DisplayName;
        public DriveLayout Layout;
        public TierRank Tier;
        public ResolvedStats BaseStats;
        public PartSlot[] SlotProfile;
        public TraitId Trait;
        public TuningRange TuningRange;
        public UnlockRule Unlock;
        public long BuyCost;

        /// <summary>Cor do cubo de placeholder, em hexadecimal RRGGBB. O modelo entra depois.</summary>
        public string PlaceholderColor;
    }

    /// <summary>
    /// Trecho autoral de pista. D-05: o gerador COMBINA modulos, nunca gera geometria
    /// por ruido. GDD 11.2.
    /// </summary>
    public sealed class ModuleDef
    {
        public string Id;
        public string DisplayName;
        public Segment Segment;

        /// <summary>
        /// Ids que podem vir DEPOIS deste. Vazio significa "qualquer um".
        /// E aqui que moram as regras de adjacencia da GDD 11.3 - autorais, nao emergentes.
        /// </summary>
        public string[] AllowedNext;

        /// <summary>Peso relativo na escolha do gerador.</summary>
        public float Weight;
    }

    /// <summary>Pista fixa do MVP. O gerador da Fase 12 produz o mesmo formato. GDD 11.</summary>
    public sealed class TrackDef
    {
        public string Id;
        public string DisplayName;
        public string RegionId;
        public TierRank Tier;
        public string[] ModuleIds;

        /// <summary>Tempo de referencia do grid de adversarios. GDD 5.6.</summary>
        public float BaseTimeSeconds;

        public float ScoreMultiplier;
        public long BaseCash;
        public int BaseXp;

        /// <summary>Reputacao necessaria para a pista aparecer no mapa.</summary>
        public int ReputationRequired;
    }

    /// <summary>Peso de um clima dentro de uma regiao. GDD 11.5.</summary>
    public struct WeatherWeight
    {
        public Weather Weather;
        public float Weight;
    }

    /// <summary>Regiao do grafo de mapa. GDD 11.1.</summary>
    public sealed class RegionDef
    {
        public string Id;
        public string DisplayName;
        public string[] ModuleIds;
        public WeatherWeight[] Weather;
        public string[] PartPool;
        public float CashMultiplier;
        public float ScoreMultiplier;
        public bool IsUrban;
        public bool IsMountain;
        public string[] Neighbors;
        public int ReputationRequired;
    }

    /// <summary>Tier D..S. Cada tier muda a natureza, nao so a escala. GDD 14.2.</summary>
    public sealed class TierDef
    {
        public TierRank Rank;
        public float DifficultyScale;
        public float RewardScale;

        /// <summary>Ordem: Common, Uncommon, Rare, Epic, Legendary. GDD 10.2.</summary>
        public float[] RarityWeights;

        public int ItemLevelCap;
        public int ReputationRequired;
    }

    /// <summary>
    /// Rival nomeado. GDD 13.1.
    ///
    /// Rivais sao carros REAIS com build real, resolvidos pelo mesmo simulador - e por
    /// isso a build deles e legivel, contra-atacavel, e dropa a peca que usavam.
    /// </summary>
    public sealed class RivalDef
    {
        public string Id;
        public string DisplayName;
        public string CarId;
        public DriftStyle Style;

        /// <summary>Bonus plano aplicado sobre as stats base do carro do rival.</summary>
        public ResolvedStats StatBonus;

        /// <summary>A peca que ele usa - e que dropa quando e derrotado.</summary>
        public string SignaturePartId;

        public string LessonText;
        public int TierIndex;
    }
}
