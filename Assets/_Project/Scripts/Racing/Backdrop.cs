// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 19.1, 19.4
// -----------------------------------------------------------------------------

using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// O horizonte: um cilindro sem tampas, texturizado por dentro, ancorado na camera.
    ///
    /// <b>Por que nao um Skybox.</b> O panorama tem a linha do horizonte a 35% da altura,
    /// e nao no meio. Um Skybox/Panoramic mapeia v = 0.5 no nivel do olho, entao a cidade
    /// cairia uns 28 graus ABAIXO do horizonte - exatamente onde o plano do chao cobre
    /// tudo. O resultado seria um ceu vazio e uma textura que nunca aparece. Com o
    /// cilindro a altura da linha do horizonte e um parametro (<see cref="_horizonV"/>),
    /// e ela assenta no y do tracado.
    ///
    /// O cilindro acompanha a camera em XZ mas nao gira com ela: e assim que a silhueta
    /// fica parada quando o carro anda em linha reta e VARRE quando ele faz a curva, que
    /// e a leitura de direcao que a 19.1 pede do horizonte sempre visivel.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class Backdrop : MonoBehaviour
    {
        [SerializeField] private Transform _follow;
        [SerializeField] private string _texture = "Environments/BackGround";

        /// <summary>Raio do cilindro. Fica dentro do far clip da camera de proposito.</summary>
        [SerializeField] private float _radius = 500f;

        [SerializeField] private int _sides = 48;

        /// <summary>
        /// Onde esta a linha do horizonte NA IMAGEM, com 0 na base e 1 no topo. E este
        /// numero que decide a altura do cilindro, porque e ele que tem de cair em y = 0.
        /// </summary>
        [SerializeField] private float _horizonV = 0.346f;

        /// <summary>
        /// Quanto ceu o cilindro cobre acima do horizonte. Precisa passar da metade do
        /// FOV vertical menos a arfagem da camera, senao aparece a cor chapada de fundo
        /// no topo do quadro.
        /// </summary>
        [SerializeField] private float _topAngleDegrees = 38f;

        private Transform _dome;

        private void Start()
        {
            if (_follow == null && Camera.main != null) _follow = Camera.main.transform;

            Texture2D texture = Resources.Load<Texture2D>(_texture);

            // Sem textura nao se monta nada: o clear color chapado da camera ja e um ceu
            // aceitavel, e um cilindro branco de 500 m seria pior que nenhum.
            if (texture == null) return;

            var go = new GameObject("Backdrop");
            go.transform.SetParent(transform, false);
            _dome = go.transform;

            go.AddComponent<MeshFilter>().sharedMesh = BuildMesh();

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.mainTexture = texture;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Place();
        }

        private void LateUpdate()
        {
            Place();
        }

        /// <summary>
        /// Segue a camera no plano, nunca em altura. Travar o Y e o que mantem a linha do
        /// horizonte colada no nivel do tracado quando a camera sobe.
        /// </summary>
        private void Place()
        {
            if (_dome == null || _follow == null) return;

            Vector3 at = _follow.position;
            _dome.position = new Vector3(at.x, 0f, at.z);
        }

        /// <summary>
        /// Anel de quads virado para DENTRO, com v = 0 na base da imagem e v = 1 no topo.
        /// A imagem da uma volta inteira: quando o carro faz um retorno, a mesma silhueta
        /// reaparece, que e o preco de ter um unico panorama.
        /// </summary>
        private Mesh BuildMesh()
        {
            int sides = Mathf.Max(8, _sides);

            float top = _radius * Mathf.Tan(_topAngleDegrees * Mathf.Deg2Rad);
            float height = top / Mathf.Max(0.01f, 1f - _horizonV);
            float bottom = top - height;

            // Uma coluna a mais que os lados: a emenda precisa de u = 0 e u = 1 no MESMO
            // lugar, e um vertice compartilhado so consegue carregar um dos dois.
            int columns = sides + 1;
            var vertices = new Vector3[columns * 2];
            var normals = new Vector3[columns * 2];
            var uvs = new Vector2[columns * 2];

            for (int i = 0; i < columns; i++)
            {
                float u = (float)i / sides;
                float angle = u * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * _radius;
                float z = Mathf.Cos(angle) * _radius;

                vertices[i * 2] = new Vector3(x, bottom, z);
                vertices[i * 2 + 1] = new Vector3(x, top, z);

                Vector3 inward = new Vector3(-x, 0f, -z).normalized;
                normals[i * 2] = inward;
                normals[i * 2 + 1] = inward;

                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
            }

            var triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;

                // Horario visto de dentro - e o que faz a face de frente apontar para o
                // eixo, sem precisar desligar o culling.
                int t = i * 6;
                triangles[t] = t0;
                triangles[t + 1] = t1;
                triangles[t + 2] = b1;
                triangles[t + 3] = t0;
                triangles[t + 4] = b1;
                triangles[t + 5] = b0;
            }

            var mesh = new Mesh { name = "Backdrop" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
