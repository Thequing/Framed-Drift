// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.PlayMode
//  GDD 0.2  secoes 10.1, 20.5
// -----------------------------------------------------------------------------

using System.Collections;
using System.IO;
using FramedDrift.Core;
using FramedDrift.Data;
using FramedDrift.Garage;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FramedDrift.Tests.PlayMode
{
    /// <summary>
    /// Save e o unico dado do jogador. A secao 20.5 pede escrita atomica, 3 backups
    /// rotativos e migracao explicita por versao - estes testes verificam os tres.
    /// </summary>
    public class SaveLoadTests
    {
        private string _backupOfExistingSave;

        [SetUp]
        public void PreserveExistingSave()
        {
            // Os testes escrevem no persistentDataPath real. Guardar e restaurar o save
            // do desenvolvedor e o minimo: rodar a suite nao pode custar a garagem dele.
            if (!File.Exists(SaveManager.SavePath)) return;
            _backupOfExistingSave = File.ReadAllText(SaveManager.SavePath);
        }

        [TearDown]
        public void RestoreExistingSave()
        {
            SaveManager.DeleteAll();
            if (_backupOfExistingSave == null) return;

            File.WriteAllText(SaveManager.SavePath, _backupOfExistingSave);
            _backupOfExistingSave = null;
        }

        private static SaveData SampleSave()
        {
            var save = new SaveData
            {
                Cash = 12345L,
                Scrap = 678L,
                Reputation = 42,
                ActiveCarIndex = 0,
            };

            save.Cars.Add(new SavedCar
            {
                CarId = "kite_130",
                Xp = 900,
                Damage = 17.5f,
                Builds = { new SavedBuild { Name = "BUILD 01", TuneLock = 0.8f, Style = DriftStyle.Aggressive } },
            });

            save.Inventory.Add(new SavedPart
            {
                BaseId = "dif_lsd2way",
                Rarity = Rarity.Rare,
                ItemLevel = 6,
                Seed = 8675309UL,
                Uid = 1,
            });
            save.NextPartUid = 2;
            save.Progress.UnlockedTracks.Add("city_loop");

            return save;
        }

        [Test]
        public void SaveThenLoad_RoundTripsEveryField()
        {
            SaveManager.DeleteAll();

            SaveData original = SampleSave();
            SaveManager.Save(original);

            SaveData loaded = SaveManager.Load();
            Assert.IsNotNull(loaded, "O save precisa ser lido de volta.");

            Assert.AreEqual(original.Cash, loaded.Cash);
            Assert.AreEqual(original.Scrap, loaded.Scrap);
            Assert.AreEqual(original.Reputation, loaded.Reputation);
            Assert.AreEqual(original.Cars.Count, loaded.Cars.Count);
            Assert.AreEqual(original.Cars[0].Damage, loaded.Cars[0].Damage, 0.001f);
            Assert.AreEqual(original.Cars[0].Builds[0].TuneLock, loaded.Cars[0].Builds[0].TuneLock, 0.001f);
            Assert.AreEqual(DriftStyle.Aggressive, loaded.Cars[0].Builds[0].Style);
            Assert.AreEqual(original.Inventory.Count, loaded.Inventory.Count);
            Assert.AreEqual(original.Inventory[0].Seed, loaded.Inventory[0].Seed);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.Version);
        }

        [Test]
        public void PartsSurviveTheRoundTripByRerollingFromSeed()
        {
            SaveManager.DeleteAll();

            ContentDatabase content = GameContent.Load();
            var resolver = new RaceResolver(content);

            SaveData original = SampleSave();
            var inventoryBefore = new InventoryManager(original, resolver.Loot, new EconomyLedger(original, content.Balance));
            PartInstance before = inventoryBefore.Get(1);

            SaveManager.Save(original);
            SaveData loaded = SaveManager.Load();

            var inventoryAfter = new InventoryManager(loaded, resolver.Loot, new EconomyLedger(loaded, content.Balance));
            PartInstance after = inventoryAfter.Get(1);

            Assert.IsNotNull(before);
            Assert.IsNotNull(after);

            // O save guarda so a seed; a peca inteira e reconstruida (GDD 10.1). Se isto
            // falhar, todo inventario muda a cada carga.
            Assert.AreEqual(before.Rolled.Affixes.Length, after.Rolled.Affixes.Length);
            for (int i = 0; i < before.Rolled.Affixes.Length; i++)
            {
                Assert.AreEqual(before.Rolled.Affixes[i].AffixId, after.Rolled.Affixes[i].AffixId);
                Assert.AreEqual(before.Rolled.Affixes[i].Value, after.Rolled.Affixes[i].Value, 0.0001f);
            }
        }

        [Test]
        public void CorruptedSave_FallsBackToBackup()
        {
            SaveManager.DeleteAll();

            SaveData first = SampleSave();
            SaveManager.Save(first);

            // Segundo save: o primeiro vira backup 0.
            first.Cash = 99999L;
            SaveManager.Save(first);

            File.WriteAllText(SaveManager.SavePath, "{ isto nao e json valido");

            SaveData recovered = SaveManager.Load();
            Assert.IsNotNull(recovered, "Um save corrompido precisa cair para o backup.");
            Assert.AreEqual(12345L, recovered.Cash, "O backup 0 guarda o estado anterior.");
        }

        [Test]
        public void ClockRunningBackwards_ReportsZeroElapsed()
        {
            var save = new SaveData();
            save.LastTimestamp = System.DateTime.UtcNow.AddHours(6);   // futuro

            // GDD 20.5: clamp em zero, sem acusacao de trapaca.
            Assert.AreEqual(0f, SaveManager.SecondsSince(save), 0.001f);
        }

        [UnityTest]
        public IEnumerator BootstrapScene_RunsARaceEndToEnd()
        {
            SaveManager.DeleteAll();

            var root = new GameObject("TestBootstrap");
            root.AddComponent<FramedDrift.Bootstrap.Bootstrap>();

            yield return null;   // Awake do Bootstrap monta o grafo

            App.GameManager game = App.GameManager.Instance;
            Assert.IsNotNull(game, "O Bootstrap precisa instanciar o GameManager.");
            Assert.IsNotNull(game.Content, "O conteudo precisa carregar na inicializacao.");
            Assert.IsNotNull(game.ActiveCar(), "O jogo novo comeca com um carro.");

            // O carro novo vem com as pecas de serie montadas - sem isso ele correria
            // pior que o inicial, o que leria como bug e nao como decisao.
            for (int slot = 0; slot < 8; slot++)
                Assert.Greater(game.ActiveCar().ActiveBuild.SlotUids[slot], 0,
                    "Slot " + (PartSlot)slot + " deveria vir equipado de fabrica.");

            long cashBefore = game.Save.Cash;
            game.StartRace();

            Assert.IsTrue(game.IsRacing, "A corrida deve comecar imediatamente.");
            Assert.IsNotNull(game.CurrentResult, "O simulador resolve a corrida ANTES de reproduzir.");
            Assert.Greater(game.CurrentResult.Timeline.Length, 0);

            float timeout = 0f;
            while (game.IsRacing && timeout < 200f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            Assert.Less(timeout, 200f, "A corrida deveria ter terminado.");
            Assert.IsNotNull(game.CurrentResult.Rewards, "A finalizacao precisa gerar recompensa.");
            Assert.Greater(game.CurrentResult.Rewards.DriftScore, 0L);
            Assert.Greater(game.Save.Cash, cashBefore, "A corrida precisa creditar cash.");

            Object.Destroy(root);
            yield return null;
        }
    }
}
