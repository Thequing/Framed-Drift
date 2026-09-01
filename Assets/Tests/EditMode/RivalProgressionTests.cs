// -----------------------------------------------------------------------------
//  Framed Drift  -  Tests.EditMode
//  GDD 0.2  secoes 13.1, 8.2, 14.7, 20.5
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Progression;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using NUnit.Framework;

namespace FramedDrift.Tests.EditMode
{
    /// <summary>
    /// O que a derrota de um rival muda no save (GDD 13.1).
    ///
    /// Antes disto, `Progress.RivalsDefeated` era uma lista que TRES sistemas liam e
    /// NENHUM escrevia: a missao semanal "derrote um rival" era incumprivel, a regra de
    /// desbloqueio por rival de `MeetsUnlock` nunca podia passar, e o "derrotar tres
    /// vezes desbloqueia o carro dele" nao tinha onde contar - era uma lista de ids, sem
    /// contagem.
    /// </summary>
    public sealed class RivalProgressionTests
    {
        private ContentDatabase _content;
        private SaveData _save;
        private RivalSystem _rivals;
        private ReputationSystem _reputation;

        [SetUp]
        public void SetUp()
        {
            _content = TestWorld.Shared.Content;
            _save = new SaveData();
            _reputation = new ReputationSystem(_save, _content);
            _rivals = new RivalSystem(_save, _content);
        }

        // --- a contagem ---------------------------------------------------------------

        [Test]
        public void VencerNoScoreContaUmaDerrota()
        {
            _rivals.Record(Outcome("akira", playerWon: true));

            Assert.AreEqual(1, _rivals.DefeatsOf("akira"));
        }

        [Test]
        public void PerderNoScoreNaoContaDerrota()
        {
            _rivals.Record(Outcome("akira", playerWon: false));

            Assert.AreEqual(0, _rivals.DefeatsOf("akira"));
        }

        /// <summary>
        /// O encontro conta mesmo quando o jogador perde. E ele que a tela de rival usa
        /// para dizer "voce ja o encontrou 4 vezes, venceu 1".
        /// </summary>
        [Test]
        public void OEncontroContaMesmoNaDerrota()
        {
            _rivals.Record(Outcome("akira", playerWon: false));

            Assert.AreEqual(1, _rivals.EncountersOf("akira"));
        }

        [Test]
        public void DerrotasDeRivaisDiferentesNaoSeMisturam()
        {
            _rivals.Record(Outcome("akira", playerWon: true));
            _rivals.Record(Outcome("ken", playerWon: true));
            _rivals.Record(Outcome("ken", playerWon: true));

            Assert.AreEqual(1, _rivals.DefeatsOf("akira"));
            Assert.AreEqual(2, _rivals.DefeatsOf("ken"));
        }

        /// <summary>A lista antiga continua sendo escrita: e ela que MeetsUnlock le.</summary>
        [Test]
        public void ADerrotaEntraNaListaQueOsDesbloqueiosLeem()
        {
            _rivals.Record(Outcome("akira", playerWon: true));

            Assert.Contains("akira", _save.Progress.RivalsDefeated);
        }

        // --- a planta de CARRO (13.1) -------------------------------------------------

        /// <summary>
        /// "Derrotar o mesmo rival tres vezes desbloqueia seu carro como blueprint."
        /// Literal, e ate agora sem ninguem que contasse ate tres.
        /// </summary>
        [Test]
        public void TresDerrotasDesbloqueiamOCarroDoRival()
        {
            RivalDef akira = _content.Rival("akira");

            for (int i = 0; i < 3; i++) _rivals.Record(Outcome("akira", playerWon: true));

            Assert.Contains(akira.CarId, _save.Progress.CarBlueprints);
        }

        [Test]
        public void DuasDerrotasAindaNaoDesbloqueiam()
        {
            for (int i = 0; i < 2; i++) _rivals.Record(Outcome("akira", playerWon: true));

            Assert.AreEqual(0, _save.Progress.CarBlueprints.Count);
        }

        [Test]
        public void ADerrotaSeguinteNaoDuplicaAPlanta()
        {
            for (int i = 0; i < 5; i++) _rivals.Record(Outcome("akira", playerWon: true));

            Assert.AreEqual(1, _save.Progress.CarBlueprints.Count);
        }

