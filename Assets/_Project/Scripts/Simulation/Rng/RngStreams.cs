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

        public RngStreams(ulong masterSeed)
        {
            Execution = new DeterministicRng(masterSeed ^ 0xA1B2C3D4E5F60718UL);
            Loot = new DeterministicRng(masterSeed ^ 0x1122334455667788UL);
            Events = new DeterministicRng(masterSeed ^ 0xDEADBEEFCAFEBABEUL);
            Opponents = new DeterministicRng(masterSeed ^ 0x0F1E2D3C4B5A6978UL);
        }
    }
}
