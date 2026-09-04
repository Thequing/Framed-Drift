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

        [Header("Paleta chapada (GDD 19.1) - vale quando a textura nao carrega")]
        [SerializeField] private Color _asphalt = new Color(0.16f, 0.16f, 0.20f);
        [SerializeField] private Color _curveTint = new Color(0.22f, 0.20f, 0.30f);
        [SerializeField] private Color _wallColor = new Color(0.85f, 0.25f, 0.45f);
        [SerializeField] private Color _groundColor = new Color(0.07f, 0.07f, 0.10f);

        [Header("Texturas (caminhos dentro de Resources/)")]
        [SerializeField] private string _asphaltTexture = "Ground/Asphalt";
        [SerializeField] private string _groundTexture = "Ground/GroudPlane";
        [SerializeField] private string _signTexture = "Environments/Sign";

        [Header("Repeticao das texturas")]
        [SerializeField] private int _asphaltTilesPerSlice = 1;
        [SerializeField] private int _signsPerSlice = 3;
        [SerializeField] private bool _signsPointForward = true;
        [SerializeField] private float _groundMetersPerTile = 8f;

        /// <summary>Escala do Plane do chao. 400 = 4 km de lado, folga de sobra sobre o
        /// ponto mais distante da origem que uma pista do MVP alcanca (~870 m).</summary>
        [SerializeField] private float _groundScale = 400f;

        /// <summary>
        /// Tinte da curva quando o asfalto tem textura. O <see cref="_curveTint"/> chapado
        /// nao serve aqui: BaseColor MULTIPLICA o mapa, e uma cor escura como aquela
        /// apagaria a textura inteira em vez de tingi-la.
        /// </summary>
        [SerializeField] private Color _curveTexTint = new Color(0.72f, 0.62f, 1f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Material _sharedRoad;
        private Material _sharedCurve;
        private Material _sharedSignLeft;
        private Material _sharedSignRight;
        private Material _sharedGround;

        public TrackPath Path { get; private set; }

        /// <summary>
        /// Altura do topo do asfalto acima do tracado. O trecho e um cubo centrado no
        /// ponto do <see cref="TrackPath"/>, entao a superficie fica meia espessura acima
        /// - e e nela que um modelo com pivo no chao assenta.
        /// </summary>
        public float SurfaceOffset { get { return _roadThickness * 0.5f; } }

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
                BuildWall(root.transform, position, heading, sliceLength, -half, _sharedSignLeft);
                if (segment.Walls == WallConfig.BothSides)
                    BuildWall(root.transform, position, heading, sliceLength, half, _sharedSignRight);
            }
        }

        private void BuildWall(Transform parent, Vector3 center, float heading, float length,
                               float offset, Material material)
        {
            Quaternion rotation = Quaternion.Euler(0f, heading, 0f);
            Vector3 position = center + rotation * new Vector3(offset, _wallHeight * 0.5f, 0f);

            GameObject wall = Primitive(parent, "wall", position, heading);
            wall.transform.localScale = new Vector3(_wallThickness, _wallHeight, length * 1.05f);
            Paint(wall, material);
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
            ground.transform.localScale = Vector3.one * _groundScale;
            DestroyImmediate(ground.GetComponent<Collider>());

            ground.GetComponent<Renderer>().sharedMaterial = _sharedGround;

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
        /// Cinco materiais compartilhados para a pista inteira.
        ///
        /// Um material por cubo geraria centenas de draw calls e estouraria o orcamento
        /// de 60 fps da 20.4 antes de existir um unico modelo de verdade. As placas
        /// gastam DOIS materiais por um motivo geometrico, nao estetico - ver
        /// <see cref="EnsureSignMaterials"/>.
        /// </summary>
        private void EnsureMaterials()
        {
            if (_sharedRoad != null) return;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Texture2D asphalt = Resources.Load<Texture2D>(_asphaltTexture);

            _sharedRoad = Surface(unlit, asphalt, _asphalt, Color.white);
            _sharedCurve = Surface(unlit, asphalt, _curveTint, _curveTexTint);

            // A textura de asfalto ja e a secao TRANSVERSAL inteira: as duas faixas
            // continuas caem nas bordas da pista com U indo de 0 a 1 uma unica vez.
            // So V, que corre no sentido da fatia, repete.
            if (asphalt != null)
            {
                var tiling = new Vector2(1f, Mathf.Max(1, _asphaltTilesPerSlice));
                _sharedRoad.mainTextureScale = tiling;
                _sharedCurve.mainTextureScale = tiling;
            }

            EnsureSignMaterials(unlit);

            _sharedGround = Surface(unlit, Resources.Load<Texture2D>(_groundTexture),
                                    _groundColor, Color.white);

            // O Plane nativo tem 10 unidades de lado e UV 0..1 na malha inteira. Sem
            // reescalar, um ladrilho de grama cobriria os 4 km do chao e sobraria um
            // pixel a cada quatro metros.
            if (_sharedGround.mainTexture != null && _groundMetersPerTile > 0.01f)
            {
                float tiles = 10f * _groundScale / _groundMetersPerTile;
                _sharedGround.mainTextureScale = new Vector2(tiles, tiles);
            }
        }

        /// <summary>
        /// As placas dos dois lados saem da MESMA imagem, girada 180 graus de um lado.
        ///
        /// Nao e escolha de arte, e a geometria do cubo: a face que o piloto ve na parede
        /// da esquerda e a +X, e na da direita e a -X. As duas sao faces OPOSTAS, e o UV
        /// do cubo nativo corre em sentidos contrarios nelas - o mesmo material deixaria a
        /// seta apontando para tras de um lado. Espelhar U num dos materiais desfaz isso,
        /// e como a seta e simetrica na vertical, espelhar em U e o mesmo que girar 180.
        /// </summary>
        private void EnsureSignMaterials(Shader unlit)
        {
            Texture2D sign = Resources.Load<Texture2D>(_signTexture);

            _sharedSignLeft = Surface(unlit, sign, _wallColor, Color.white);
            _sharedSignRight = Surface(unlit, sign, _wallColor, Color.white);

            if (sign == null) return;

            // A face visivel tem o comprimento da fatia por _wallHeight de altura. Repetir
            // um numero INTEIRO de vezes mantem a placa quase quadrada e faz o padrao
            // recomecar exatamente na emenda entre duas fatias.
            float tiles = Mathf.Max(1, _signsPerSlice);
            float leftU = _signsPointForward ? -tiles : tiles;

            _sharedSignLeft.mainTextureScale = new Vector2(leftU, 1f);
            _sharedSignRight.mainTextureScale = new Vector2(-leftU, 1f);
        }

        /// <summary>
        /// Um material chapado, com ou sem textura.
        ///
        /// Com textura o BaseColor vira o TINTE dela e o normal e branco; sem textura ele
        /// volta a ser a cor solida da paleta. Os dois casos convivem de proposito - um
        /// PNG que nao carregou tem de deixar a pista legivel em vez de preta.
        /// </summary>
        private static Material Surface(Shader shader, Texture2D texture, Color flat, Color tint)
        {
            var material = new Material(shader);

            if (texture != null)
            {
                material.mainTexture = texture;
                material.color = tint;
            }
            else material.color = flat;

            return material;
        }
    }
}
