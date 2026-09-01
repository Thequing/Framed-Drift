// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secoes 13.1, 3.5, 5.6, 20.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;

namespace FramedDrift.Simulation
{
    /// <summary>O que aconteceu entre o jogador e o rival numa corrida. GDD 13.1.</summary>
    public sealed class RivalOutcome
    {
        public string RivalId;
        public string RivalDisplayName;
        public string CarId;
        public string SignaturePartId;

        public float RivalTime;
        public long RivalScore;
        public int RivalPosition;

        /// <summary>
        /// Falhas mecanicas na corrida DELE (GDD 12.3). E o que deixa "as vezes se
        /// destroi sozinho" visivel na tela de resultado - e mensuravel no teste - sem
        /// nenhum caso especial: sai de pFail_seg sobre a build do rival.
        /// </summary>
        public int RivalFailures;

        public float PlayerTime;
        public long PlayerScore;
        public int PlayerPosition;

        /// <summary>Ele cruzou a linha na frente? E a metade da tensao da secao 3.5.</summary>
        public bool RivalFinishedAhead { get { return RivalTime < PlayerTime; } }

        /// <summary>
        /// Derrota, e a UNICA definicao dela: Drift Score, nunca posicao (D-03).
        ///
        /// E o que torna a licao do AKIRA ensinavel - ele chega na frente e perde - em vez
        /// de uma frase no JSON que o codigo contradiz.
        /// </summary>
        public bool PlayerWon { get { return PlayerScore > RivalScore; } }
    }

    /// <summary>
    /// Rivais nomeados: carros REAIS, com build real, resolvidos pelo MESMO simulador
    /// (GDD 13.1).
    ///
    /// O <see cref="RaceSimulator"/> ja dizia isto em comentario desde a Fase 3 - o grid
    /// anonimo da secao 5.6 e uma distribuicao, e "rivais nomeados sao a excecao". Esta
    /// classe e essa excecao. Sortear o tempo do rival de uma gaussiana seria muito mais
    /// barato e mataria a premissa inteira: a build dele precisa ser legivel e
    /// contra-atacavel, e isso exige que ela passe pelo solucionador de verdade.
    ///
    /// O rival OCUPA uma vaga do grid em vez de acrescentar uma. Ele sempre esteve entre
    /// os N carros da largada; a diferenca e que agora um deles e resolvido, e nao
    /// amostrado.
    /// </summary>
    public sealed class RivalResolver
    {
        /// <summary>
        /// Sal que deriva os fluxos da corrida DO RIVAL a partir da mesma seed mestra.
        ///
        /// Sem ele o rival consumiria o fluxo Execution do jogador e incluir um rival
        /// mudaria as curvas do proprio jogador - toda seed salva passaria a render outro
        /// resultado, em silencio (GDD 20.3).
        /// </summary>
        private const ulong RivalRaceSalt = 0x9E3779B97F4A7C15UL;

        private static readonly string[] StockBuild =
        {
            "eng_stock", "tur_stock", "trn_stock", "dif_street",
            "sus_stock", "tir_street", "brk_stock", "aer_stock",
        };

        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;
        private readonly RaceResolver _resolver;
        private readonly LoadoutResolver _loadouts;

        public RivalResolver(ContentDatabase content, RaceResolver resolver)
        {
            _content = content;
            _balance = content.Balance;
            _resolver = resolver;
            _loadouts = new LoadoutResolver(content);
        }

        // --- o encontro (13.1) --------------------------------------------------------

        /// <summary>
        /// Sorteia SE um rival aparece nesta corrida, e qual. Devolve null quando nenhum.
        ///
        /// A tiragem da chance acontece ANTES do filtro por tier, sempre: se o filtro
        /// viesse primeiro, acrescentar um rival ao conteudo deslocaria o fluxo e mudaria
        /// todos os encontros ja sorteados (GDD 20.3).
        /// </summary>
        public RivalDef RollEncounter(RaceInstance race, DeterministicRng rng)
        {
            if (_balance.RivalEncounterChance <= 0f) return null;
            if (!rng.Chance(_balance.RivalEncounterChance)) return null;

            List<RivalDef> eligible = EligibleFor(race.TierIndex);
            if (eligible.Count == 0) return null;

            return eligible[rng.Range(0, eligible.Count)];
        }

        /// <summary>Os rivais que correm no tier informado. GDD 14.2: cada tier traz os seus.</summary>
        public List<RivalDef> EligibleFor(int tierIndex)
        {
            var list = new List<RivalDef>();
            for (int i = 0; i < _content.RivalList.Count; i++)
                if (_content.RivalList[i].TierIndex == tierIndex) list.Add(_content.RivalList[i]);
            return list;
        }

        // --- a corrida do rival -------------------------------------------------------

        /// <summary>
        /// Resolve a corrida do rival na MESMA pista, com as MESMAS condicoes, e coloca o
        /// tempo dele no grid do jogador.
        ///
        /// O resultado do jogador e reescrito num unico campo - a posicao - porque um
        /// carro anonimo virou um carro de verdade. Timeline, tempo e score dele nao sao
        /// tocados: o rival nao pode mudar a corrida que o jogador ja correu.
        /// </summary>
        public RivalOutcome Resolve(RaceInstance race, RivalDef rival, RaceResult playerResult)
        {
            RaceInstance twin = Twin(race, rival);
            var streams = new RngStreams(race.Seed ^ RivalRaceSalt);

            RaceResult rivalResult = _resolver.Simulator.Simulate(twin, streams);

            DriftScorer.Context rivalCtx = _resolver.Scorer.BuildContext(twin);
            long rivalScore = _resolver.Scorer.Score(rivalResult.Timeline, in rivalCtx);

            var outcome = new RivalOutcome
            {
                RivalId = rival.Id,
                RivalDisplayName = rival.DisplayName,
                CarId = rival.CarId,
                SignaturePartId = rival.SignaturePartId,
                RivalTime = rivalResult.TotalTime,
                RivalScore = rivalScore,
                RivalFailures = rivalResult.Failures,
                PlayerTime = playerResult.TotalTime,
                PlayerScore = PlayerScore(race, playerResult),
            };

            PlaceOnGrid(playerResult, outcome);
            return outcome;
        }

