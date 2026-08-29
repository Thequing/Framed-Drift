// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 5.1, 7.1, 8.2, 9.1, 10.2
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>Tipos de segmento de pista. GDD 5.1.</summary>
    public enum SegmentType
    {
        Straight,
        Gentle,
        Sharp,
        Hairpin,
        SCurve,
        Tunnel,
        Bridge,
        Junction,
    }

    /// <summary>Sentido da curva. GDD 5.1.</summary>
    public enum TurnDirection { None, Left, Right }

    /// <summary>Superficie do segmento; alimenta k_surface. GDD 5.2.</summary>
    public enum SurfaceType { Asphalt, WornAsphalt, Concrete, Gravel, Snow }

    /// <summary>Paredes do segmento; habilita bonus de proximidade e risco. GDD 5.1 / 6.1.</summary>
    public enum WallConfig { None, OneSide, BothSides }

    /// <summary>Clima; alimenta k_weather. GDD 5.2.</summary>
    public enum Weather { Clear, Cloudy, Rain, HeavyRain, Fog, Snow }

    /// <summary>Periodo do dia. GDD 11 / 19.4.</summary>
    public enum TimeOfDay { Day, Night }

    /// <summary>Densidade de trafego; alimenta condMod. GDD 5.5.</summary>
    public enum TrafficDensity { None, Light, Medium, Heavy }

    /// <summary>Estilo de pilotagem; alimenta estiloBias e estiloRisco. GDD 5.4 / 5.5.</summary>
    public enum DriftStyle { Safe, Balanced, Aggressive, Reckless }

    /// <summary>Qualidade de execucao de uma curva. GDD 5.5.</summary>
    public enum DriftQuality { Bad, Good, Perfect }

    /// <summary>Cinco raridades no lancamento. Prototype e Mythic ficam fora. GDD 10.2.</summary>
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>Os 8 slots equipaveis. GDD 9.1.</summary>
    public enum PartSlot
    {
        Engine,
        Turbo,
        Transmission,
        Differential,
        Suspension,
        Tires,
        Brakes,
        AeroWeight,
    }

    /// <summary>Tipo de diferencial: o slot de assinatura. GDD 9.2.</summary>
    public enum DifferentialType { Open, StreetLsd, Lsd15Way, Lsd2Way, Competition }

    /// <summary>Perfil de pneu; alimenta k_tire. GDD 5.2 / 9.3.</summary>
    public enum TireProfile { Street, Sport, SemiSlick, Drift, Racing, Wet }

    /// <summary>Layout de tracao do carro. GDD 8.2.</summary>
    public enum DriveLayout { FR, FF, MR, RR, AWD }

    /// <summary>Tier de dificuldade / progressao. GDD 8.2 / 14.</summary>
    public enum TierRank { D, C, B, A, S }
}
