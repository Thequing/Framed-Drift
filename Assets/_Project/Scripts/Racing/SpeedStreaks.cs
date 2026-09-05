// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 19.1, 19.2
// -----------------------------------------------------------------------------

using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// Riscos brancos passando pela camera nas RETAS.
    ///
    /// A camera elevada da 19.2 foi escolhida para legibilidade de yaw, nao para
    /// sensacao de velocidade: naquela altura o ponto de fuga quase nao se mexe e a
    /// estrada a frente encurta. O que sobra para vender velocidade e movimento
    /// PERIFERICO - coisas passando perto da tela.
    ///
    /// Por isso as particulas sao ESTATICAS no mundo. Quem se move e a camera, e a
    /// esteira sai do proprio deslocamento dela (<c>cameraVelocityScale</c>): o risco
    /// tem exatamente o comprimento da velocidade real, sem nenhum numero inventado.
    ///
    /// <b>Os numeros sao mais agressivos do que pareceriam sozinhos</b> porque este
    /// sistema herdou o trabalho do motion blur, que foi removido por borrar o CARRO
    /// (ver Bootstrap.CreatePostProcessing). Densidade, alfa e comprimento do risco
    /// subiram para cobrir o buraco - e este canal, ao contrario do blur, so pinta o
    /// que passa AO LADO da camera, entao nada disso custa nitidez de silhueta.
    ///
    /// <b>Some nas curvas de proposito.</b> Na curva o canal de leitura e a fumaca
    /// (19.3), que e orcada como sistema de gameplay e tem teste cego proprio. Riscos
    /// brancos por cima dela competiriam com o unico canal que comunica o angulo do
    /// drift - o efeito de velocidade nao pode custar a leitura do drift.
    /// </summary>
    public sealed class SpeedStreaks : MonoBehaviour
    {
        [Header("Referencia")]
        [SerializeField] private RaceVisualizer _visualizer;

        [Header("Quando aparecer")]
        [Tooltip("Acima deste angulo o carro esta em curva e os riscos saem de cena.")]
        [SerializeField] private float _yawThreshold = 6f;

        [Tooltip("Abaixo desta velocidade nao ha o que vender.")]
        [SerializeField] private float _minSpeedKmh = 22f;

        [Tooltip("Velocidade em que a densidade satura.")]
        [SerializeField] private float _fullSpeedKmh = 190f;

        [Header("Forma")]
        [SerializeField] private float _maxEmission = 240f;
        [SerializeField] private Color _color = new Color(1f, 1f, 1f, 0.72f);

        private ParticleSystem _particles;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;

        private void Awake()
        {
            BuildParticles();
        }

        private void LateUpdate()
        {
            if (_visualizer == null || _visualizer.Car == null) return;

            // O emissor fica A FRENTE do carro: as particulas precisam existir no volume
            // por onde a camera AINDA VAI passar, senao nascem ja atras dela.
            Transform car = _visualizer.Car;
            transform.position = car.position + car.forward * 13f + Vector3.up * 1.5f;
            transform.rotation = car.rotation;
        }

        /// <summary>Chamado pelo visualizador todo frame, com a velocidade e o yaw ja amortecido.</summary>
        public void SetState(float speedKmh, float yawDegrees)
        {
            if (_particles == null) return;

            bool straight = Mathf.Abs(yawDegrees) < _yawThreshold;
            float intensity = straight
                ? Mathf.InverseLerp(_minSpeedKmh, _fullSpeedKmh, speedKmh)
                : 0f;

            _emission.rateOverTime = _maxEmission * intensity;
            _main.startColor = new Color(_color.r, _color.g, _color.b, _color.a * intensity);

            bool wanted = intensity > 0.01f;
            if (wanted && !_particles.isPlaying) _particles.Play();
            else if (!wanted && _particles.isPlaying)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void BuildParticles()
        {
            _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();

            _main = _particles.main;
            _main.loop = true;
            _main.playOnAwake = false;

            // Paradas no mundo. Toda a sensacao vem da camera passando por elas.
            _main.simulationSpace = ParticleSystemSimulationSpace.World;
            _main.startSpeed = 0f;
            _main.startLifetime = 1.6f;
            _main.startSize = 0.07f;
            _main.gravityModifier = 0f;
            _main.maxParticles = 700;

            _emission = _particles.emission;
            _emission.rateOverTime = 0f;

            // Largo o bastante para passar FORA da pista de 9 m, que e onde o movimento
            // periferico realmente e lido, e alto o bastante para nao virar sujeira no chao.
            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 4.0f, 22f);

            ParticleSystem.ColorOverLifetimeModule fade = _particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0f;
            renderer.cameraVelocityScale = 0.75f;   // o risco E o deslocamento da camera
            renderer.lengthScale = 2.6f;
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        }
    }
}
