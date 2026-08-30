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

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_visualizer == null || _visualizer.Car == null) return;
            if (!EnsureBalance()) return;

            Transform car = _visualizer.Car;
            float speed = _visualizer.CurrentSpeedKmh;
            float yaw = _visualizer.CurrentYaw;

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

            float damping = 1f - Mathf.Exp(-_balance.CameraDamping * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, damping);

            // A camera ATRASA de proposito em relacao ao carro: e o atraso que vende o
            // angulo (GDD 19.2).
            Vector3 lookAt = car.position + Vector3.up * 0.6f;
            Quaternion desired = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
            desired *= Quaternion.Euler(_balance.CameraPitchDegrees * 0.25f, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, damping);

            float fov = _balance.CameraFovBase
                        + speed * _balance.CameraFovPerSpeed
                        + Mathf.Abs(yaw) * _balance.CameraFovPerAngle;
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
