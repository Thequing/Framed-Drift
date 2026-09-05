// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 19.2
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Simulation.Balance;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

        /// <summary>
        /// A vinheta do perfil montado pelo Bootstrap. Opcional de proposito: sem
        /// pos-processamento a camera continua funcionando, so sem o tunel.
        /// </summary>
        [SerializeField] private Vignette _vignette;

        /// <summary>
        /// O motion blur do perfil do Bootstrap, que nasce em intensidade 0. Opcional
        /// pelo mesmo motivo que a vinheta.
        /// </summary>
        [SerializeField] private MotionBlur _blur;

        /// <summary>Vinheta em repouso e no talo. Fechar a moldura e efeito de VELOCIDADE.</summary>
        public const float VignetteBase = 0.26f;
        private const float VignetteFull = 0.42f;

        /// <summary>Velocidade em que a vinheta satura, em km/h.</summary>
        private const float VignetteFullSpeedKmh = 190f;

        /// <summary>
        /// Faixa de angulo de drift, em graus, que abre o motion blur. O piso fica logo
        /// acima do limiar de "esta em curva" dos riscos de velocidade (6 graus), entao os
        /// dois canais trocam de turno em vez de se sobrepor: os riscos saem, o blur entra.
        /// </summary>
        private const float BlurYawMin = 7f;
        private const float BlurYawFull = 26f;

        /// <summary>Intensidade no angulo maximo. Acima disto o carro deixa de ser silhueta.</summary>
        private const float BlurMax = 0.5f;

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

            // --- abertura de FOV -------------------------------------------------------
            // Duas parcelas, e a separacao e o ponto: a de VELOCIDADE vale sempre, a de
            // ANGULO so na curva.
            //
            // Antes o boost inteiro era multiplicado por corner, entao em reta o FOV era
            // constante - e reta e exatamente onde a camera elevada da 19.2 mais perde
            // sensacao de velocidade. Com a parcela de velocidade solta, acelerar numa
            // reta ABRE a lente, que e o que substituiu o motion blur que borrava o carro
            // (ver Bootstrap.CreatePostProcessing).
            //
            // A parcela de angulo continua presa ao sinal amortecido de curva, e nao ao
            // yaw cru, para nao pulsar a cada fronteira de segmento numa sequencia.
            float fov = _balance.CameraFovBase
                        + speed * _balance.CameraFovPerSpeed
                        + Mathf.Abs(yaw) * _balance.CameraFovPerAngle * corner;

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fov, damping);

            // A moldura fecha com a velocidade. Age so nas BORDAS da tela, entao compra
            // sensacao de velocidade sem tocar num pixel do carro.
            if (_vignette != null)
            {
                float t = Mathf.InverseLerp(0f, VignetteFullSpeedKmh, speed);
                _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value,
                                                       Mathf.Lerp(VignetteBase, VignetteFull, t),
                                                       damping);
            }

            // O motion blur so existe no DRIFT, e cresce com o angulo.
            //
            // Ele e CameraOnly e portanto borra a tela inteira, carro incluso - por isso
            // nao pode ficar ligado o tempo todo (ver Bootstrap.CreatePostProcessing). Mas
            // e exatamente no drift que borrar o carro AJUDA: a derrapagem e o momento em
            // que a leitura pedida pela 19.3 e o movimento, nao o contorno parado.
            //
            // Preso ao yaw amortecido, e nao ao sinal de curva: o que borra e a
            // DERRAPAGEM, e uma curva feita sem angulo nao tem por que borrar nada.
            // Amortecido junto com a camera para o efeito entrar e sair sem estalo.
            if (_blur != null)
            {
                float t = Mathf.InverseLerp(BlurYawMin, BlurYawFull, Mathf.Abs(yaw));
                float wanted = Mathf.Lerp(_blur.intensity.value, BlurMax * t, damping);

                // A cauda do amortecimento e assintotica: sem este corte a intensidade
                // pararia em algo como 0.0004 e nunca em 0, e a URP considera o efeito
                // ATIVO com qualquer valor acima de zero. Ficariam um passe de blur por
                // frame e um veu permanente sobre o carro - exatamente o que saiu daqui.
                _blur.intensity.value = wanted < 0.002f ? 0f : wanted;
            }
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