        /// <summary>
        /// O score que o jogador levou para casa. Depois da reproducao ele ja esta em
        /// Rewards - inclusive com as promocoes da Entrada Perfeita, que o rival tem de
        /// enfrentar de verdade. Antes dela, pontua a timeline como esta.
        /// </summary>
        private long PlayerScore(RaceInstance race, RaceResult playerResult)
        {
            if (playerResult.Rewards != null) return playerResult.Rewards.DriftScore;

            DriftScorer.Context ctx = _resolver.Scorer.BuildContext(race);
            return _resolver.Scorer.Score(playerResult.Timeline, in ctx);
        }

        /// <summary>
        /// Troca UM adversario anonimo pelo rival e recalcula as duas posicoes.
        ///
        /// Trocar em vez de acrescentar mantem o tamanho do grid da secao 5.6 constante:
        /// um jogador que encontra um rival nao passa a correr contra um carro a mais.
        /// </summary>
        private static void PlaceOnGrid(RaceResult playerResult, RivalOutcome outcome)
        {
            float[] times = playerResult.OpponentTimes;
            if (times == null || times.Length == 0)
            {
                outcome.PlayerPosition = playerResult.Position;
                outcome.RivalPosition = outcome.RivalFinishedAhead ? 1 : 2;
                return;
            }

            times[0] = outcome.RivalTime;

            int aheadOfPlayer = 0;
            int aheadOfRival = 0;
            for (int i = 0; i < times.Length; i++)
            {
                if (times[i] < playerResult.TotalTime) aheadOfPlayer++;
                if (i != 0 && times[i] < outcome.RivalTime) aheadOfRival++;
            }
            if (playerResult.TotalTime < outcome.RivalTime) aheadOfRival++;

            playerResult.Position = 1 + aheadOfPlayer;
            outcome.PlayerPosition = playerResult.Position;
            outcome.RivalPosition = 1 + aheadOfRival;
        }

        // --- a build do rival (13.1) --------------------------------------------------

        /// <summary>
        /// A corrida do rival: mesma pista, mesmas condicoes, outro carro e outro estilo.
        /// </summary>
        private RaceInstance Twin(RaceInstance race, RivalDef rival)
        {
            return new RaceInstance
            {
                Track = race.Track,
                Car = BuildLoadout(rival),
                Driver = race.Driver,
                Conditions = race.Conditions,
                Style = rival.Style,
                Seed = race.Seed,

                TrackId = race.TrackId,
                TrackDisplayName = race.TrackDisplayName,
                RegionId = race.RegionId,
                TierIndex = race.TierIndex,
                Stage = race.Stage,
                DifficultyScale = race.DifficultyScale,
                TrackBaseTimeSeconds = race.TrackBaseTimeSeconds,
                ScoreMultiplier = race.ScoreMultiplier,
                RewardMultiplier = race.RewardMultiplier,
                TrackBaseCash = race.TrackBaseCash,
                TrackBaseXp = race.TrackBaseXp,
                PartPool = race.PartPool,

                // O prestigio e do JOGADOR. Dar o multiplicador dele ao rival faria o
                // rival ficar mais forte a cada temporada do jogador, sem que nada no
                // conteudo dissesse isso.
                PrestigeMultiplier = 1f,

                RegionIsUrban = race.RegionIsUrban,
                RegionIsMountain = race.RegionIsMountain,
            };
        }

        /// <summary>
        /// A build do rival: pecas de serie, a assinatura dele no lugar dela, e o bonus
        /// plano por cima.
        ///
        /// A assinatura entra como peca EQUIPADA, e nao como numero, porque e literalmente
        /// "a peca que ele estava usando" - a mesma que dropa quando ele perde (13.1).
        /// </summary>
        public CarLoadout BuildLoadout(RivalDef rival)
        {
            CarDef car = _content.Car(rival.CarId);

            var equipped = new RolledPart[8];
            for (int i = 0; i < StockBuild.Length; i++)
            {
                RolledPart p = _resolver.Loot.Factory(StockBuild[i]);
                if (p != null) equipped[(int)p.Slot] = p;
            }

            // A build declarada no conteudo por cima da de serie. Um rival de tier alto
            // que corresse com pecas de serie seria uma vitoria de graca: a peca do
            // JOGADOR sobe a cada tier, e a do rival tem de subir junto.
            if (rival.BuildPartIds != null)
            {
                for (int i = 0; i < rival.BuildPartIds.Length; i++)
                {
                    RolledPart p = _resolver.Loot.Factory(rival.BuildPartIds[i]);
                    if (p != null) equipped[(int)p.Slot] = p;
                }
            }

            if (!string.IsNullOrEmpty(rival.SignaturePartId))
            {
                RolledPart sig = _resolver.Loot.Factory(rival.SignaturePartId);
                if (sig != null) equipped[(int)sig.Slot] = sig;
            }

            CarLoadout loadout = _loadouts.Resolve(car, equipped, TuningSetup.Neutral, 0f);

            ResolvedStats stats = StatOps.Sum(in loadout.Stats, rival.StatBonus);
            StatOps.ClampToValidRanges(ref stats);
            loadout.Stats = stats;

            return loadout;
        }
    }
}
