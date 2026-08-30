// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secoes 5.1, 11.6, 14.2
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// Tudo que o simulador precisa para resolver uma corrida.
    ///
    /// RaceInstance + Seed reproduzem a corrida byte a byte (GDD 20.3). Nenhum campo
    /// aqui pode ser um objeto vivo do jogo: o auto-equipar monta 200 destes por
    /// avaliacao (GDD 16.3) e nao pode tocar no estado do jogador.
    /// </summary>
    public sealed class RaceInstance
    {
        public Segment[] Track;
        public CarLoadout Car;
        public DriverStats Driver;
        public RaceConditions Conditions;

        /// <summary>Estilo de pilotagem. Alimenta estiloBias e estiloRisco. GDD 5.4 / 5.5.</summary>
        public DriftStyle Style;

        /// <summary>Semente mestra. Os fluxos por dominio derivam dela. GDD 20.3.</summary>
        public ulong Seed;

        // --- contexto da pista (11.6, 14.2) -------------------------------------

        public string TrackId;
        public string TrackDisplayName;
        public string RegionId;

        /// <summary>Indice do tier: D = 0 .. S = 4. Alimenta t_ref e a dificuldade. GDD 5.6.</summary>
        public int TierIndex;

        /// <summary>Stage dentro do tier, 1..20. GDD 14.2.</summary>
        public int Stage;

        /// <summary>Escala de dificuldade do tier, ja incluindo o stage. GDD 14.2.</summary>
        public float DifficultyScale;

        /// <summary>Tempo de referencia base do grid de adversarios. GDD 5.6.</summary>
        public float TrackBaseTimeSeconds;

        /// <summary>trackMult da secao 6.2, ja combinando pista e regiao.</summary>
        public float ScoreMultiplier;

        /// <summary>Parte de rewardMult que nao depende de clima/horario/trafego. GDD 11.6.</summary>
        public float RewardMultiplier;

        public long TrackBaseCash;
        public int TrackBaseXp;

        /// <summary>Ids das pecas que esta regiao pode dropar. GDD 11.1.</summary>
        public string[] PartPool;

        /// <summary>Multiplicador permanente vindo do prestigio. GDD 6.2 / 14.5.</summary>
        public float PrestigeMultiplier;

        /// <summary>A regiao e urbana? Alimenta a trait Carro de rua. GDD 8.3.</summary>
        public bool RegionIsUrban;
        public bool RegionIsMountain;

        public RaceInstance()
        {
            DifficultyScale = 1f;
            ScoreMultiplier = 1f;
            RewardMultiplier = 1f;
            PrestigeMultiplier = 1f;
            Driver = DriverStats.Neutral;
        }
    }
}
