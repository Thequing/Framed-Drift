// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secao 3.6.2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Simulation.Rng;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FramedDrift.Racing
{
    /// <summary>Um coletavel vivo na tela.</summary>
    public sealed class ActiveCollectible
    {
        public CollectibleKind Kind;
        public int SegmentIndex;
        public float SpawnsAt;
        public float ExpiresAt;
        public GameObject Marker;
        public bool Taken;
    }

    /// <summary>
    /// Coletaveis raros: drone de patrocinador, brilho de hot lap, selo de reputacao.
    ///
    /// A referencia e a golden cookie do Cookie Clicker e a engrenagem dourada do Rusty's
    /// Retirement: efemero, opcional, alto valor, baixo esforco - e AUSENTE DA MATEMATICA
    /// OFFLINE.
    ///
    /// Essa ultima parte e a que importa: coletaveis nao existem na agregacao da secao
    /// 17. Eles nao sao "perdidos" na ausencia, eles simplesmente NAO SAO GERADOS. E isso
    /// que mantem a promessa de D-02 ("offline rende 100%") literalmente verdadeira, e
    /// nao apenas tecnicamente verdadeira.
    ///
    /// Por isso este componente vive em Racing, roda so durante a reproducao, e o
    /// simulador nao sabe que ele existe.
    ///
    /// <b>Estado atual: o marcador e um cubo.</b> Drone, brilho e selo entram depois.
    /// </summary>
    public sealed class CollectibleSpawner : MonoBehaviour
    {
        [SerializeField] private RaceVisualizer _visualizer;
        [SerializeField] private float _lifetimeSeconds = 3.5f;
        [SerializeField] private float _hoverHeight = 2.6f;
        [SerializeField] private float _clickRadiusPixels = 90f;

        private readonly List<ActiveCollectible> _active = new List<ActiveCollectible>();
        private Camera _camera;

        public IReadOnlyList<ActiveCollectible> Active { get { return _active; } }

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
            ClearMarkers();

            GameManager game = GameManager.Instance;
            if (game == null) return;

            var balance = game.Content.Balance;

            // Fluxo de EVENTOS, nunca o de execucao (GDD 20.3): sortear coletaveis nao
            // pode deslocar o resultado da corrida. Se compartilhassem o fluxo, uma
            // corrida com drone teria qualidades diferentes de uma sem - e o replay
            // deixaria de bater.
            var rng = new RngStreams(evt.Race.Seed).Events;

            TrySpawn(CollectibleKind.SponsorDrone, balance.CollectibleDroneChance, evt, rng);
            TrySpawn(CollectibleKind.HotLapGlow, balance.CollectibleHotLapChance, evt, rng);
            TrySpawn(CollectibleKind.ReputationStamp, balance.CollectibleRepChance, evt, rng);
        }

        private void TrySpawn(CollectibleKind kind, float chance, RaceStarted evt, DeterministicRng rng)
        {
            if (!rng.Chance(chance)) return;

            var timeline = evt.Result.Timeline;
            if (timeline.Length == 0) return;

            int index = rng.Range(0, timeline.Length);
            float at = timeline[index].TimeOffset;

            var collectible = new ActiveCollectible
            {
                Kind = kind,
                SegmentIndex = index,
                SpawnsAt = at,
                ExpiresAt = at + _lifetimeSeconds,
            };
            _active.Add(collectible);

            EventBus.Publish(new CollectibleSpawned
            {
                Kind = kind,
                SegmentIndex = index,
                ExpiresAt = collectible.ExpiresAt,
            });
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || _visualizer == null || _visualizer.Car == null) return;

            float now = game.PlaybackTime;
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveCollectible c = _active[i];

                if (c.Taken || now > c.ExpiresAt)
                {
                    Despawn(c);
                    _active.RemoveAt(i);
                    continue;
                }

                if (now < c.SpawnsAt) continue;

                EnsureMarker(c);
                PositionMarker(c, now);

                if (clicked && IsUnderCursor(c))
                {
                    c.Taken = true;
                    game.CollectCollectible(c.Kind);
                }
            }
        }

        private void EnsureMarker(ActiveCollectible c)
        {
            if (c.Marker != null) return;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Collectible_" + c.Kind;
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * 1.1f;
            DestroyImmediate(marker.GetComponent<Collider>());

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = ColorFor(c.Kind);
            marker.GetComponent<Renderer>().sharedMaterial = material;

            c.Marker = marker;
        }

        private void PositionMarker(ActiveCollectible c, float now)
        {
            // Fica sobre o carro, nao sobre o segmento: o alvo precisa estar onde o olho
            // ja esta. Um alvo no fim da pista seria um teste de atencao, nao um bonus.
            Transform car = _visualizer.Car;
            float bob = Mathf.Sin(now * 4f) * 0.25f;

            c.Marker.transform.position = car.position + Vector3.up * (_hoverHeight + bob)
                                          + car.forward * 6f;
            c.Marker.transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }

        private bool IsUnderCursor(ActiveCollectible c)
        {
            if (c.Marker == null || Mouse.current == null) return false;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector3 screen = _camera.WorldToScreenPoint(c.Marker.transform.position);
            if (screen.z <= 0f) return false;

            Vector2 cursor = Mouse.current.position.ReadValue();
            return Vector2.Distance(cursor, new Vector2(screen.x, screen.y)) <= _clickRadiusPixels;
        }

        private static Color ColorFor(CollectibleKind kind)
        {
            switch (kind)
            {
                case CollectibleKind.SponsorDrone: return new Color(1f, 0.85f, 0.2f);
                case CollectibleKind.HotLapGlow: return new Color(0.4f, 1f, 0.75f);
                default: return new Color(0.7f, 0.5f, 1f);
            }
        }

        private void Despawn(ActiveCollectible c)
        {
            if (c.Marker != null) Destroy(c.Marker);
            c.Marker = null;
        }

        private void ClearMarkers()
        {
            for (int i = 0; i < _active.Count; i++) Despawn(_active[i]);
            _active.Clear();
        }
    }
}
