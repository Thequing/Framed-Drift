// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secoes 3.6, 16, 17, 20.1, 20.6
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using FramedDrift.Automation;
using FramedDrift.Core;
using FramedDrift.Data;
using FramedDrift.Garage;
using FramedDrift.Progression;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using UnityEngine;

namespace FramedDrift.App
{
    /// <summary>
    /// Composicao raiz. Instancia os sistemas, cuida do ciclo de vida da sessao e roda o
    /// loop de corrida.
    ///
    /// O loop tem duas fases, e a separacao e a razao de D-02 funcionar (GDD 20.6):
    ///
    ///   1. RESOLVER  - o simulador decide a corrida inteira, de uma vez.
    ///   2. REPRODUZIR - o visualizador toca a linha do tempo; a Entrada Perfeita edita
    ///                   apenas a QUALIDADE de curvas ja resolvidas; no fim, o scorer
    ///                   reduz a timeline a um numero.
    ///
    /// Offline pula a fase 2 e chama o MESMO scorer com a timeline intocada. Nao existe
    /// um segundo solucionador em lugar nenhum deste arquivo - e essa ausencia que
    /// sustenta o pilar P3.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Reproducao")]
        [Tooltip("1 = tempo real. Acelerar so muda a apresentacao, nunca o resultado.")]
        [SerializeField] private float _playbackSpeed = 1f;

        [Tooltip("Resolve sem reproduzir. E o que o modo Segundo Plano usa (GDD 17.1).")]
        [SerializeField] private bool _headless;

        // --- sistemas ------------------------------------------------------------
        public ContentDatabase Content { get; private set; }
        public SaveData Save { get; private set; }

        public RaceResolver Resolver { get; private set; }
        public RaceFactory Factory { get; private set; }
        public LoadoutResolver Loadouts { get; private set; }
        public TrackGenerator Generator { get; private set; }
        public OfflineAggregator Offline { get; private set; }

        public InventoryManager Inventory { get; private set; }
        public BuildManager Builds { get; private set; }
        public Crafting Crafting { get; private set; }
        public Shop Shop { get; private set; }

        public EconomyLedger Economy { get; private set; }
        public ReputationSystem Reputation { get; private set; }
        public MissionSystem Missions { get; private set; }
        public PrestigeSystem Prestige { get; private set; }
        public AchievementSystem Achievements { get; private set; }

        public RuleEngine Rules { get; private set; }
        public FleetManager Fleet { get; private set; }
        public AutomationRules Automation { get; private set; }
        public TimeManager Clock { get; private set; }

        public List<CarInstance> Cars { get; private set; }

        // --- estado da corrida em curso ---------------------------------------------
        public RaceInstance CurrentRace { get; private set; }
        public RaceResult CurrentResult { get; private set; }
        public bool IsRacing { get; private set; }

        /// <summary>Segundos decorridos DENTRO da corrida reproduzida.</summary>
        public float PlaybackTime { get; private set; }

        /// <summary>Score acumulado ate o segmento ja reproduzido. Sobe com tweening na UI.</summary>
        public double RunningScore { get; private set; }

        public int CurrentSegment { get; private set; }
        public OfflineReport PendingOfflineReport { get; private set; }

        private CollectibleHaul _haul;
        private DriftScorer.Context _scoreContext;
        private float _autosaveTimer;
        private Weather _forecast = Weather.Clear;

