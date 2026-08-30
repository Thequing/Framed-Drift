// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 11.2, 19.1, D-05
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// Monta a pista visivel a partir dos segmentos.
    ///
    /// <b>Estado atual: geometria de PLACEHOLDER.</b> Cada trecho e um cubo achatado e
    /// cada parede e um cubo fino. Os prefabs autorais dos modulos (GDD 11.2) entram
    /// depois e substituem <see cref="BuildPlaceholderSegment"/> - o resto desta classe,
    /// incluindo o <see cref="TrackPath"/>, continua valendo.
    ///
    /// D-05 vale mesmo com placeholder: a pista e uma sequencia de MODULOS, e a geometria
    /// sai do que o simulador ja decidiu. Nenhum ruido gera nada aqui.
    /// </summary>
    public sealed class TrackAssembler : MonoBehaviour
    {
        [Header("Placeholder (substituir por prefabs de modulo)")]
        [SerializeField] private float _roadWidth = 9f;
        [SerializeField] private float _roadThickness = 0.25f;
        [SerializeField] private float _wallHeight = 1.4f;
        [SerializeField] private float _wallThickness = 0.5f;

        [Header("Paleta chapada (GDD 19.1)")]
        [SerializeField] private Color _asphalt = new Color(0.16f, 0.16f, 0.20f);
        [SerializeField] private Color _curveTint = new Color(0.22f, 0.20f, 0.30f);
        [SerializeField] private Color _wallColor = new Color(0.85f, 0.25f, 0.45f);
        [SerializeField] private Color _groundColor = new Color(0.07f, 0.07f, 0.10f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Material _sharedRoad;
        private Material _sharedCurve;
        private Material _sharedWall;

        public TrackPath Path { get; private set; }

        public void Assemble(Segment[] segments)
        {
            Clear();
            Path = TrackPath.Build(segments);

            EnsureMaterials();
            BuildGround();

            float distance = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                BuildPlaceholderSegment(segments[i], distance, i);
                distance += segments[i].LengthM;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();
        }

        /// <summary>
        /// Um trecho de pista, feito de cubos.
        ///
        /// Deliberadamente burro: subdivide o segmento em fatias curtas e coloca um cubo
        /// achatado em cada, orientado pela heading da linha de centro. Numa curva isso
        /// faceta, e tudo bem - o modelo autoral resolve isso, e o objetivo aqui e ter uma
        /// pista legivel enquanto ele nao existe.
        /// </summary>
        private void BuildPlaceholderSegment(Segment segment, float startDistance, int index)
        {
            var root = new GameObject("Seg" + index.ToString("00") + "_" + segment.Type);
            root.transform.SetParent(transform, false);
            _spawned.Add(root);

            int slices = Mathf.Max(1, Mathf.CeilToInt(segment.LengthM / TrackPath.SampleSpacing));
            float sliceLength = segment.LengthM / slices;

            for (int s = 0; s < slices; s++)
            {
                float at = startDistance + sliceLength * (s + 0.5f);
                Vector3 position = Path.PositionAt(at);
                float heading = Path.HeadingDegreesAt(at);

                GameObject road = Primitive(root.transform, "road", position, heading);
                road.transform.localScale = new Vector3(_roadWidth, _roadThickness, sliceLength * 1.05f);
                Paint(road, segment.IsCurve ? _sharedCurve : _sharedRoad);

                if (segment.Walls == WallConfig.None) continue;

                float half = (_roadWidth + _wallThickness) * 0.5f;
                BuildWall(root.transform, position, heading, sliceLength, -half);
                if (segment.Walls == WallConfig.BothSides)
                    BuildWall(root.transform, position, heading, sliceLength, half);
            }
        }

        private void BuildWall(Transform parent, Vector3 center, float heading, float length, float offset)
        {
            Quaternion rotation = Quaternion.Euler(0f, heading, 0f);
            Vector3 position = center + rotation * new Vector3(offset, _wallHeight * 0.5f, 0f);

            GameObject wall = Primitive(parent, "wall", position, heading);
            wall.transform.localScale = new Vector3(_wallThickness, _wallHeight, length * 1.05f);
            Paint(wall, _sharedWall);
        }

        /// <summary>
        /// O chao: um plano enorme para o horizonte existir.
        ///
        /// O horizonte e requisito, nao decoracao - a pista ocupa a maior parte da tela e
        /// o horizonte fica SEMPRE visivel (GDD 19.1). Sem ele a camera elevada da 19.2
        /// olha para o vazio.
        /// </summary>
        private void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            ground.transform.localScale = Vector3.one * 400f;
            DestroyImmediate(ground.GetComponent<Collider>());

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _groundColor;
            ground.GetComponent<Renderer>().sharedMaterial = material;

            _spawned.Add(ground);
        }

        private static GameObject Primitive(Transform parent, string name, Vector3 position, float heading)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, heading, 0f);

            // Sem colisores: nada nesta cena colide. A fisica ja aconteceu (D-01), e um
            // Collider por fatia seriam centenas de corpos inuteis no orcamento da 20.4.
            DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Paint(GameObject go, Material material)
        {
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>
        /// Tres materiais compartilhados para a pista inteira.
        ///
        /// Um material por cubo geraria centenas de draw calls e estouraria o orcamento
        /// de 60 fps da 20.4 antes de existir um unico modelo de verdade.
        /// </summary>
        private void EnsureMaterials()
        {
            if (_sharedRoad != null) return;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _sharedRoad = new Material(unlit) { color = _asphalt };
            _sharedCurve = new Material(unlit) { color = _curveTint };
            _sharedWall = new Material(unlit) { color = _wallColor };
        }
    }
}
