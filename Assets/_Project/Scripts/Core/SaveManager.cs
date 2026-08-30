// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secao 20.5
// -----------------------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;

namespace FramedDrift.Core
{
    /// <summary>
    /// Escrita atomica, 3 backups rotativos, autosave a cada 60 s, migracao por versao.
    ///
    /// A escrita atomica nao e preciosismo: o jogo salva em todo evento significativo e
    /// fica horas aberto sem supervisao. Uma queda de energia no meio de um
    /// <c>File.WriteAllText</c> deixaria o save truncado, e o jogador perderia a garagem
    /// inteira - que e a unica coisa que ele tem.
    /// </summary>
    public static class SaveManager
    {
        public const string FileName = "framed-drift.save.json";
        public const int BackupCount = 3;
        public const float AutosaveIntervalSeconds = 60f;

        public static string SaveDirectory { get { return Application.persistentDataPath; } }
        public static string SavePath { get { return Path.Combine(SaveDirectory, FileName); } }

        private static string BackupPath(int index)
        {
            return Path.Combine(SaveDirectory, FileName + ".bak" + index);
        }

        public static bool Exists()
        {
            return File.Exists(SavePath);
        }

        // --- escrita ---------------------------------------------------------------

        public static void Save(SaveData data)
        {
            if (data == null) return;

            data.Version = SaveData.CurrentVersion;
            data.LastTimestamp = DateTime.UtcNow;

            string json = JsonUtility.ToJson(data, true);
            string temp = SavePath + ".tmp";

            try
            {
                Directory.CreateDirectory(SaveDirectory);
                File.WriteAllText(temp, json);

                RotateBackups();

                // File.Replace e atomico e ja preserva o original como backup 0. Se o save
                // ainda nao existe, Replace lanca - dai o Move no caminho de primeira vez.
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, BackupPath(0));
                else File.Move(temp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Falha ao salvar: " + e.Message);
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private static void RotateBackups()
        {
            try
            {
                for (int i = BackupCount - 1; i > 0; i--)
                {
                    string from = BackupPath(i - 1);
                    string to = BackupPath(i);
                    if (!File.Exists(from)) continue;
                    if (File.Exists(to)) File.Delete(to);
                    File.Move(from, to);
                }
            }
            catch (Exception e)
            {
                // Backup e uma rede de seguranca; falhar em rotacionar nao pode impedir
                // o save em si de acontecer.
                Debug.LogWarning("Nao consegui rotacionar os backups: " + e.Message);
            }
        }

        // --- leitura ----------------------------------------------------------------

        /// <summary>
        /// Le o save; cai para os backups se o principal estiver corrompido. Devolve null
        /// quando nao existe save nenhum - o chamador decide se comeca um jogo novo.
        /// </summary>
        public static SaveData Load()
        {
            SaveData data = TryLoad(SavePath);
            if (data != null) return data;

            for (int i = 0; i < BackupCount; i++)
            {
                data = TryLoad(BackupPath(i));
                if (data == null) continue;

                Debug.LogWarning("Save principal ilegivel; recuperado do backup " + i + ".");
                return data;
            }
            return null;
        }

        private static SaveData TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;

                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;

                Migrate(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save ilegivel em " + path + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Migracao EXPLICITA por versao (GDD 20.5). Cada degrau tem o seu bloco; nada de
        /// "se o campo for zero, adivinhe" - adivinhacao silenciosa e como um save quebra
        /// sem ninguem descobrir por dois patches.
        /// </summary>
        private static void Migrate(SaveData data)
        {
            if (data.Version == SaveData.CurrentVersion) return;

            if (data.Version < 1)
            {
                // Formato pre-1: nada a fazer alem de marcar. O primeiro degrau real de
                // migracao entra aqui quando a versao 2 existir.
                data.Version = 1;
            }

            data.Version = SaveData.CurrentVersion;
        }

        // --- tempo de ausencia --------------------------------------------------------

        /// <summary>
        /// Segundos desde o ultimo save, em UTC.
        ///
        /// Se o relogio do sistema andou para tras, devolve ZERO. Nao pune, nao acusa,
        /// nao mostra aviso de trapaca (GDD 20.5) - o caso comum e fuso horario ou
        /// sincronizacao de NTP, nao fraude.
        /// </summary>
        public static float SecondsSince(SaveData data)
        {
            if (data == null || string.IsNullOrEmpty(data.LastTimestampUtc)) return 0f;

            TimeSpan elapsed = DateTime.UtcNow - data.LastTimestamp;
            double seconds = elapsed.TotalSeconds;
            return seconds <= 0.0 ? 0f : (float)seconds;
        }

        public static void DeleteAll()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                for (int i = 0; i < BackupCount; i++)
                    if (File.Exists(BackupPath(i))) File.Delete(BackupPath(i));
            }
            catch (Exception e)
            {
                Debug.LogError("Falha ao apagar o save: " + e.Message);
            }
        }
    }
}
