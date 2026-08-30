// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 20.2, 21.2
// -----------------------------------------------------------------------------

using FramedDrift.Data;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O mundo minimo que os testes de balanceamento precisam.
    ///
    /// Carrega o conteudo REAL de Resources/Content - nao um fixture. Os testes da secao
    /// 21.2 sao sobre o jogo que sera publicado; rodar sobre dados inventados mediria
    /// outra coisa.
    /// </summary>
    public sealed class TestWorld
    {
        public readonly ContentDatabase Content;
        public readonly RaceResolver Resolver;
        public readonly LoadoutResolver Loadouts;
        public readonly RaceFactory Factory;

        private static TestWorld _shared;

        /// <summary>Uma instancia para toda a sessao: recarregar por teste seria desperdicio.</summary>
        public static TestWorld Shared
        {
            get
            {
                if (_shared == null) _shared = new TestWorld();
                return _shared;
            }
        }

        private TestWorld()
        {
            Content = GameContent.Load();
            Resolver = new RaceResolver(Content);
            Loadouts = new LoadoutResolver(Content);
            Factory = new RaceFactory(Content);
        }

        /// <summary>Build de fabrica: base artesanal, zero afixos. A linha de base honesta.</summary>
        public RolledPart[] FactoryBuild()
        {
            string[] stock =
            {
                "eng_stock", "tur_stock", "trn_stock", "dif_street",
                "sus_stock", "tir_street", "brk_stock", "aer_stock",
            };

            var parts = new RolledPart[8];
            for (int i = 0; i < stock.Length; i++) parts[i] = Resolver.Loot.Factory(stock[i]);
            return parts;
        }

        public RolledPart Part(string id)
        {
            return Resolver.Loot.Factory(id);
        }

        public RaceInstance Race(string carId, string trackId, RolledPart[] build, TuningSetup tuning,
                                 DriftStyle style, TimeOfDay time, Weather weather, ulong seed)
        {
            CarLoadout car = Loadouts.Resolve(Content.Car(carId), build, tuning, 0f);
            var conditions = new RaceConditions
            {
                Weather = weather,
                TimeOfDay = time,
                Traffic = TrafficDensity.Medium,
            };
            return Factory.Build(Content.Track(trackId), car, conditions, style, 1, 1f, seed);
        }

        /// <summary>Carro inicial, pista inicial, dia, seco. A referencia da secao 6.3.</summary>
        public RaceInstance StarterRace(ulong seed)
        {
            return Race("kite_130", "city_loop", FactoryBuild(), TuningSetup.Neutral,
                        DriftStyle.Balanced, TimeOfDay.Day, Weather.Clear, seed);
        }
    }
}
