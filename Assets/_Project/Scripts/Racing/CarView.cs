// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 19.1, 19.2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// Cola entre um modelo de carro exportado e o <see cref="RaceVisualizer"/>.
    ///
    /// E adicionado em tempo de execucao ao prefab instanciado, nunca gravado nele. Assim
    /// o prefab continua sendo o modelo cru, sem uma referencia serializada sequer - a
    /// mesma razao pela qual a cena e minima e o Bootstrap monta o resto em codigo.
    ///
    /// <b>Convencoes que o modelo precisa respeitar</b> (ver Art/Cars/.gitkeep):
    ///
    /// - as rodas sao filhas chamadas <c>Wheel_FL/FR/RL/RR</c>, cada uma com origem no
    ///   proprio cubo da roda. Sem isso o estercamento nao aparece, e a 19.1 pede
    ///   explicitamente rodas visiveis o suficiente para ele aparecer;
    /// - o material pintavel termina em <c>_Paint</c>. E o unico que a cor do carro tinge.
    ///   Aros e pneus tem material proprio de proposito: se dividissem o material da
    ///   lataria, a variante de pintura de um rival repintaria as rodas junto;
    /// - o carro aponta para +Z, que e a direcao em que o visualizador o dirige.
    ///
    /// O giro e o estercamento sao calculados no espaco do CARRO e convertidos para o
    /// espaco de cada roda. Um FBX pode entrar com nos intermediarios girados conforme o
    /// exportador e o Bake Axis Conversion - de fato as rodas deste modelo entram 90
    /// graus fora -, e amarrar a rotacao ao eixo local da roda giraria no eixo errado.
    /// </summary>
    public sealed class CarView : MonoBehaviour
    {
        private const string PaintSuffix = "_Paint";
        private const string WheelPrefix = "Wheel_";

        /// <summary>
        /// Quanto o volante responde ao angulo de drift. O simulador nao produz um angulo
        /// de estercamento - ele produz YAW -, e um carro derrapando corrige para o lado
        /// contrario. Contraesterco proporcional e a leitura honesta disso, e e o canal
        /// que a 19.1 pede para o drift aparecer nas rodas.
        /// </summary>
        private const float SteerPerYawDegree = 0.6f;
        private const float MaxSteerDegrees = 35f;

        private readonly List<Material> _paint = new List<Material>();
        private readonly List<Material> _owned = new List<Material>();

        private Transform[] _wheels;
        private Quaternion[] _toLocal;
        private Quaternion[] _fromLocal;
        private float[] _wheelRadius;
        private float[] _wheelSpin;
        private bool[] _wheelSteers;

        public bool HasPaint { get { return _paint.Count > 0; } }
        public int WheelCount { get { return _wheels == null ? 0 : _wheels.Length; } }

        /// <summary>
        /// Troca os materiais importados por Unlit e cataloga rodas. Chamado uma vez,
        /// logo depois do Instantiate.
        /// </summary>
        public void Bind()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

            // Nada de PBR, nada de reflexo, nada de sombra suave (GDD 19.1). O importador
            // do FBX cria materiais Lit; aqui eles viram Unlit preservando a cor base.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] source = renderers[r].sharedMaterials;
                var swapped = new Material[source.Length];

                for (int i = 0; i < source.Length; i++)
                {
                    swapped[i] = ToUnlit(source[i], unlit);
                    _owned.Add(swapped[i]);

                    if (source[i] != null && source[i].name.EndsWith(PaintSuffix))
                        _paint.Add(swapped[i]);
                }

                renderers[r].sharedMaterials = swapped;
            }

            var wheels = new List<Transform>();
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name.StartsWith(WheelPrefix)) wheels.Add(children[i]);

            _wheels = wheels.ToArray();
            _toLocal = new Quaternion[_wheels.Length];
            _fromLocal = new Quaternion[_wheels.Length];
            _wheelRadius = new float[_wheels.Length];
            _wheelSpin = new float[_wheels.Length];
            _wheelSteers = new bool[_wheels.Length];

            for (int i = 0; i < _wheels.Length; i++)
            {
                // Orientacao de repouso da roda vista do carro. O par converte uma
                // rotacao pensada no espaco do carro para o localRotation da roda.
                Transform parent = _wheels[i].parent != null ? _wheels[i].parent : transform;
                Quaternion parentInCar = Quaternion.Inverse(transform.rotation) * parent.rotation;

                _toLocal[i] = Quaternion.Inverse(parentInCar);
                _fromLocal[i] = parentInCar * _wheels[i].localRotation;
                _wheelRadius[i] = RadiusOf(_wheels[i]);

                // Wheel_FL / Wheel_FR estercam, Wheel_RL / Wheel_RR nao.
                _wheelSteers[i] = _wheels[i].name.Length > WheelPrefix.Length
                                  && _wheels[i].name[WheelPrefix.Length] == 'F';
            }
        }

        /// <summary>Cor da lataria. So os materiais <c>_Paint</c> respondem.</summary>
        public void SetPaint(Color color)
        {
            for (int i = 0; i < _paint.Count; i++)
            {
                _paint[i].color = color;
                if (_paint[i].HasProperty("_BaseColor")) _paint[i].SetColor("_BaseColor", color);
            }
        }

        /// <summary>
        /// Gira e esterca as rodas. Chamado pelo visualizador todo frame, com a mesma
        /// velocidade e o mesmo yaw ja amortecido que alimentam camera e fumaca.
        /// </summary>
        public void Tick(float speedKmh, float yawDegrees, float deltaTime)
        {
            if (_wheels == null || _wheels.Length == 0) return;

            float speed = speedKmh / 3.6f;
            float steer = Mathf.Clamp(-yawDegrees * SteerPerYawDegree, -MaxSteerDegrees, MaxSteerDegrees);

            for (int i = 0; i < _wheels.Length; i++)
            {
                if (_wheelRadius[i] > 0.01f)
                {
                    float degrees = speed / _wheelRadius[i] * Mathf.Rad2Deg * deltaTime;
                    _wheelSpin[i] = Mathf.Repeat(_wheelSpin[i] + degrees, 360f);
                }

                // No espaco do carro: esterca em torno de Y, gira em torno de X.
                Quaternion delta = Quaternion.AngleAxis(_wheelSpin[i], Vector3.right);
                if (_wheelSteers[i]) delta = Quaternion.AngleAxis(steer, Vector3.up) * delta;

                _wheels[i].localRotation = _toLocal[i] * delta * _fromLocal[i];
            }
        }

        private static Material ToUnlit(Material source, Shader unlit)
        {
            Color color = Color.white;
            if (source != null)
            {
                if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color")) color = source.color;
            }

            var material = new Material(unlit);
            material.name = source != null ? source.name : "Car";
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            return material;
        }

        private static float RadiusOf(Transform wheel)
        {
            var filter = wheel.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return 0f;

            Vector3 extents = filter.sharedMesh.bounds.extents;
            Vector3 scale = wheel.lossyScale;
            return Mathf.Max(extents.y * Mathf.Abs(scale.y), extents.z * Mathf.Abs(scale.z));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _owned.Count; i++)
                if (_owned[i] != null) Destroy(_owned[i]);

            _owned.Clear();
            _paint.Clear();
        }
    }
}
