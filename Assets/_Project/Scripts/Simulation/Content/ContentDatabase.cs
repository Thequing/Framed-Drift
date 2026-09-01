// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Content
//  GDD 0.2  secao 20.2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Content
{
    /// <summary>
    /// Todo o conteudo carregado, em C# puro e somente-leitura em tempo de execucao.
    ///
    /// Uma instancia, montada uma vez na inicializacao. O simulador recebe referencias
    /// daqui - nunca procura conteudo por conta propria, para que um teste possa montar
    /// um banco minimo sem tocar em arquivo.
    /// </summary>
    public sealed class ContentDatabase
    {
        public BalanceSettings Balance;

        public readonly Dictionary<string, CarDef> Cars = new Dictionary<string, CarDef>();
        public readonly Dictionary<string, PartDef> Parts = new Dictionary<string, PartDef>();
        public readonly Dictionary<string, AffixDef> Affixes = new Dictionary<string, AffixDef>();
        public readonly Dictionary<string, PassiveDef> Passives = new Dictionary<string, PassiveDef>();
        public readonly Dictionary<string, SetDef> Sets = new Dictionary<string, SetDef>();
        public readonly Dictionary<string, ModuleDef> Modules = new Dictionary<string, ModuleDef>();
        public readonly Dictionary<string, TrackDef> Tracks = new Dictionary<string, TrackDef>();
        public readonly Dictionary<string, RegionDef> Regions = new Dictionary<string, RegionDef>();
        public readonly Dictionary<string, RivalDef> Rivals = new Dictionary<string, RivalDef>();

        /// <summary>Indexado por <see cref="TierRank"/>.</summary>
        public TierDef[] Tiers = new TierDef[5];

        // --- listas na ordem de declaracao, para UI e sorteio ---------------------
        public readonly List<CarDef> CarList = new List<CarDef>();
        public readonly List<PartDef> PartList = new List<PartDef>();
        public readonly List<AffixDef> AffixList = new List<AffixDef>();
        public readonly List<PassiveDef> PassiveList = new List<PassiveDef>();
        public readonly List<TrackDef> TrackList = new List<TrackDef>();
        public readonly List<RegionDef> RegionList = new List<RegionDef>();
        public readonly List<RivalDef> RivalList = new List<RivalDef>();

        public CarDef Car(string id) { return Lookup(Cars, id, "carro"); }
        public PartDef Part(string id) { return Lookup(Parts, id, "peca"); }
        public ModuleDef Module(string id) { return Lookup(Modules, id, "modulo"); }
        public TrackDef Track(string id) { return Lookup(Tracks, id, "pista"); }
        public RegionDef Region(string id) { return Lookup(Regions, id, "regiao"); }
        public RivalDef Rival(string id) { return Lookup(Rivals, id, "rival"); }

        public CarDef CarOrNull(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CarDef v;
            return Cars.TryGetValue(id, out v) ? v : null;
        }

        public RivalDef RivalOrNull(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            RivalDef v;
            return Rivals.TryGetValue(id, out v) ? v : null;
        }

        public SetDef SetOrNull(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            SetDef v;
            return Sets.TryGetValue(id, out v) ? v : null;
        }

        public TierDef Tier(TierRank rank)
        {
            int i = (int)rank;
            if (Tiers == null || i < 0 || i >= Tiers.Length || Tiers[i] == null)
                throw new ContentException("Tier " + rank + " nao esta definido em tiers.json.");
            return Tiers[i];
        }

        /// <summary>
        /// Monta o array de segmentos de uma pista a partir dos ids de modulo.
        /// D-05: a pista e uma COMBINACAO de modulos autorais - nunca ruido.
        /// </summary>
        public Segment[] BuildTrack(TrackDef track)
        {
            Segment[] segments = new Segment[track.ModuleIds.Length];
            for (int i = 0; i < track.ModuleIds.Length; i++)
                segments[i] = Module(track.ModuleIds[i]).Segment;
            return segments;
        }

        private static T Lookup<T>(Dictionary<string, T> map, string id, string kind) where T : class
        {
            T v;
            if (id != null && map.TryGetValue(id, out v)) return v;
            throw new ContentException("Nenhum(a) " + kind + " com id '" + id + "'.");
        }
    }

    public sealed class ContentException : System.Exception
    {
        public ContentException(string message) : base(message) { }
    }
}
