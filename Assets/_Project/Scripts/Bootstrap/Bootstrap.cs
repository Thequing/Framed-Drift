// -----------------------------------------------------------------------------
//  Framed Drift  -  App
//  GDD 0.2  secao 20.1
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Racing;
using FramedDrift.UI;
using UnityEngine;
using UnityEngine.Rendering;

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

        private void Awake()
        {
            EnsureGameManager();

            var track = CreateChild<TrackAssembler>("Track");
            var visualizer = CreateChild<RaceVisualizer>("RaceVisualizer");
            var smoke = CreateChild<DriftSmokeSystem>("DriftSmoke");
            var perfectEntry = CreateChild<PerfectEntryController>("PerfectEntry");
            var collectibles = CreateChild<CollectibleSpawner>("Collectibles");

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

            Wire.Set(smoke, "_visualizer", visualizer);
            Wire.Set(collectibles, "_visualizer", visualizer);
            if (rig != null) Wire.Set(rig, "_visualizer", visualizer);

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
