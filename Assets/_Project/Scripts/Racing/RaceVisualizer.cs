// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 19.1, 19.2, 20.6
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Core;
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
    /// <b>Estado atual: o carro e um cubo.</b> O modelo entra depois. As duas restricoes
    /// de silhueta da 19.1 - legivel a 360x48 e com YAW legivel da camera traseira
    /// elevada - sao requisito de modelagem, e o placeholder ja e alongado e assimetrico
    /// para que a rotacao apareca.
    /// </summary>
    public sealed class RaceVisualizer : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TrackAssembler _track;
        [SerializeField] private CameraRig _camera;
        [SerializeField] private DriftSmokeSystem _smoke;

        [Header("Placeholder do carro (substituir por modelo)")]
        [SerializeField] private Vector3 _carSize = new Vector3(1.8f, 0.9f, 4.2f);

        private Transform _car;
        private Renderer _carRenderer;
        private Material _carMaterial;

        private RaceResult _result;
        private RaceInstance _race;
        private float _visualYaw;

        /// <summary>Angulo de drift atual em graus. A camera e a fumaca leem daqui.</summary>
        public float CurrentYaw { get { return _visualYaw; } }

        public float CurrentSpeedKmh { get; private set; }
        public bool HasRace { get { return _result != null; } }

        private void Awake()
        {
            EnsureCar();
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

            if (_track != null) _track.Assemble(_race.Track);

            EnsureCar();
            PaintCar();

            if (_camera != null) _camera.Snap();
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _result == null || _track == null || _track.Path == null) return;

            SampleTimeline(game.PlaybackTime, out float distance, out float yaw, out float speed);

            Vector3 position = _track.Path.PositionAt(distance);
            float heading = _track.Path.HeadingDegreesAt(distance);

            // Amortece o yaw: o valor do simulador e por segmento, e um corte seco de 0
            // para 40 graus na fronteira leria como teleporte. Nada de corte brusco
            // (GDD 19.2) - toda transicao e amortecida.
            _visualYaw = Mathf.Lerp(_visualYaw, yaw, 1f - Mathf.Exp(-8f * Time.deltaTime));
            CurrentSpeedKmh = speed;

            _car.position = position + Vector3.up * (_carSize.y * 0.5f + 0.2f);
            _car.rotation = Quaternion.Euler(0f, heading + _visualYaw, 0f);

            if (_smoke != null) _smoke.SetState(_visualYaw, speed, CurrentQuality(), CurrentCombo());
        }

        /// <summary>
        /// Onde o carro esta, de acordo com a timeline ja resolvida.
        ///
        /// A distancia percorrida sai da fracao de tempo DENTRO do segmento corrente, nao
        /// de integrar velocidade: integrar acumularia erro de frame e o carro chegaria ao
        /// fim da pista num tempo diferente do que o simulador calculou.
        /// </summary>
        private void SampleTimeline(float playbackTime, out float distance, out float yaw, out float speed)
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

        // --- placeholder ---------------------------------------------------------------

        private void EnsureCar()
        {
            if (_car != null) return;

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
        }

        private void PaintCar()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _carMaterial == null) return;

            CarDef def = game.Content.Car(_race.Car.CarId);
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + def.PlaceholderColor, out color))
                _carMaterial.color = color;
        }

        public Transform Car { get { return _car; } }
    }
}
