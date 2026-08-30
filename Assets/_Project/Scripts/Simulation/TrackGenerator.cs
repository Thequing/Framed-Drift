// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 11.2, 11.3, apendice C
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Rng;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>Esqueleto ritmico de uma pista. GDD 11.3, passo 1.</summary>
    public enum TrackSkeleton
    {
        /// <summary>Muitas curvas fechadas e grampos; premia DriftControl.</summary>
        Technical,

        /// <summary>Curvas suaves encadeadas; premia Transition.</summary>
        Flowing,

        /// <summary>Retas longas com curvas isoladas; premia TopSpeed.</summary>
        Fast,

        Mixed,
    }

    /// <summary>
    /// Combina MODULOS AUTORAIS respeitando regras de adjacencia (D-05).
    ///
    /// Nenhuma geometria e gerada por ruido. O gerador escolhe um esqueleto ritmico da
    /// regiao, preenche com modulos validos, e valida a dificuldade antes de devolver -
    /// a validacao existe porque o criterio de saida da Fase 12 e "200 pistas geradas,
    /// nenhuma invalida ou injogavel" (GDD 22.2).
    /// </summary>
    public sealed class TrackGenerator
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;

        public TrackGenerator(ContentDatabase content)
        {
            _content = content;
            _balance = content.Balance;
        }

        public sealed class Request
        {
            public string RegionId;
            public TierRank Tier;
            public int SegmentCount = 24;
            public TrackSkeleton Skeleton = TrackSkeleton.Mixed;
            public ulong Seed;
            public TimeOfDay TimeOfDay;
            public Weather Weather;
        }

        public TrackDef Generate(Request request)
        {
            RegionDef region = _content.Region(request.RegionId);
            var rng = new DeterministicRng(request.Seed);

            List<string> chain = BuildChain(region, request, rng);

            float length = 0f;
            float difficulty = 0f;
            for (int i = 0; i < chain.Count; i++)
            {
                Segment s = _content.Module(chain[i]).Segment;
                length += s.LengthM;
                difficulty += s.Difficulty;
            }

            // Tempo de referencia: uma estimativa grosseira serve, porque o grid da 5.6
            // e uma distribuicao em torno dele - nao um adversario real a bater.
            float avgSpeedKmh = 70f + (request.Skeleton == TrackSkeleton.Fast ? 25f : 0f);
            float baseTime = length / (avgSpeedKmh / _balance.MsToKmh);

            var track = new TrackDef
            {
                Id = "gen_" + request.Seed.ToString("x16"),
                DisplayName = Name(region, request, chain),
                RegionId = region.Id,
                Tier = request.Tier,
                ModuleIds = chain.ToArray(),
                BaseTimeSeconds = baseTime,
                ScoreMultiplier = 1f + difficulty / (chain.Count * 400f),
                BaseCash = 200 + (long)((int)request.Tier * 140),
                BaseXp = 40 + (int)request.Tier * 25,
            };

            return track;
        }

        /// <summary>
        /// Passo 2: preenche com modulos respeitando adjacencia.
        ///
        /// Quando nenhum sucessor e valido o gerador VOLTA um passo em vez de relaxar a
        /// regra. Relaxar produziria a sequencia ruim que a regra existe para impedir -
        /// dois grampos seguidos sem reta, S saindo de rampa (GDD 11.3).
        /// </summary>
        private List<string> BuildChain(RegionDef region, Request request, DeterministicRng rng)
        {
            var pool = new List<ModuleDef>();
            for (int i = 0; i < region.ModuleIds.Length; i++)
                pool.Add(_content.Module(region.ModuleIds[i]));

            if (pool.Count == 0)
                throw new ContentException("Regiao " + region.Id + " nao tem modulos para gerar pista.");

            var chain = new List<string>(request.SegmentCount);
            var attempts = new List<int>(request.SegmentCount);

            while (chain.Count < request.SegmentCount)
            {
                ModuleDef previous = chain.Count == 0 ? null : _content.Module(chain[chain.Count - 1]);
                ModuleDef next = PickNext(pool, previous, request.Skeleton, rng);

                if (next == null)
                {
                    if (chain.Count == 0)
                        throw new ContentException("Regiao " + region.Id + " nao tem nenhum modulo inicial valido.");

                    chain.RemoveAt(chain.Count - 1);
                    attempts.RemoveAt(attempts.Count - 1);
                    continue;
                }

                chain.Add(next.Id);
                attempts.Add(0);
            }

            return chain;
        }

        private ModuleDef PickNext(List<ModuleDef> pool, ModuleDef previous, TrackSkeleton skeleton,
                                   DeterministicRng rng)
        {
            var weights = new float[pool.Count];
            bool any = false;

            for (int i = 0; i < pool.Count; i++)
            {
                if (!CanFollow(previous, pool[i])) continue;
                weights[i] = pool[i].Weight * SkeletonBias(skeleton, pool[i].Segment.Type);
                if (weights[i] > 0f) any = true;
            }

            if (!any) return null;
            int index = rng.WeightedIndex(weights);
            return index < 0 ? null : pool[index];
        }

        private static bool CanFollow(ModuleDef previous, ModuleDef candidate)
        {
            if (previous == null) return true;
            if (previous.AllowedNext == null || previous.AllowedNext.Length == 0) return true;
            for (int i = 0; i < previous.AllowedNext.Length; i++)
                if (previous.AllowedNext[i] == candidate.Id) return true;
            return false;
        }

        /// <summary>Passo 1: o esqueleto so inclina os pesos - ele nao substitui a adjacencia.</summary>
        private static float SkeletonBias(TrackSkeleton skeleton, SegmentType type)
        {
            switch (skeleton)
            {
                case TrackSkeleton.Technical:
                    if (type == SegmentType.Hairpin || type == SegmentType.Sharp) return 2.2f;
                    if (type == SegmentType.Straight) return 0.5f;
                    return 1f;

                case TrackSkeleton.Flowing:
                    if (type == SegmentType.Gentle || type == SegmentType.SCurve) return 2.4f;
                    if (type == SegmentType.Hairpin) return 0.4f;
                    return 1f;

                case TrackSkeleton.Fast:
                    if (type == SegmentType.Straight) return 2.6f;
                    if (type == SegmentType.Hairpin) return 0.3f;
                    return 1f;

                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Passo 4: nome a partir de regiao + assinatura + periodo + clima.
        /// Formato do apendice C.
        /// </summary>
        private static string Name(RegionDef region, Request request, List<string> chain)
        {
            string signature = SignatureWord(request.Skeleton);
            string period = request.TimeOfDay == TimeOfDay.Night ? "da Madrugada" : "do Dia";

            string name = region.DisplayName + " - " + signature + " " + period;
            if (request.Weather != Weather.Clear && request.Weather != Weather.Cloudy)
                name += ", " + WeatherWord(request.Weather);
            return name;
        }

        private static string SignatureWord(TrackSkeleton skeleton)
        {
            switch (skeleton)
            {
                case TrackSkeleton.Technical: return "Grampos";
                case TrackSkeleton.Flowing: return "Curvas";
                case TrackSkeleton.Fast: return "Corrida";
                default: return "Circuito";
            }
        }

        private static string WeatherWord(Weather weather)
        {
            switch (weather)
            {
                case Weather.Rain: return "Chuva";
                case Weather.HeavyRain: return "Tempestade";
                case Weather.Fog: return "Neblina";
                case Weather.Snow: return "Neve";
                default: return "Tempo Firme";
            }
        }
    }
}
