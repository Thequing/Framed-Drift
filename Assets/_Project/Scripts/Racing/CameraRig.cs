// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 19.2
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Simulation.Balance;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// A camera traseira ELEVADA - a decisao que faz o drift funcionar (GDD 19.2).
    ///
    /// A camera canonica de Top Gear / Horizon Chase e baixa e colada. Aquele
    /// enquadramento e sintonizado para SENSACAO DE VELOCIDADE, nao para leitura de
    /// curva: naquela altura o yaw fica tao encurtado que um carro derrapando aparece
    /// so como um carro mais estreito.
    ///
    /// O alvo e 30-40% mais alta que a linha de base do Horizon Chase, com a arfagem
    /// correspondente apontando um pouco mais para baixo. Isso preserva a estrada
    /// correndo em direcao ao jogador e RECUPERA a leitura do yaw.
    ///
    /// Todos os numeros vem do BalanceConfig e sao calibrados na Fase 1 - e o unico jeito
    /// de descobrir a altura certa e olhando, nao calculando (nota da secao 22.2).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private RaceVisualizer _visualizer;

        private Camera _camera;
        private BalanceSettings _balance;

        // Posicao e rotacao AMORTECIDAS, antes de tranco e roll. Os dois efeitos entram
        // depois destas: se entrassem antes, o proprio amortecimento os comeria e
        // sobraria um tremor morno em vez de um tranco.
        private Vector3 _basePosition;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _hasBase;

        private float _lastTrackHeading;
        private float _turnRate;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (_visualizer == null || _visualizer.Car == null) return;
            if (!EnsureBalance()) return;

            Transform car = _visualizer.Car;
            float speed = _visualizer.CurrentSpeedKmh;
            float yaw = _visualizer.CurrentYaw;
            float corner = _visualizer.CurrentCorner;

            // posicao alvo = carro.pos + offset base + offset de velocidade + offset de drift
            float height = _balance.CameraBaseHeight * _balance.CameraHeightMultiplier;
            float distance = _balance.CameraBaseDistance + speed * _balance.CameraSpeedPullback;
            float lateral = -yaw * _balance.CameraDriftLateralOffset;

            // A camera segue a direcao da PISTA, nao a do carro. Se seguisse o carro, o
            // yaw ficaria sempre centralizado - e o angulo de drift desapareceria
            // exatamente na imagem que existe para mostra-lo.
            float trackHeading = car.eulerAngles.y - yaw;
            Quaternion frame = Quaternion.Euler(0f, trackHeading, 0f);

            Vector3 target = car.position + frame * new Vector3(lateral, height, -distance);

            float dt = Time.deltaTime;

            // No primeiro frame nao ha o que amortecer - amortecer contra lixo daria
            // alguns frames de camera vindo do infinito.
            float damping = _hasBase ? 1f - Mathf.Exp(-_balance.CameraDamping * dt) : 1f;
            _hasBase = true;

            _basePosition = Vector3.Lerp(_basePosition, target, damping);

            // A camera ATRASA de proposito em relacao ao carro: e o atraso que vende o
            // angulo (GDD 19.2).
            Vector3 lookAt = car.position + Vector3.up * 0.6f;
            Quaternion desired = Quaternion.LookRotation(lookAt - _basePosition, Vector3.up);
            desired *= Quaternion.Euler(_balance.CameraPitchDegrees * 0.25f, 0f, 0f);
            _baseRotation = Quaternion.Slerp(_baseRotation, desired, damping);

            // --- roll na curva ---------------------------------------------------------
            // A taxa de giro da PISTA em graus por segundo e curvatura x velocidade, que e
            // exatamente o que o corpo sente numa curva - e sai de graca do heading que ja
            // esta aqui, sem precisar consultar o TrackPath. O sinal inclina PARA DENTRO
            // da curva; trocar o sinal de cameraRollPerTurnRate inclina para fora.
            float turn = dt > 0.0001f ? Mathf.DeltaAngle(_lastTrackHeading, trackHeading) / dt : 0f;
            _lastTrackHeading = trackHeading;
            _turnRate = Mathf.Lerp(_turnRate, turn, 1f - Mathf.Exp(-8f * dt));

            float roll = Mathf.Clamp(-_turnRate * _balance.CameraRollPerTurnRate,
                                     -_balance.CameraRollMax, _balance.CameraRollMax);

            // --- tranco na curva -------------------------------------------------------
            // So treme em curva, e la a amplitude ainda cresce com a velocidade. Em reta
            // quem vende velocidade sao os riscos e o motion blur, nao o tranco.
            // Perlin em vez de Random: ruido continuo tremula, ruido branco pisca.
            float amplitude = Mathf.Min(speed * _balance.CameraShakePerSpeed, _balance.CameraShakeMax)
                              * corner;
            Vector3 shake = Vector3.zero;
            if (amplitude > 0.0001f)
            {
                float t = Time.time * 22f;
                shake = new Vector3(Mathf.PerlinNoise(t, 0.37f) - 0.5f,
                                    Mathf.PerlinNoise(0.71f, t) - 0.5f,
                                    0f) * (2f * amplitude);
            }

            transform.rotation = _baseRotation * Quaternion.Euler(0f, 0f, roll);
            transform.position = _basePosition + transform.rotation * shake;

            // A abertura de FOV tambem e um efeito de CURVA. Presa ao sinal amortecido
            // de curva, e nao ao yaw, ela nao pulsa a cada fronteira de segmento numa
            // sequencia de curvas - abre uma vez e fica aberta ate a sequencia acabar.
            float boost = speed * _balance.CameraFovPerSpeed
                          + Mathf.Abs(yaw) * _balance.CameraFovPerAngle;

            float fov = _balance.CameraFovBase + boost * corner;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fov, damping);
        }

        /// <summary>Corta para a posicao final sem amortecer. Usado na largada.</summary>
        public void Snap()
        {
            if (_visualizer == null || _visualizer.Car == null || !EnsureBalance()) return;

            Transform car = _visualizer.Car;
            float height = _balance.CameraBaseHeight * _balance.CameraHeightMultiplier;

            transform.position = car.position
                                 + Quaternion.Euler(0f, car.eulerAngles.y, 0f)
                                 * new Vector3(0f, height, -_balance.CameraBaseDistance);
            transform.LookAt(car.position + Vector3.up * 0.6f);

            _basePosition = transform.position;
            _baseRotation = transform.rotation;
            _hasBase = true;
            _lastTrackHeading = car.eulerAngles.y;
            _turnRate = 0f;
        }

        private bool EnsureBalance()
        {
            if (_balance != null) return true;

            GameManager game = GameManager.Instance;
            if (game == null || game.Content == null) return false;

            _balance = game.Content.Balance;
            return true;
        }
    }
}
