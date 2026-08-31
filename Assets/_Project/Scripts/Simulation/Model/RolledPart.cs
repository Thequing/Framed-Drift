// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 10.1, 10.3, 10.4
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Content;

namespace FramedDrift.Simulation.Model
{
    /// <summary>Um afixo ja rolado. GDD 10.3.</summary>
    public struct RolledAffix
    {
        public string AffixId;
        public StatId Stat;
        public float Value;

        /// <summary>Linha pronta para o tooltip: "+14 Potencia".</summary>
        public string Describe()
        {
            string sign = Value >= 0f ? "+" : "";
            return sign + Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                   + " " + StatOps.DisplayName(Stat);
        }
    }

    /// <summary>
    /// Uma peca materializada a partir da sua Seed.
    ///
    /// O save guarda BaseId + Rarity + ItemLevel + Seed; esta struct e reconstruida na
    /// carga (GDD 10.1). Guardar a semente em vez dos valores mantem o save pequeno e
    /// deixa o pool de afixos rebalancavel sem invalidar inventarios ja existentes.
    /// </summary>
    public sealed class RolledPart
    {
        public string BaseId;
        public string DisplayName;
        public PartSlot Slot;
        public Rarity Rarity;
        public int ItemLevel;
        public ulong Seed;

        public ResolvedStats BaseStats;
        public RolledAffix[] Affixes;

        public bool HasPassive;
        public PassiveEffect Passive;
        public string PassiveId;
        public string PassiveText;

        public string SetId;
        public TireProfile TireProfile;
        public bool IsTire;
        public DifferentialType DifferentialType;
        public bool IsDifferential;

        public long SellValue;

        public static readonly RolledAffix[] NoAffixes = new RolledAffix[0];

        public RolledPart()
        {
            Affixes = NoAffixes;
        }

        /// <summary>
        /// Texto de tooltip completo. A cor codifica raridade e nada mais (GDD 18.5).
        ///
        /// Mora na peca ROLADA, e nao na do inventario, porque a vitrine da loja precisa
        /// do mesmo texto para uma peca que ainda nao foi comprada.
        /// </summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(DisplayName).Append("  ").Append(Rarity);
            if (ItemLevel > 0) sb.Append("  iLvl ").Append(ItemLevel);

            for (int i = 0; i < Affixes.Length; i++)
                sb.Append('\n').Append("  ").Append(Affixes[i].Describe());

            if (HasPassive)
                sb.Append("\n  ").Append(PassiveText);

            return sb.ToString();
        }

        /// <summary>Stats totais desta peca: base artesanal mais afixos rolados.</summary>
        public ResolvedStats TotalStats()
        {
            ResolvedStats s = BaseStats;
            for (int i = 0; i < Affixes.Length; i++)
                StatOps.Add(ref s, Affixes[i].Stat, Affixes[i].Value);
            return s;
        }
    }

    /// <summary>
    /// Ajuste fino do diferencial (GDD 9.2), em tres eixos continuos 0-1 com 0,5 neutro.
    ///
    /// Nenhum eixo e "mais e melhor" - cada um paga o que ganha, o que e a razao de o
    /// tuning nao ser um botao de upgrade (GDD 9.4).
    /// </summary>
    public struct TuningSetup
    {
        /// <summary>Trava: Angle + / Grip -.</summary>
        public float Lock;

        /// <summary>Aceleracao: Initiation + / Stability -.</summary>
        public float Accel;

        /// <summary>Desaceleracao: Transition + / risco de Bad +.</summary>
        public float Decel;

        public static TuningSetup Neutral
        {
            get { return new TuningSetup { Lock = 0.5f, Accel = 0.5f, Decel = 0.5f }; }
        }
    }
}