        // --- ciclo de vida ------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EventBus.Clear();   // estaticos sobrevivem ao Play Mode no editor
            Boot();
        }

        private void Boot()
        {
            Content = GameContent.Load();

            Resolver = new RaceResolver(Content);
            Factory = new RaceFactory(Content);
            Loadouts = new LoadoutResolver(Content);
            Generator = new TrackGenerator(Content);
            Offline = new OfflineAggregator(Content, Resolver);

            Save = SaveManager.Load();
            bool freshGame = Save == null;
            if (freshGame) Save = NewGame();

            Clock = new TimeManager(Content.Balance.RealSecondsPerGameDay);
            Clock.SetClock(Save.GameClockHours);

            Economy = new EconomyLedger(Save, Content.Balance);
            Inventory = new InventoryManager(Save, Resolver.Loot, Economy);
            Builds = new BuildManager(Content, Resolver, Factory);
            Crafting = new Crafting(Save, Content, Inventory, Resolver.Loot, Economy);
            Shop = new Shop(Save, Content, Inventory, Economy, Resolver.Loot);
            Reputation = new ReputationSystem(Save, Content);
            Missions = new MissionSystem();
            Prestige = new PrestigeSystem(Save);
            Achievements = new AchievementSystem(Save);

            Automation = AutomationRules.From(Save.Automation);
            Rules = new RuleEngine(Save, Content, Inventory, Crafting, Builds, Economy, Reputation);
            Fleet = new FleetManager(Save, Content.Balance);

            MaterializeCars();
            Reputation.Evaluate();
            Fleet.Rebind(Cars);

            if (!freshGame) ResolveAbsence();

            ExecutionBudget.Apply(ExecutionState.Active);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Novo jogo: um carro, pecas de fabrica, uma pista.
        ///
        /// A primeira hora da secao 22.3 comeca aqui e exige que os primeiros dois
        /// minutos sejam "uma corrida, um carro, sem menu". Por isso nada aqui precisa de
        /// escolha do jogador - a garagem so abre aos 5 minutos.
        /// </summary>
        private SaveData NewGame()
        {
            var save = new SaveData();
            save.LastTimestamp = System.DateTime.UtcNow;
            save.Cash = 0;
            save.Progress.UnlockedCars.Add("kite_130");
            save.Progress.UnlockedTracks.Add("city_loop");
            save.Cars.Add(new SavedCar
            {
                CarId = "kite_130",
                Builds = { new SavedBuild { Name = "BUILD 01" } },
            });
            return save;
        }

        private void MaterializeCars()
        {
            Cars = new List<CarInstance>(Save.Cars.Count);

            for (int i = 0; i < Save.Cars.Count; i++)
            {
                SavedCar saved = Save.Cars[i];
                CarDef def = Content.Car(saved.CarId);
                var car = new CarInstance(saved, def, Loadouts, Inventory);

                if (saved.Builds.Count == 0) saved.Builds.Add(new SavedBuild { Name = "BUILD 01" });
                EquipFactoryPartsIfEmpty(car);

                Cars.Add(car);
            }
        }

        /// <summary>
        /// Um carro recem-adquirido vem com as pecas de serie montadas.
        ///
        /// Sem isso, o carro novo sairia da loja com oito slots vazios e correria PIOR que
        /// o inicial - o que leria como bug, nao como decisao.
        /// </summary>
        private void EquipFactoryPartsIfEmpty(CarInstance car)
        {
            string[] stock =
            {
                "eng_stock", "tur_stock", "trn_stock", "dif_street",
                "sus_stock", "tir_street", "brk_stock", "aer_stock",
            };

            SavedBuild build = car.ActiveBuild;
            for (int slot = 0; slot < stock.Length; slot++)
            {
                if (build.SlotUids[slot] > 0 && Inventory.Get(build.SlotUids[slot]) != null) continue;

                PartInstance part = Inventory.AddFactory(stock[slot]);
                if (part != null) build.SlotUids[slot] = part.Uid;
            }
            car.MarkDirty();
        }

        // --- ausencia (17) ------------------------------------------------------------

        /// <summary>
        /// Reconstroi o que aconteceu enquanto o jogo estava fechado.
        ///
        /// Nao credita nada ainda: o relatorio fica PENDENTE ate o jogador apertar
        /// COLETAR na tela de retorno (GDD 17.4). Creditar em silencio transformaria a
        /// melhor tela do jogo num numero que ninguem le.
        /// </summary>
        private void ResolveAbsence()
        {
            float elapsed = SaveManager.SecondsSince(Save);
            if (elapsed < 60f) return;

            CarInstance car = ActiveCar();
            if (car == null) return;

            Clock.Advance(elapsed);

            RaceInstance prototype = BuildRace(car, Save.Progress.TrackId, 0UL);
            OfflineRules rules = Automation.ToOfflineRules(Save.InventoryFreeSlots);

            PendingOfflineReport = Offline.Aggregate(
                prototype, elapsed, Save.Prestige.ExtraShiftLevels, rules,
                car.Damage, (ulong)Save.LastTimestamp.Ticks);

            EventBus.Publish(new OfflineReportReady { Report = PendingOfflineReport });
        }

        /// <summary>Credita o relatorio pendente. So o botao COLETAR chama isto.</summary>
        public void CollectOfflineReport()
        {
            OfflineReport report = PendingOfflineReport;
            if (report == null) return;

            Economy.AddCash(report.Cash);
            Economy.AddScrap(report.Scrap);
            Economy.AddReputation(report.Reputation);

            CarInstance car = ActiveCar();
            if (car != null)
            {
                car.ApplyRaceDamage(report.DamageTaken);
                car.Saved.RacesRun += report.Races;
                car.Saved.Wins += report.Wins;
                car.Saved.Xp += report.Xp;
            }

            for (int i = 0; i < report.KeptDrops.Count; i++)
                if (Inventory.Add(report.KeptDrops[i]) == null) break;

            // Pedaco de planta nao ocupa slot, entao a ausencia nunca os perde por
            // inventario cheio - ao contrario dos drops acima, que param no primeiro que
            // nao couber.
            for (int i = 0; i < report.BlueprintFragments.Count; i++)
                Crafting.AddBlueprintFragment(report.BlueprintFragments[i]);

            Missions.RecordOffline(report);
            Reputation.Evaluate();

            PendingOfflineReport = null;
            SaveNow();
        }

        // --- loop de corrida --------------------------------------------------------------

        public CarInstance ActiveCar()
        {
            if (Cars == null || Cars.Count == 0) return null;
            int i = Mathf.Clamp(Save.ActiveCarIndex, 0, Cars.Count - 1);
            return Cars[i];
        }

        public TrackDef ActiveTrack()
        {
            return Content.Track(Save.Progress.TrackId);
        }

        /// <summary>
        /// Monta a instancia da corrida. As CONDICOES saem do relogio do jogo e do sorteio
        /// de clima da regiao - nunca de escolha implicita: o jogador ve a previsao com
        /// duas corridas de antecedencia (GDD 11.5) e a automacao pode reagir a ela.
        /// </summary>
        public RaceInstance BuildRace(CarInstance car, string trackId, ulong seed)
        {
            TrackDef track = Content.Track(trackId);
            RegionDef region = Content.Region(track.RegionId);

            var conditions = new RaceConditions
            {
                Weather = _forecast,
                TimeOfDay = Clock.Period,
                Traffic = Clock.Period == TimeOfDay.Day ? TrafficDensity.Heavy : TrafficDensity.Medium,
            };

            return Factory.Build(track, car.Loadout, conditions, car.Style,
                                 Save.Progress.Stage, Save.Prestige.ScoreMultiplier, seed);
        }

        public void StartRace()
        {
            if (IsRacing) return;
            if (Rules.IsStopped) Rules.Resume();
            StartCoroutine(RaceRoutine());
        }

        private IEnumerator RaceRoutine()
        {
            CarInstance car = ActiveCar();
            if (car == null) yield break;

            IsRacing = true;

            // --- fase 1: resolver -------------------------------------------------
            ulong seed = NextSeed();
            CurrentRace = BuildRace(car, Save.Progress.TrackId, seed);
            CurrentResult = Resolver.Simulate(CurrentRace);

            _scoreContext = Resolver.Scorer.BuildContext(CurrentRace);
            _haul = CollectibleHaul.None;
            PlaybackTime = 0f;
            RunningScore = 0.0;
            CurrentSegment = 0;

            EventBus.Publish(new RaceStarted { Race = CurrentRace, Result = CurrentResult });

            // --- fase 2: reproduzir ------------------------------------------------
            if (!_headless)
            {
                while (CurrentSegment < CurrentResult.Timeline.Length)
                {
                    PlaybackTime += Time.deltaTime * Mathf.Max(0.05f, _playbackSpeed);
                    AdvancePlayback();
                    yield return null;
                }
            }
            else
            {
                PlaybackTime = CurrentResult.TotalTime;
                CurrentSegment = CurrentResult.Timeline.Length;
            }

            // --- fase 3: pontuar e recompensar ---------------------------------------
            FinishRace(car);

            yield return new WaitForSeconds(Fleet.InterRaceDelay);
            IsRacing = false;

            if (Automation.AutoStartRace && Reputation.HasAutomation("auto_race") && !Rules.IsStopped)
                StartRace();
        }

        /// <summary>
        /// Avanca a reproducao ate o segmento cujo tempo ja passou.
        ///
        /// Nao interpola fisica: a fisica ja aconteceu. Isto so decide QUANDO cada evento
        /// ja resolvido aparece na tela - e por isso minimizar no meio nao perde nada
        /// (GDD 20.6).
        /// </summary>
        private void AdvancePlayback()
        {
            SegmentOutcome[] timeline = CurrentResult.Timeline;

            while (CurrentSegment < timeline.Length
                   && PlaybackTime >= timeline[CurrentSegment].TimeOffset + timeline[CurrentSegment].Duration)
            {
                SegmentOutcome outcome = timeline[CurrentSegment];
                double segScore = Resolver.Scorer.SegmentScore(in outcome, in _scoreContext);
                RunningScore += segScore * _scoreContext.TotalMultiplier;

                EventBus.Publish(new SegmentPlayed { Outcome = outcome, Score = segScore });
                CurrentSegment++;
            }
        }

        /// <summary>
        /// A Entrada Perfeita. Promove UMA curva de Good para Perfect, e nada mais muda -
        /// nem trajetoria, nem velocidade de saida, nem combo (GDD 3.6.1).
        /// </summary>
        public bool TryPromote(int segmentIndex)
        {
            if (CurrentResult == null) return false;
            if (!CurrentResult.Promote(segmentIndex)) return false;

            EventBus.Publish(new CurvePromoted { SegmentIndex = segmentIndex });
            return true;
        }

        /// <summary>Registra um coletavel apanhado. Nunca existe offline (GDD 3.6.2).</summary>
        public void CollectCollectible(CollectibleKind kind)
        {
            var b = Content.Balance;
            _haul.Count++;

            switch (kind)
            {
                case CollectibleKind.SponsorDrone:
                    _haul.CashBonus += (long)(CurrentRace.TrackBaseCash * b.CollectibleDroneCashFactor);
                    break;
                case CollectibleKind.HotLapGlow:
                    _haul.DropChanceBonus += b.CollectibleHotLapDropBonus;
                    break;
                case CollectibleKind.ReputationStamp:
                    _haul.ReputationBonus += b.CollectibleRepFlat;
                    break;
            }
        }

        private void FinishRace(CarInstance car)
        {
            RaceRewards rewards = Resolver.Finalize(CurrentRace, CurrentResult, _haul);

            long offlineScore = Resolver.Scorer.ScoreWithoutPromotions(CurrentResult, in _scoreContext);
            long presence = rewards.DriftScore - offlineScore;

            Economy.AddCash(rewards.Cash);
            Economy.AddReputation(rewards.Reputation);

            for (int i = 0; i < rewards.BlueprintIds.Length; i++)
                Crafting.AddBlueprintFragment(rewards.BlueprintIds[i]);

            car.ApplyRaceDamage(CurrentResult.Damage);
            car.Saved.Xp += rewards.Xp;
            car.Saved.RacesRun++;
            if (CurrentResult.Won) car.Saved.Wins++;
            if (rewards.DriftScore > car.Saved.BestScore) car.Saved.BestScore = rewards.DriftScore;

            Missions.RecordRace(CurrentResult, CurrentRace.Conditions.TimeOfDay == TimeOfDay.Night);
            Achievements.Evaluate(CurrentResult, CurrentRace, CurrentResult.RiskIndex, Content.Balance);

            Rules.AfterRace(car, CurrentResult, Automation, ActiveTrack(), CurrentRace.Conditions);
            Reputation.Evaluate();

            RollForecast();

            EventBus.Publish(new RaceFinished
            {
                Race = CurrentRace,
                Result = CurrentResult,
                Rewards = rewards,
                ScoreFromPresence = presence,
            });

            SaveNow();
        }

        /// <summary>
        /// Sorteia o clima da PROXIMA corrida, no fluxo de eventos.
        ///
        /// Sorteado com antecedencia de proposito: o jogador ve "chuva chegando" e a
        /// automacao pode trocar para a build de chuva antes da largada (GDD 11.5 / 16.2).
        /// </summary>
        private void RollForecast()
        {
            RegionDef region = Content.Region(ActiveTrack().RegionId);
            var events = new RngStreams(NextSeed()).Events;
            _forecast = Factory.RollWeather(region, events);
        }

        public Weather Forecast { get { return _forecast; } }

        private ulong NextSeed()
        {
            // Semente derivada do tempo de jogo, nao de UnityEngine.Random: o replay de um
            // bug reportado depende de a seed ser reproduzivel a partir do save (GDD 20.3).
            ulong ticks = (ulong)System.DateTime.UtcNow.Ticks;
            return ticks ^ ((ulong)Save.NextPartUid << 32) ^ (ulong)(Save.PlaySeconds * 1000.0);
        }

        // --- tempo e save -------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;

            Save.PlaySeconds += dt;
            Clock.Tick(dt);
            Save.GameClockHours = Clock.ClockHours;

            _autosaveTimer += dt;
            if (_autosaveTimer >= SaveManager.AutosaveIntervalSeconds)
            {
                _autosaveTimer = 0f;
                SaveNow();
            }
        }

        public void SaveNow()
        {
            Automation.CopyTo(Save.Automation);
            Save.GameClockHours = Clock.ClockHours;
            SaveManager.Save(Save);
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationFocus(bool focused)
        {
            // Segundo plano: tick agregado, render desligado, menos de 1% de CPU (GDD 17.1).
            ExecutionBudget.Apply(focused ? ExecutionState.Active : ExecutionState.Background);
            if (!focused) SaveNow();
        }
    }
}
