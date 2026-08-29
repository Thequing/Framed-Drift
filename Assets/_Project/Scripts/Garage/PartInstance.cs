// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secao 10.1
// -----------------------------------------------------------------------------

using System;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>
    /// Uma peca concreta no inventario. Serializada no save.
    /// A Seed reproduz a rolagem inteira - guardar a seed em vez dos valores mantem
    /// o save pequeno e permite rebalancear pools sem invalidar inventarios.
    /// </summary>
    [Serializable]
    public class PartInstance
    {
        /// <summary>Referencia a PartData artesanal.</summary>
        public string BaseId;

        public Rarity Rarity;

        /// <summary>Deriva do tier da pista onde caiu.</summary>
        public int ItemLevel;

        /// <summary>Reproduz a rolagem de afixos e passiva.</summary>
        public ulong Seed;

        // TODO(Fase 9): Affixes (0-4) e Passive (0-1, Rare+) materializados da Seed.
    }
}
