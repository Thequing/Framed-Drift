// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secoes 20.6, 3.6, 12
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Outcomes
{
    /// <summary>
    /// A corrida inteira, ja resolvida, antes de qualquer visualizacao.
    ///
    /// REPARE QUE NAO EXISTE UM CAMPO DriftScore AQUI. Isso e proposital (GDD 20.6):
    /// o score e calculado por <c>DriftScorer.Score(timeline)</c> DEPOIS que a corrida
    /// termina de tocar, porque a Timeline ainda pode ser editada pelo input do jogador
    /// durante a reproducao (GDD 3.6.1).
    ///
    /// Chamar o mesmo scorer com a timeline intocada devolve o resultado offline. E a
    /// mesma funcao - por isso a paridade de D-02 e estrutural, e nao algo a testar.
    ///
    /// <see cref="Rewards"/> segue a mesma regra: fica nulo ate o
    /// <c>RaceResolver.Finalize</c> rodar, porque cash e xp dependem do score (GDD 15.2).
    /// </summary>
    [System.Serializable]
    public class RaceResult
    {
        public float TotalTime;
        public int Position;
        public int MaxCombo;
        public float DistanceM;

        /// <summary>Dano sofrido NESTA corrida. Somado ao dano persistente do carro.</summary>
        public float Damage;

        /// <summary>Nulo ate a finalizacao. Ver o resumo da classe.</summary>
        public RaceRewards Rewards;

        /// <summary>A corrida, segmento a segmento. GDD 5.5.</summary>
        public SegmentOutcome[] Timeline;

        /// <summary>Janelas de Entrada Perfeita. Vazio quando resolvido offline. GDD 3.6.1.</summary>
        public InputWindow[] InputWindows;

        // --- contexto, para o log e para a UI de resultado -----------------------

        public string TrackDisplayName;
        public string CarId;
        public ulong Seed;

        /// <summary>0-100, quatro faixas na UI. GDD 12.1.</summary>
        public float RiskIndex;

        public int Failures;
        public int Collisions;
        public int DriftSegments;
        public int PerfectSegments;
        public int BadSegments;

        public float FinalTireWear;
        public float FinalTemperature;

        /// <summary>Tempos amostrados do grid. GDD 5.6.</summary>
        public float[] OpponentTimes;
        public float ReferenceTime;

        /// <summary>Quantas janelas o jogador acertou. Sempre 0 offline. GDD 3.6.1.</summary>
        public int PromotionsApplied;

        /// <summary>Coletaveis apanhados. Nunca existem offline. GDD 3.6.2.</summary>
        public int CollectiblesTaken;

        public bool Won { get { return Position == 1; } }

        /// <summary>Desfaz todas as promocoes. O caminho offline e o replay usam isto.</summary>
        public void ResetPromotions()
        {
            if (Timeline == null) return;
            for (int i = 0; i < Timeline.Length; i++)
                Timeline[i].ResetPromotion();
            PromotionsApplied = 0;
        }

        /// <summary>
        /// Aplica a Entrada Perfeita a um segmento: Good vira Perfect, e nada mais muda.
        /// Devolve false quando a curva nao era promovivel (Bad continua sendo erro do
        /// simulador, GDD 3.6.1).
        /// </summary>
        public bool Promote(int segmentIndex)
        {
            if (Timeline == null || segmentIndex < 0 || segmentIndex >= Timeline.Length) return false;
            if (!Timeline[segmentIndex].Promotable) return false;
            if (Timeline[segmentIndex].QualityFinal == Model.DriftQuality.Perfect) return false;

            Timeline[segmentIndex].QualityFinal = Model.DriftQuality.Perfect;
            PromotionsApplied++;
            return true;
        }
    }
}
