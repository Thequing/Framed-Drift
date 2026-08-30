// -----------------------------------------------------------------------------
//  Framed Drift  -  Editor
//  GDD 0.2  secoes 11.3, 20.2, 20.5
// -----------------------------------------------------------------------------

using System.IO;
using System.Text;
using FramedDrift.Core;
using FramedDrift.Data;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FramedDrift.EditorTools
{
    /// <summary>Utilitarios de projeto no menu do editor.</summary>
    public static class ProjectMenu
    {
        public const string ScenePath = "Assets/_Project/Scenes/FramedDrift.unity";

        /// <summary>
        /// Valida o conteudo sem entrar em Play Mode.
        ///
        /// Um id de modulo digitado errado precisa aparecer como FRASE, na hora, e nao
        /// como NullReference no meio de uma operacao de oito horas sem supervisao.
        /// </summary>
        [MenuItem("Framed Drift/Validar conteudo")]
        public static void ValidateContent()
        {
            try
            {
                ContentDatabase db = GameContent.Reload();

                var sb = new StringBuilder();
                sb.AppendLine("[Framed Drift] conteudo valido.");
                sb.Append("  carros ").Append(db.CarList.Count)
                  .Append("   pecas ").Append(db.PartList.Count)
                  .Append("   afixos ").Append(db.AffixList.Count)
                  .Append("   passivas ").Append(db.PassiveList.Count)
                  .Append("   modulos ").Append(db.Modules.Count)
                  .Append("   pistas ").Append(db.TrackList.Count)
                  .Append("   regioes ").Append(db.RegionList.Count)
                  .AppendLine();

                Debug.Log(sb.ToString());
                EditorUtility.DisplayDialog("Framed Drift", "Conteudo valido.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Framed Drift] " + e.Message);
                EditorUtility.DisplayDialog("Framed Drift - conteudo invalido", e.Message, "OK");
            }
        }

        /// <summary>
        /// Gera 200 pistas e verifica que nenhuma e invalida.
        /// E o criterio de saida da Fase 12 (GDD 22.2), disponivel desde ja.
        /// </summary>
        [MenuItem("Framed Drift/Testar gerador de pistas")]
        public static void TestGenerator()
        {
            ContentDatabase db = GameContent.Reload();
            var generator = new TrackGenerator(db);
            var skeletons = (TrackSkeleton[])System.Enum.GetValues(typeof(TrackSkeleton));

            int invalid = 0;
            var problems = new StringBuilder();

            for (int i = 0; i < 200; i++)
            {
                var request = new TrackGenerator.Request
                {
                    RegionId = db.RegionList[0].Id,
                    Tier = TierRank.D,
                    SegmentCount = 18 + (i % 13),
                    Skeleton = skeletons[i % skeletons.Length],
                    Seed = (ulong)i * 6364136223846793005UL + 1442695040888963407UL,
                };

                TrackDef track = generator.Generate(request);
                string problem = Validate(db, track);
                if (problem == null) continue;

                invalid++;
                if (invalid <= 5) problems.AppendLine("  " + problem);
            }

            string message = invalid == 0
                ? "200 pistas geradas, nenhuma invalida."
                : invalid + " de 200 invalidas:\n" + problems;

            Debug.Log("[Framed Drift] " + message);
            EditorUtility.DisplayDialog("Framed Drift - gerador", message, "OK");
        }

        private static string Validate(ContentDatabase db, TrackDef track)
        {
            if (track.ModuleIds.Length < 12) return track.Id + ": so " + track.ModuleIds.Length + " segmentos";
            if (track.BaseTimeSeconds <= 0f) return track.Id + ": tempo de referencia nao positivo";

            for (int i = 1; i < track.ModuleIds.Length; i++)
            {
                ModuleDef previous = db.Module(track.ModuleIds[i - 1]);
                if (previous.AllowedNext.Length == 0) continue;

                bool ok = false;
                for (int k = 0; k < previous.AllowedNext.Length; k++)
                    if (previous.AllowedNext[k] == track.ModuleIds[i]) ok = true;

                if (!ok) return track.Id + ": " + track.ModuleIds[i] + " apos " + previous.Id;
            }
            return null;
        }

        /// <summary>
        /// Recria a cena jogavel.
        ///
        /// A cena e deliberadamente MINIMA: um GameObject com <c>Bootstrap</c>, que monta
        /// o resto em codigo. Uma cena com dezenas de referencias serializadas viraria um
        /// YAML que quebra em todo merge e nao explica nada em review.
        /// </summary>
        [MenuItem("Framed Drift/Recriar cena jogavel")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("FramedDrift");
            root.AddComponent<Bootstrap.Bootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = buildScenes;

            Debug.Log("[Framed Drift] cena criada em " + ScenePath);
        }

        [MenuItem("Framed Drift/Save/Abrir pasta do save")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(SaveManager.SaveDirectory);
        }

        /// <summary>
        /// Apaga o save. Pede confirmacao: e a unica acao verdadeiramente irreversivel do
        /// projeto, e a regra da 18.5 sobre confirmacao existe exatamente para estes casos.
        /// </summary>
        [MenuItem("Framed Drift/Save/Apagar save")]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apagar save",
                    "Isto apaga o save e os 3 backups. Nao ha como desfazer.",
                    "Apagar", "Cancelar")) return;

            SaveManager.DeleteAll();
            Debug.Log("[Framed Drift] save apagado.");
        }
    }
}
