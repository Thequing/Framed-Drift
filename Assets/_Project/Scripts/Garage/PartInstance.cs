// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secao 10.1
// -----------------------------------------------------------------------------

using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Garage
{
    /// <summary>
    /// A visao de runtime de uma peca: o registro salvo (<see cref="SavedPart"/>) mais a
    /// rolagem ja materializada (<see cref="RolledPart"/>).
    ///
    /// A separacao existe porque o save guarda so a SEED (GDD 10.1) - o que mantem o
    /// arquivo pequeno e permite rebalancear pools sem invalidar inventarios - enquanto a
    /// UI precisa dos numeros prontos para desenhar o tooltip.
    /// </summary>
    public sealed class PartInstance
    {
        public readonly SavedPart Saved;
        public readonly RolledPart Rolled;

        public PartInstance(SavedPart saved, RolledPart rolled)
        {
            Saved = saved;
            Rolled = rolled;
        }

        public int Uid { get { return Saved.Uid; } }
        public PartSlot Slot { get { return Rolled.Slot; } }
        public Rarity Rarity { get { return Rolled.Rarity; } }
        public string DisplayName { get { return Rolled.DisplayName; } }
        public bool Locked { get { return Saved.Locked; } }

        /// <summary>
        /// Materializa uma peca salva. A seed zero e a peca de FABRICA - sem afixos -
        /// que o carro inicial vem equipado e a loja vende.
        /// </summary>
        public static PartInstance Materialize(SavedPart saved, LootRoller loot)
        {
            RolledPart rolled = saved.Seed == 0UL
                ? loot.Factory(saved.BaseId)
                : loot.Materialize(new Simulation.Outcomes.PartDrop
                {
                    BaseId = saved.BaseId,
                    Rarity = saved.Rarity,
                    ItemLevel = saved.ItemLevel,
                    Seed = saved.Seed,
                });

            return new PartInstance(saved, rolled);
        }

        /// <summary>Texto de tooltip completo. A cor codifica raridade e nada mais (GDD 18.5).</summary>
        public string Describe()
        {
            return Rolled.Describe();
        }
    }
}