        /// <summary>
        /// A planta e um ATALHO, nao um carimbo: ela dispensa a reputacao que o carro
        /// pedia. Sem isso o "carro do rival" nao seria uma recompensa - seria um aviso
        /// de que o carro que o jogador ja ia ganhar por reputacao agora esta pago duas
        /// vezes.
        /// </summary>
        [Test]
        public void APlantaDeCarroDispensaAReputacaoExigida()
        {
            CarDef car = _content.Car(_content.Rival("akira").CarId);

            _save.Reputation = 0;
            Assert.IsFalse(_reputation.MeetsUnlock(car), "Sem reputacao e sem planta ele nao devia abrir.");

            for (int i = 0; i < 3; i++) _rivals.Record(Outcome("akira", playerWon: true));

            Assert.IsTrue(_reputation.MeetsUnlock(car), "Tres derrotas precisam abrir o carro dele.");
        }

        [Test]
        public void APlantaDeCarroFazOCarroAparecerNosDesbloqueios()
        {
            for (int i = 0; i < 3; i++) _rivals.Record(Outcome("akira", playerWon: true));

            List<UnlockGranted> granted = _reputation.Evaluate();

            bool sawCar = false;
            for (int i = 0; i < granted.Count; i++)
                if (granted[i].Kind == "carro" && granted[i].Id == _content.Rival("akira").CarId) sawCar = true;

            Assert.IsTrue(sawCar, "O carro do rival precisa chegar como desbloqueio, com aviso.");
        }

        // --- o drop da assinatura (13.1) ----------------------------------------------

        /// <summary>
        /// "Quando derrotado, dropa a peca que ele estava usando." A assinatura saiu do
        /// pool de loot da regiao justamente para que este seja o unico caminho ate ela.
        /// </summary>
        [Test]
        public void ADerrotaDropaAAssinaturaDoRival()
        {
            RivalDef akira = _content.Rival("akira");
            RivalReward reward = _rivals.Record(Outcome("akira", playerWon: true));

            Assert.AreEqual(akira.SignaturePartId, reward.SignaturePartId,
                "A derrota tem de entregar a peca que ele usava.");
        }

        /// <summary>
        /// Alem da peca, a derrota rende PEDACOS da planta dela (GDD 10.6 + 13.1).
        ///
        /// E o que mantem a assinatura alcancavel a longo prazo depois que ela saiu do
        /// pool de loot: a primeira copia vem da derrota, e as seguintes da bancada, para
        /// quem insistir no mesmo rival.
        /// </summary>
        [Test]
        public void ADerrotaRendePedacosDaPlantaDaAssinatura()
        {
            RivalReward reward = _rivals.Record(Outcome("akira", playerWon: true));

            Assert.AreEqual(_content.Balance.RivalDefeatFragments, reward.BlueprintFragments);
            Assert.Greater(reward.BlueprintFragments, 0, "Uma derrota tem de render pedaco.");
        }

        [Test]
        public void PerderNaoDropaNada()
        {
            RivalReward reward = _rivals.Record(Outcome("akira", playerWon: false));

            Assert.IsNull(reward.SignaturePartId);
            Assert.AreEqual(0, reward.BlueprintFragments);
        }

        // --- missoes (14.7) -----------------------------------------------------------

        /// <summary>
        /// A missao semanal "Derrote um rival" existe desde a Fase 10 e era literalmente
        /// incumprivel: nada no jogo avancava a metrica RivalsDefeated.
        /// </summary>
        [Test]
        public void ADerrotaAvancaAMissaoSemanalDeRival()
        {
            var missions = new MissionSystem();
            Mission weekly = null;
            for (int i = 0; i < missions.Missions.Count; i++)
                if (missions.Missions[i].Metric == MissionMetric.RivalsDefeated) weekly = missions.Missions[i];

            Assert.IsNotNull(weekly, "A missao de rival sumiu do catalogo.");
            Assert.AreEqual(0L, weekly.Progress);

            missions.RecordRivalDefeat();

            Assert.AreEqual(1L, weekly.Progress);
        }

        private RivalOutcome Outcome(string rivalId, bool playerWon)
        {
            RivalDef rival = _content.Rival(rivalId);
            return new RivalOutcome
            {
                RivalId = rival.Id,
                RivalDisplayName = rival.DisplayName,
                CarId = rival.CarId,
                SignaturePartId = rival.SignaturePartId,
                PlayerScore = playerWon ? 2000L : 1000L,
                RivalScore = 1500L,
            };
        }
    }
}
