// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 19.1, 19.2, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// Consome um <see cref="RaceResult"/> que ja contem a linha do tempo completa.
    ///
    /// O visualizador NAO simula nada. Ele interpola o carro ao longo do tracado
    /// respeitando os tempos de segmento e dispara VFX nos eventos que ja foram
    /// decididos (GDD 20.6). Consequencias diretas dessa escolha:
    ///
    /// - o "replay" sai de graca: e a mesma funcao com a mesma timeline;
    /// - o modo Compacto e o Taskbar consomem a MESMA timeline com outro renderizador;
    /// - minimizar no meio da corrida nao perde nada, porque a fisica ja existe.
    ///
    /// <b>Carro:</b> se existir <c>Resources/Cars/&lt;id do carro&gt;</c> o modelo entra;
    /// senao cai no cubo placeholder. Os dois convivem de proposito - so um dos tres
    /// carros do MVP tem modelo, e um carro sem modelo precisa continuar rodando em vez
    /// de sumir da pista.
    ///
    /// As duas restricoes de silhueta da 19.1 - legivel a 360x48 e com YAW legivel da
    /// camera traseira elevada - continuam sendo requisito de modelagem. O placeholder e
    /// alongado e tem faixa justamente para nao mentir sobre elas.
    /// </summary>
    public sealed class RaceVisualizer : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TrackAssembler _track;
        [SerializeField] private CameraRig _camera;
        [SerializeField] private DriftSmokeSystem _smoke;
        [SerializeField] private SpeedStreaks _streaks;

        [Header("Placeholder do carro (usado quando nao ha modelo)")]
        [SerializeField] private Vector3 _carSize = new Vector3(1.8f, 0.9f, 4.2f);

        /// <summary>Onde o instanciador procura os modelos, por id de carro.</summary>
        private const string CarResourceFolder = "Cars/";

        private Transform _car;
        private Renderer _carRenderer;
        private Material _carMaterial;
        private CarView _carView;
        private string _carId;
        private bool _carBuilt;
        private float _groundOffset;

        /// <summary>
        /// Giro da PISTA, em graus por segundo, abaixo do qual nao ha curva nenhuma. Uma
        /// reta da exatamente zero (o <see cref="TrackPath"/> usa curvatura 0 nela), entao
        /// esta margem so absorve ruido de amostragem.
        /// </summary>
        private const float CornerMinTurnRate = 4f;

        /// <summary>Giro em que o sinal satura. ~26 graus/s e uma curva media do MVP a 100 km/h.</summary>
        private const float CornerFullTurnRate = 26f;

        private const float CornerAttack = 12f;
        private const float CornerRelease = 3f;

        private RaceResult _result;
        private RaceInstance _race;
        private float _visualYaw;
        private float _corner;
        private float _lastHeading;
        private bool _hasHeading;

        /// <summary>Angulo de drift atual em graus. A camera e a fumaca leem daqui.</summary>
        public float CurrentYaw { get { return _visualYaw; } }

        /// <summary>
        /// 0 em reta, 1 em curva fechada, com transicao amortecida. A camera pendura
        /// tranco e abertura de FOV aqui.
        ///
        /// Sai da VELOCIDADE DE GIRO DA PISTA - quanto o heading do tracado muda por
        /// segundo - e nao do tipo do segmento. A diferenca importa nas duas pontas:
        ///
        /// - <b>comeco.</b> O sinal por tipo de segmento ligava ANTES da curva existir,
        ///   de proposito, com uma antecipacao de 0,8 s. Na tela isso lia como a camera
        ///   reagindo a uma curva que o carro ainda nao tinha comecado a fazer. O giro
        ///   real e zero na reta e so cresce quando o carro entra na curva, entao o
        ///   efeito comeca exatamente junto com o carro.
        /// - <b>forca.</b> Curvatura x velocidade e o que o corpo sente numa curva, entao
        ///   uma curva fechada a 180 km/h agora pesa mais que uma aberta a 90.
        ///
        /// Continua nao saindo do yaw: o yaw tem forma de seno dentro de cada segmento e
        /// volta a zero em toda fronteira, entao efeitos pendurados nele piscariam numa
        /// sequencia de curvas. O giro da pista e constante ao longo do trecho, e a
        /// soltura lenta (<see cref="CornerRelease"/>) cobre a reta curta entre duas.
        /// </summary>
        public float CurrentCorner { get { return _corner; } }

        public float CurrentSpeedKmh { get; private set; }
        public bool HasRace { get { return _result != null; } }

        private void Start()
        {
            // Nao da para montar em Awake: o Bootstrap cria este componente DENTRO do
            // proprio Awake dele, e o GameManager so termina de bootar no fim do Awake
            // dele. Em Start o save ja existe e da para perguntar qual carro esta
            // equipado - que e o que tem de aparecer na tela, nao um cubo.
            SyncIdleCar();
            if (_car == null) BuildCar(null);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RaceStarted>(OnRaceStarted);
        }

        private void OnRaceStarted(RaceStarted evt)
        {
            _race = evt.Race;
            _result = evt.Result;
            _corner = 0f;
            _hasHeading = false;

            if (_track != null) _track.Assemble(_race.Track);

            BuildCar(_race.Car.CarId);
            PaintCar(_race.Car.CarId);

            if (_camera != null) _camera.Snap();
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            if (_result == null || _track == null || _track.Path == null)
            {
                // Fora de corrida quem aparece e o carro EQUIPADO. Trocar de carro na
                // garagem tem de trocar o que esta na pista na hora, sem esperar a
                // proxima largada.
                SyncIdleCar();
                return;
            }

            SampleTimeline(game.PlaybackTime, out float distance, out float yaw, out float speed);

            Vector3 position = _track.Path.PositionAt(distance);
            float heading = _track.Path.HeadingDegreesAt(distance);

            float dt = Time.deltaTime;

            // Amortece o yaw: o valor do simulador e por segmento, e um corte seco de 0
            // para 40 graus na fronteira leria como teleporte. Nada de corte brusco
            // (GDD 19.2) - toda transicao e amortecida.
            _visualYaw = Mathf.Lerp(_visualYaw, yaw, 1f - Mathf.Exp(-8f * dt));

            // Quanto a PISTA girou neste frame. No primeiro frame de uma corrida nao ha
            // com o que comparar, e um delta contra lixo daria um pico de curva na
            // largada - que e justamente uma reta.
            float turnRate = _hasHeading && dt > 0.0001f
                ? Mathf.Abs(Mathf.DeltaAngle(_lastHeading, heading)) / dt
                : 0f;
            _lastHeading = heading;
            _hasHeading = true;

            float corner = Mathf.InverseLerp(CornerMinTurnRate, CornerFullTurnRate, turnRate);

            // Sobe rapido e desce devagar: entrar na curva e um evento, sair dela e um
            // alivio. A soltura lenta tambem cobre qualquer buraco de um frame entre
            // dois trechos de curva.
            float rate = corner > _corner ? CornerAttack : CornerRelease;
            _corner = Mathf.Lerp(_corner, corner, 1f - Mathf.Exp(-rate * dt));

            CurrentSpeedKmh = speed;

            _car.position = position + Vector3.up * _groundOffset;
            _car.rotation = Quaternion.Euler(0f, heading + _visualYaw, 0f);

            if (_carView != null) _carView.Tick(speed, _visualYaw, Time.deltaTime);
            if (_smoke != null) _smoke.SetState(_visualYaw, speed, CurrentQuality(), CurrentCombo());
            if (_streaks != null) _streaks.SetState(speed, _visualYaw);
        }

        /// <summary>
        /// Onde o carro esta, de acordo com a timeline ja resolvida.
        ///
        /// A distancia percorrida sai da fracao de tempo DENTRO do segmento corrente, nao
        /// de integrar velocidade: integrar acumularia erro de frame e o carro chegaria ao
        /// fim da pista num tempo diferente do que o simulador calculou.
        /// </summary>
        private void SampleTimeline(float playbackTime, out float distance, out float yaw,
                                   out float speed)
        {
            SegmentOutcome[] timeline = _result.Timeline;
            distance = 0f;
            yaw = 0f;
            speed = 0f;

            for (int i = 0; i < timeline.Length; i++)
            {
                SegmentOutcome outcome = timeline[i];
                float end = outcome.TimeOffset + outcome.Duration;

                if (playbackTime >= end)
                {
                    distance = _track.Path.SegmentStart(i) + outcome.LengthM;
                    continue;
                }

                float t = outcome.Duration <= 0.0001f
                    ? 1f
                    : Mathf.Clamp01((playbackTime - outcome.TimeOffset) / outcome.Duration);

                distance = _track.Path.SegmentStart(i) + outcome.LengthM * t;
                speed = outcome.VDrift;

                if (outcome.Drift)
                {
                    // O angulo visual e o Angle da build mapeado para graus, com uma
                    // entrada e uma saida suaves dentro do segmento. Perfect sustenta
                    // mais angulo que Good, e Bad colapsa - que e o que a 19.3 pede que
                    // a fumaca comunique.
                    float shape = Mathf.Sin(t * Mathf.PI);
                    float quality = QualityAngleFactor(outcome.QualityFinal);
                    float side = _race.Track[i].Direction == TurnDirection.Left ? -1f : 1f;
                    yaw = outcome.Angle * 0.55f * shape * quality * side;
                }
                return;
            }

            distance = _track.Path.TotalLength;
            speed = timeline.Length > 0 ? timeline[timeline.Length - 1].VExit : 0f;
        }

        private static float QualityAngleFactor(DriftQuality quality)
        {
            switch (quality)
            {
                case DriftQuality.Perfect: return 1f;
                case DriftQuality.Bad: return 0.45f;
                default: return 0.85f;
            }
        }

        private DriftQuality CurrentQuality()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _result == null) return DriftQuality.Good;

            int i = Mathf.Clamp(game.CurrentSegment, 0, _result.Timeline.Length - 1);
            return _result.Timeline[i].QualityFinal;
        }

        private int CurrentCombo()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _result == null) return 0;

            int i = Mathf.Clamp(game.CurrentSegment, 0, _result.Timeline.Length - 1);
            return _result.Timeline[i].Combo;
        }

        // --- carro -----------------------------------------------------------------------

        /// <summary>
        /// Monta o carro do id pedido. Reconstroi so quando o id muda, para que duas
        /// corridas seguidas com o mesmo carro nao joguem o modelo fora a toa.
        /// </summary>
        private bool BuildCar(string carId)
        {
            if (_carBuilt && _carId == carId) return false;

            if (_car != null) DestroyImmediate(_car.gameObject);

            _car = null;
            _carView = null;
            _carRenderer = null;
            _carMaterial = null;
            _carId = carId;
            _carBuilt = true;

            GameObject prefab = string.IsNullOrEmpty(carId)
                ? null
                : Resources.Load<GameObject>(CarResourceFolder + carId);

            if (prefab != null) BuildModel(prefab);
            else BuildPlaceholder();

            return true;
        }

        /// <summary>Poe o carro equipado na pista, e o troca quando a garagem trocar.</summary>
        private void SyncIdleCar()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            CarInstance car = game.ActiveCar();
            if (car == null) return;

            if (BuildCar(car.Id)) PaintCar(car.Id);
        }

        private void BuildModel(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            go.name = "Car (" + _carId + ")";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _car = go.transform;
            _carView = go.AddComponent<CarView>();
            _carView.Bind();

            // O modelo tem pivo no chao, entao ele assenta na superficie do asfalto. O
            // cubo tem pivo no centro e por isso pede outra conta - ver BuildPlaceholder.
            float surface = _track != null ? _track.SurfaceOffset : 0.125f;
            _groundOffset = surface + 0.01f;
        }

        private void BuildPlaceholder()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Car (placeholder)";
            go.transform.SetParent(transform, false);
            go.transform.localScale = _carSize;
            DestroyImmediate(go.GetComponent<Collider>());

            _car = go.transform;
            _carRenderer = go.GetComponent<Renderer>();

            _carMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _carRenderer.sharedMaterial = _carMaterial;

            // Uma faixa que atravessa o eixo longitudinal e a exigencia de silhueta da
            // 19.1: sem ela, o topo do carro vira uma mancha uniforme e o teste de yaw
            // falha. O modelo tera de resolver isso de verdade; o cubo so nao mente sobre
            // o requisito.
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "YawStripe";
            stripe.transform.SetParent(_car, false);
            stripe.transform.localScale = new Vector3(0.22f, 1.05f, 1.02f);
            DestroyImmediate(stripe.GetComponent<Collider>());

            var stripeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            stripeMaterial.color = new Color(1f, 0.85f, 0.2f);
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMaterial;

            _groundOffset = _carSize.y * 0.5f + 0.2f;
        }

        private void PaintCar(string carId)
        {
            GameManager game = GameManager.Instance;
            if (game == null || _car == null || string.IsNullOrEmpty(carId)) return;

            CarDef def = game.Content.Car(carId);
            Color color;
            if (!ColorUtility.TryParseHtmlString("#" + def.PlaceholderColor, out color)) return;

            if (_carView != null) _carView.SetPaint(color);
            else if (_carMaterial != null) _carMaterial.color = color;
        }

        public Transform Car { get { return _car; } }
    }
}
