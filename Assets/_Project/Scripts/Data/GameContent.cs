// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 20.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Content;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>
    /// Carrega os arquivos de Resources/Content e entrega o
    /// <see cref="ContentDatabase"/> que o simulador consome.
    ///
    /// Uma unica instancia por sessao. A validacao roda no carregamento e LANCA: um id
    /// digitado errado precisa aparecer como frase na inicializacao, nao como
    /// NullReference no meio de uma operacao de oito horas sem supervisao.
    /// </summary>
    public static class GameContent
    {
        public const string ResourceFolder = "Content/";

        private static ContentDatabase _database;

        public static ContentDatabase Database
        {
            get
            {
                if (_database == null) Load();
                return _database;
            }
        }

        public static bool IsLoaded { get { return _database != null; } }

        public static ContentDatabase Load()
        {
            _database = ContentLoader.Load(ReadResource);
            return _database;
        }

        /// <summary>Forca a releitura. O botao "recarregar balanceamento" do editor usa isto.</summary>
        public static ContentDatabase Reload()
        {
            _database = null;
            return Load();
        }

        private static string ReadResource(string name)
        {
            var asset = Resources.Load<TextAsset>(ResourceFolder + name);
            if (asset == null)
                throw new ContentException(
                    "Nao encontrei Resources/" + ResourceFolder + name + ".json. "
                    + "Os arquivos de conteudo vivem em Assets/_Project/Resources/Content/.");

            string text = asset.text;
            Resources.UnloadAsset(asset);
            return text;
        }
    }
}
