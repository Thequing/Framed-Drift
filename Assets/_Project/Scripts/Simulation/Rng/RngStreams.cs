// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Rng
//  GDD 0.2  secao 20.3
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Rng
{
    /// <summary>
    /// Fluxos de RNG separados por dominio, derivados da mesma seed mestra.
    ///
    /// A razao (GDD 20.3): mudar a tabela de loot NAO pode alterar o resultado da
    /// corrida. Se os dois consumissem o mesmo fluxo, um balanceamento de drop mudaria
    /// silenciosamente quem ganhou a corrida - e todo replay salvo quebraria.
    /// </summary>
    public sealed class RngStreams
    {
        /// <summary>Drift, qualidade, falha - tudo que decide a corrida.</summary>
        public readonly DeterministicRng Execution;

        /// <summary>Raridade, afixos, passivas.</summary>
        public readonly DeterministicRng Loot;

        /// <summary>Rivais, clima, eventos raros.</summary>
        public readonly DeterministicRng Events;

        /// <summary>Tempos dos adversarios amostrados de N(t_ref, sigma). GDD 5.6.</summary>
        public readonly DeterministicRng Opponents;

        /// <summary>
        /// Encontro de rival: SE um rival aparece e QUAL (GDD 13.1).
        ///
        /// Fluxo proprio pelo mesmo motivo que loot e execucao sao separados: o rival e
        /// simulado de verdade, e se a corrida dele consumisse o fluxo do jogador, incluir
        /// um rival deslocaria as curvas do proprio jogador. A corrida do rival roda com
        /// um conjunto de fluxos derivado desta seed, nunca com o do jogador.
        /// </summary>
        public readonly DeterministicRng Rival;

        public RngStreams(ulong masterSeed)
        {
            Execution = new DeterministicRng(masterSeed ^ 0xA1B2C3D4E5F60718UL);
            Loot = new DeterministicRng(masterSeed ^ 0x1122334455667788UL);
            Events = new DeterministicRng(masterSeed ^ 0xDEADBEEFCAFEBABEUL);
            Opponents = new DeterministicRng(masterSeed ^ 0x0F1E2D3C4B5A6978UL);
            Rival = new DeterministicRng(masterSeed ^ 0x5A17C0DEF00DBEEFUL);
        }
    }
}
