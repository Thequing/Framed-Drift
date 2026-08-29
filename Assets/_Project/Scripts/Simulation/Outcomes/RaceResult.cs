// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Outcomes
//  GDD 0.2  secoes 20.6, 3.6
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
    /// </summary>
    [System.Serializable]
    public class RaceResult
    {
        public float TotalTime;
        public int Position;
        public int MaxCombo;
        public float DistanceM;
        public float Damage;

        public RaceRewards Rewards;

        /// <summary>A corrida, segmento a segmento. GDD 5.5.</summary>
        public SegmentOutcome[] Timeline;

        /// <summary>Janelas de Entrada Perfeita. Vazio quando resolvido offline. GDD 3.6.1.</summary>
        public InputWindow[] InputWindows;
    }
}
