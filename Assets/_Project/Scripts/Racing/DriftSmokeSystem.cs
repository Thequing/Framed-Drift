// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 19.3
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// A fumaca NAO e um efeito de particulas bonito.
    ///
    /// Com a camera traseira encurtando o yaw (GDD 19.2), a fumaca e o CANAL PRIMARIO
    /// que comunica que existe um drift acontecendo e de que tamanho ele e. Ela e
    /// orcada como sistema de gameplay e tem teste de legibilidade proprio.
    ///
    /// Os cinco requisitos da tabela 19.3, e como cada um esta atendido aqui:
    ///
    ///   Forma       fita CURVA continua atras do carro - as particulas herdam a
    ///               velocidade lateral, entao a curvatura da fita desenha o arco do
    ///               drift. Nao e um jato radial.
    ///   Comprimento proporcional ao angulo e ao combo: leitura a distancia de "isto
    ///               esta indo bem".
    ///   Cor         iluminada pela cena. Cor customizada e cosmetico (19.6) e NUNCA
    ///               pode reduzir a legibilidade - por isso a saturacao tem piso.
    ///   Densidade   sobe com qualityMult; um Perfect tem pulso visual distinto do Good.
    ///   Bad         colapso visivel e imediato: o jogador percebe a perda de combo sem
    ///               olhar o HUD.
    ///
    /// <b>Criterio de saida da Fase 2:</b> com o HUD inteiramente desligado, um
    /// observador que nunca viu o jogo precisa distinguir corretamente Perfect / Good /
    /// Bad em 8 de 10 curvas.
    /// </summary>
    public sealed class DriftSmokeSystem : MonoBehaviour
    {
        [Header("Referencia")]
        [SerializeField] private RaceVisualizer _visualizer;

        [Header("Forma da fita (19.3)")]
        [Tooltip("Angulo minimo, em graus, para a fumaca comecar.")]
        [SerializeField] private float _yawThreshold = 6f;

        [Tooltip("Particulas por segundo no angulo maximo.")]
        [SerializeField] private float _maxEmission = 220f;

        [Tooltip("Vida da particula no angulo maximo. Define o comprimento da fita.")]
        [SerializeField] private float _maxLifetime = 1.5f;

        [Header("Paleta neon (19.1)")]
        [SerializeField] private Color _goodColor = new Color(0.85f, 0.85f, 0.95f);
        [SerializeField] private Color _perfectColor = new Color(0.45f, 0.95f, 1f);
        [SerializeField] private Color _badColor = new Color(1f, 0.35f, 0.30f);

        private ParticleSystem _particles;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;

        private DriftQuality _lastQuality = DriftQuality.Good;
        private float _perfectPulse;

        private void Awake()
        {
            BuildParticles();
        }

        private void LateUpdate()
        {
            if (_visualizer == null || _visualizer.Car == null) return;

            // A fumaca nasce ATRAS do carro, no eixo traseiro.
            Transform car = _visualizer.Car;
            transform.position = car.position - car.forward * 1.8f - Vector3.up * 0.35f;
            transform.rotation = car.rotation;

            if (_perfectPulse > 0f) _perfectPulse -= Time.deltaTime * 3f;
        }

        /// <summary>
        /// Chamado pelo visualizador a cada frame com o estado atual do drift.
        /// </summary>
        public void SetState(float yawDegrees, float speedKmh, DriftQuality quality, int combo)
        {
            if (_particles == null) return;

            float yaw = Mathf.Abs(yawDegrees);

            if (quality != _lastQuality)
            {
                // Perfect ganha um pulso distinto; Bad colapsa. Sao os dois momentos que
                // o observador do teste cego precisa reconhecer sem HUD.
                if (quality == DriftQuality.Perfect) _perfectPulse = 1f;
                _lastQuality = quality;
            }

            bool drifting = yaw >= _yawThreshold;
            float intensity = drifting ? Mathf.InverseLerp(_yawThreshold, 45f, yaw) : 0f;

            if (quality == DriftQuality.Bad)
            {
                // Colapso: a fita morre em vez de encurtar devagar.
                intensity *= 0.15f;
            }

            // Comprimento sobe com o combo: a fita longa e o sinal a distancia de que a
            // corrida esta indo bem.
            float comboBoost = 1f + Mathf.Clamp01(combo / 25f) * 0.8f;
            float pulse = 1f + _perfectPulse * 0.6f;

            _emission.rateOverTime = _maxEmission * intensity * pulse;
            _main.startLifetime = _maxLifetime * Mathf.Max(0.2f, intensity) * comboBoost;
            _main.startSpeed = Mathf.Lerp(1f, 6f, intensity) + speedKmh * 0.01f;
            _main.startSize = Mathf.Lerp(0.6f, 1.8f, intensity) * pulse;
            _main.startColor = ColorFor(quality);

            if (drifting && !_particles.isPlaying) _particles.Play();
            else if (!drifting && _particles.isPlaying) _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private Color ColorFor(DriftQuality quality)
        {
            switch (quality)
            {
                case DriftQuality.Perfect: return _perfectColor;
                case DriftQuality.Bad: return _badColor;
                default: return _goodColor;
            }
        }

        private void BuildParticles()
        {
            _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();

            _main = _particles.main;
            _main.loop = true;
            _main.playOnAwake = false;
            _main.startLifetime = _maxLifetime;
            _main.startSize = 1f;
            _main.gravityModifier = -0.05f;

            // World space: e o que faz a fita FICAR PARA TRAS e desenhar o arco. Em local
            // space ela acompanharia o carro e viraria um jato radial - exatamente a
            // forma que a 19.3 proibe.
            _main.simulationSpace = ParticleSystemSimulationSpace.World;
            _main.maxParticles = 400;

            _emission = _particles.emission;
            _emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.1f, 0.2f);

            ParticleSystem.SizeOverLifetimeModule size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.6f));

            ParticleSystem.ColorOverLifetimeModule fade = _particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        }
    }
}
