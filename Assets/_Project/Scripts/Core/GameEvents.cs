// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secoes 17.4, 20.1, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.Simulation;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Core
{
    /// <summary>
    /// Publicado assim que a corrida foi RESOLVIDA e antes de a reproducao comecar.
    ///
    /// Note que ja carrega a Timeline inteira: como a corrida toda e conhecida antes de
    /// tocar (GDD 20.6), a UI pode telegrafar uma curva antes de o carro chegar nela, e o
    /// visualizador pode montar a pista de uma vez.
    /// </summary>
    public struct RaceStarted
    {
        public RaceInstance Race;
        public RaceResult Result;
    }

    /// <summary>
    /// Publicado quando a reproducao termina e o score ja foi calculado.
    ///
    /// So aqui existe DriftScore: ele nao faz parte do RaceResult porque a Timeline
    /// ainda podia ser editada pela Entrada Perfeita durante a reproducao (GDD 20.6).
    /// </summary>
    public struct RaceFinished
    {
        public RaceInstance Race;
        public RaceResult Result;
        public RaceRewards Rewards;

        /// <summary>Quanto do score veio das promocoes do jogador. Sempre 0 offline.</summary>
        public long ScoreFromPresence;

        /// <summary>O duelo desta corrida, ou null quando nao houve rival. GDD 13.1.</summary>
        public RivalOutcome Rival;
    }

    /// <summary>Publicado a cada segmento durante a reproducao. Alimenta os pop-ups da 6.4.</summary>
    public struct SegmentPlayed
    {
        public SegmentOutcome Outcome;
        public double Score;
    }

    /// <summary>Uma janela de Entrada Perfeita abriu. GDD 3.6.1.</summary>
    public struct InputWindowOpened
    {
        public InputWindow Window;
    }

    /// <summary>O jogador acertou a janela: a curva foi promovida de Good para Perfect.</summary>
    public struct CurvePromoted
    {
        public int SegmentIndex;
    }

    /// <summary>O relatorio de ausencia esta pronto. A tela de retorno assina isto. GDD 17.4.</summary>
    /// <summary>Um rival apareceu e espera resposta: DESAFIAR ou IGNORAR. GDD 13.1.</summary>
    public struct RivalDetected
    {
        public string RivalId;
        public string DisplayName;
        public string CarId;
        public string LessonText;
        public int Defeats;
        public int DefeatsUntilCarBlueprint;
    }

    /// <summary>O jogador venceu no Drift Score. GDD 13.1 / D-03.</summary>
    public struct RivalDefeated
    {
        public string RivalId;
        public string DisplayName;
        public int Defeats;
        public string SignaturePartId;
    }

    /// <summary>Tres derrotas fecharam a planta do carro dele. GDD 13.1.</summary>
    public struct CarBlueprintUnlocked
    {
        public string CarId;
        public string DisplayName;
        public string RivalId;
    }

    public struct OfflineReportReady
    {
        public OfflineReport Report;
    }

    /// <summary>Um coletavel apareceu sobre um segmento. GDD 3.6.2.</summary>
    public struct CollectibleSpawned
    {
        public CollectibleKind Kind;
        public int SegmentIndex;
        public float ExpiresAt;
    }

    /// <summary>Os tres coletaveis da secao 3.6.2.</summary>
    public enum CollectibleKind
    {
        /// <summary>Drone de patrocinador: cash imediato. ~1 a cada 4 corridas.</summary>
        SponsorDrone,

        /// <summary>Brilho de hot lap: +25% de chance de drop nesta corrida. ~1 a cada 6.</summary>
        HotLapGlow,

        /// <summary>Selo de reputacao: reputacao plana. ~1 a cada 10.</summary>
        ReputationStamp,
    }
}
