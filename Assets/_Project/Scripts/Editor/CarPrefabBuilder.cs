// -----------------------------------------------------------------------------
//  Framed Drift  -  Editor
//  GDD 0.2  secoes 19.1, 20.2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FramedDrift.EditorTools
{
    /// <summary>
    /// Leva um modelo de <c>Art/Cars</c> ate um prefab que o <see cref="Racing.CarView"/>
    /// consegue consumir, e escreve o resultado em <c>Resources/Cars/&lt;id&gt;.prefab</c>.
    ///
    /// O id do carro e a chave: o visualizador carrega por id, sem referencia serializada,
    /// entao o nome do arquivo do modelo continua sendo o nome do MODELO (RX45) e quem
    /// amarra os dois e a tabela aqui embaixo.
    ///
    /// Reimportar com <c>bakeAxisConversion</c> nao e cosmetico: sem isso o FBX vindo do
    /// Blender entra com um no intermediario girado -90 em X, e os eixos locais das rodas
    /// deixam de coincidir com os do carro - o giro e o estercamento do CarView sairiam
    /// no eixo errado.
    /// </summary>
    public static class CarPrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Resources/Cars";
        private const int WheelPrefixLength = 6;   // "Wheel_"

        private struct Entry
        {
            public string Fbx;
            public string CarId;
        }

        private static readonly Entry[] Models =
        {
            new Entry { Fbx = "Assets/_Project/Art/Cars/RX45.fbx", CarId = "kite_130" },
        };

        [MenuItem("Framed Drift/Art/Reconstruir prefabs de carro")]
        public static void RebuildCarPrefabs()
        {
            var report = new StringBuilder();
            var problems = new List<string>();

            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();

            for (int i = 0; i < Models.Length; i++)
            {
                Entry entry = Models[i];

                var importer = AssetImporter.GetAtPath(entry.Fbx) as ModelImporter;
                if (importer == null)
                {
                    problems.Add("modelo nao encontrado: " + entry.Fbx);
                    continue;
                }

                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.bakeAxisConversion = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.None;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.SaveAndReimport();

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Fbx);
                if (model == null)
                {
                    problems.Add("nao consegui carregar " + entry.Fbx);
                    continue;
                }

                string path = PrefabFolder + "/" + entry.CarId + ".prefab";
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = entry.CarId;
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);

                report.AppendLine("  " + entry.CarId + "  <-  " + Path.GetFileName(entry.Fbx));
                Describe(path, report, problems);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (problems.Count > 0)
            {
                for (int i = 0; i < problems.Count; i++) report.AppendLine("  PROBLEMA: " + problems[i]);
                Debug.LogError("[Framed Drift] prefabs de carro:\n" + report);
            }
            else
            {
                Debug.Log("[Framed Drift] prefabs de carro:\n" + report);
            }

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Framed Drift - prefabs de carro", report.ToString(), "OK");
        }

        /// <summary>
        /// Confere as convencoes que o CarView depende, no prefab gravado - e o unico
        /// lugar onde um modelo exportado errado da para pegar antes do Play Mode.
        /// </summary>
        private static void Describe(string prefabPath, StringBuilder report, List<string> problems)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                problems.Add("prefab nao gravado: " + prefabPath);
                return;
            }

            int wheels = 0;
            bool paint = false;
            int triangles = 0;
            float frontZ = 0f;
            float rearZ = 0f;

            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
                if (filters[i].sharedMesh != null) triangles += filters[i].sharedMesh.triangles.Length / 3;

            var children = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.StartsWith("Wheel_")) continue;
                wheels++;

                // A rotacao de repouso da roda pode ser qualquer coisa - o CarView
                // converte -, mas a POSICAO decide para que lado o carro aponta, e o
                // visualizador dirige para +Z. Nariz em -Z e o carro correndo de re.
                if (children[i].name[WheelPrefixLength] == 'F') frontZ += children[i].localPosition.z;
                else rearZ += children[i].localPosition.z;

                report.Append("     ").Append(children[i].name)
                      .Append(" pos ").Append(children[i].localPosition.ToString("F3"))
                      .Append(" repouso ")
                      .Append(Quaternion.Angle(children[i].localRotation, Quaternion.identity).ToString("F1"))
                      .Append(" graus")
                      .AppendLine();
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int k = 0; k < materials.Length; k++)
                    if (materials[k] != null && materials[k].name.EndsWith("_Paint")) paint = true;
            }

            report.Append("     rodas ").Append(wheels)
                  .Append("   triangulos ").Append(triangles)
                  .Append("   material _Paint ").Append(paint ? "sim" : "NAO")
                  .AppendLine();

            if (wheels != 4) problems.Add(prefabPath + ": esperava 4 Wheel_*, achei " + wheels);
            if (!paint) problems.Add(prefabPath + ": nenhum material terminando em _Paint");
            if (wheels == 4 && frontZ <= rearZ)
                problems.Add(prefabPath + ": o carro aponta para -Z (eixo dianteiro em z="
                             + (frontZ * 0.5f).ToString("F3") + ") - correria de re");
        }
    }
}
