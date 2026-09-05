// -----------------------------------------------------------------------------
//  Framed Drift  -  App
//  GDD 0.2  secao 20.1
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Racing;
using FramedDrift.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FramedDrift.Bootstrap
{
    /// <summary>
    /// Monta a cena inteira em tempo de execucao a partir de um unico GameObject.
    ///
    /// Motivo: nenhum prefab autorado existe ainda, e uma cena montada a mao com
    /// referencias serializadas viraria um arquivo YAML gigante que quebra em todo
    /// merge e nao explica nada em code review. Com bootstrap, a cena e uma linha e a
    /// composicao fica em codigo, versionavel e legivel.
    ///
    /// Quando os prefabs de modulo e o modelo do carro existirem (Fase 1+), este arquivo
    /// vira o lugar natural para trocar primitivas por prefabs - a estrutura nao muda.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Cena")]
        [SerializeField] private Color _skyColor = new Color(0.05f, 0.05f, 0.09f);
        [SerializeField] private bool _createCamera = true;

        private Vignette _vignette;
        private MotionBlur _blur;

        private void Awake()
        {
            EnsureGameManager();

            var track = CreateChild<TrackAssembler>("Track");
            var visualizer = CreateChild<RaceVisualizer>("RaceVisualizer");
            var smoke = CreateChild<DriftSmokeSystem>("DriftSmoke");
            var streaks = CreateChild<SpeedStreaks>("SpeedStreaks");
            var perfectEntry = CreateChild<PerfectEntryController>("PerfectEntry");
            var collectibles = CreateChild<CollectibleSpawner>("Collectibles");
            var backdrop = CreateChild<Backdrop>("Backdrop");

            Camera camera = _createCamera ? CreateCamera() : Camera.main;
            CameraRig rig = camera != null ? camera.gameObject.AddComponent<CameraRig>() : null;

            var hudRoot = new GameObject("UI");
            hudRoot.transform.SetParent(transform, false);

            var raceUi = hudRoot.AddComponent<RaceUI>();
            var garage = hudRoot.AddComponent<GarageUI>();
            var map = hudRoot.AddComponent<MapUI>();
            var automation = hudRoot.AddComponent<AutomationUI>();
            var returnScreen = hudRoot.AddComponent<ReturnUI>();
            var rivalUi = hudRoot.AddComponent<RivalUI>();
            var hud = hudRoot.AddComponent<GameHud>();
            var taskbar = hudRoot.AddComponent<TaskbarUI>();
            var windowMode = hudRoot.AddComponent<WindowModeController>();

            taskbar.enabled = false;

            // Fiacao. Feita por reflexao sobre os campos [SerializeField] privados porque
            // o alternativo seria tornar tudo publico so para o bootstrap alcancar - o que
            // abriria a porta para qualquer um religar as referencias em runtime.
            Wire.Set(visualizer, "_track", track);
            Wire.Set(visualizer, "_camera", rig);
            Wire.Set(visualizer, "_smoke", smoke);
            Wire.Set(visualizer, "_streaks", streaks);

            Wire.Set(smoke, "_visualizer", visualizer);
            Wire.Set(streaks, "_visualizer", visualizer);
            Wire.Set(collectibles, "_visualizer", visualizer);
            if (rig != null) Wire.Set(rig, "_visualizer", visualizer);
            if (rig != null && _vignette != null) Wire.Set(rig, "_vignette", _vignette);
            if (rig != null && _blur != null) Wire.Set(rig, "_blur", _blur);
            if (camera != null) Wire.Set(backdrop, "_follow", camera.transform);

            Wire.Set(raceUi, "_perfectEntry", perfectEntry);

            Wire.Set(hud, "_garage", garage);
            Wire.Set(hud, "_map", map);
            Wire.Set(hud, "_automation", automation);
            Wire.Set(hud, "_returnScreen", returnScreen);

            Wire.Set(windowMode, "_fullHud", hud);
            Wire.Set(windowMode, "_taskbar", taskbar);
        }

        private void EnsureGameManager()
        {
            if (GameManager.Instance != null) return;
            gameObject.AddComponent<GameManager>();
        }

        /// <summary>
        /// Pos-processamento. A 19.1 pede estetica PS1/PS2 TRATADA com pos moderno, e o
        /// perfil e montado em codigo pelo mesmo motivo que a cena e: um .asset de Volume
        /// seria mais um arquivo serializado para quebrar em merge sem explicar nada.
        ///
        /// <b>Motion blur so no DRIFT, e nada de FXAA.</b> Os dois borravam o CARRO, que
        /// e o unico objeto da tela cuja silhueta a 19.1 exige legivel a 360x48.
        ///
        /// O motion blur e <c>CameraOnly</c>, que reconstroi velocidade por pixel a partir
        /// do movimento da CAMERA. Como esta camera persegue o carro - seguimento
        /// amortecido que nunca assenta, mais tranco de Perlin, roll e lerp de FOV - ela
        /// tem velocidade propria em TODO frame, inclusive em reta a velocidade constante.
        /// Ligado o tempo inteiro, portanto, ele borrava o carro o tempo inteiro. URP nao
        /// tem como excluir um objeto do blur de camera.
        ///
        /// A saida nao foi apagar o efeito, foi lhe dar um MOMENTO: a intensidade nasce em
        /// zero aqui e quem a abre e a <see cref="Racing.CameraRig"/>, em funcao do angulo
        /// de drift. Fora do drift a intensidade e 0 e a URP pula o passe inteiro, entao o
        /// carro fica nitido de graca; no drift o borrao entra junto com a derrapagem e
        /// vira leitura em vez de sujeira.
        ///
        /// O FXAA e um filtro de BORDA em pos: ele amaciava exatamente o contorno que
        /// carrega a leitura de yaw. O SMAA custa um pouco mais e devolve o contorno.
        ///
        /// A velocidade das RETAS, que o blur permanente vendia, foi para onde nao custa
        /// nitidez do carro: abertura de FOV por velocidade e vinheta fechando
        /// (<see cref="Racing.CameraRig"/>) e riscos perifericos
        /// (<see cref="Racing.SpeedStreaks"/>). Bloom existe para a paleta neon; nada
        /// aqui e realismo.
        /// </summary>
        private void CreatePostProcessing(Camera camera)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Nasce DESLIGADO (intensidade 0 = passe pulado pela URP). Quem o acende e a
            // CameraRig, so enquanto houver angulo de drift.
            _blur = profile.Add<MotionBlur>();
            _blur.mode.Override(MotionBlurMode.CameraOnly);
            _blur.quality.Override(MotionBlurQuality.Medium);
            _blur.intensity.Override(0f);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.6f);

            // Guardado porque a CameraRig ABRE e FECHA esta vinheta com a velocidade: o
            // tunel fechando e o efeito de velocidade que age so nas bordas da tela, que
            // e onde a camera elevada da 19.2 perde a sensacao - e o unico que faz isso
            // sem tocar num pixel do carro.
            _vignette = profile.Add<Vignette>();
            _vignette.intensity.Override(CameraRig.VignetteBase);
            _vignette.smoothness.Override(0.4f);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var go = new GameObject("PostProcessing");
            go.transform.SetParent(transform, false);

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private T CreateChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private Camera CreateCamera()
        {
            Camera existing = Camera.main;
            if (existing != null) return existing;

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _skyColor;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 900f;

            go.AddComponent<AudioListener>();

            CreatePostProcessing(camera);

            // Sem PBR, sem reflexos, sem sombras suaves (GDD 19.1). A linguagem visual e
            // chapada; sombra dinamica aqui so custaria frame sem mudar a leitura.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.85f);
            RenderSettings.fog = false;

            return camera;
        }
    }

    /// <summary>Atribui um campo privado marcado com [SerializeField].</summary>
    internal static class Wire
    {
        public static void Set(object target, string fieldName, object value)
        {
            if (target == null) return;

            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);

            if (field == null)
            {
                Debug.LogWarning("Bootstrap: campo " + fieldName + " nao existe em " + target.GetType().Name);
                return;
            }

            field.SetValue(target, value);
        }
    }
}
