// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 13.1, 3.5, 18.5
// -----------------------------------------------------------------------------

using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Simulation;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// O encontro de rival (GDD 13.1) e o placar do duelo.
    ///
    /// Duas regras governam esta tela:
    ///
    /// <b>1. Ela nunca para o jogo.</b> O rival e sorteado com a antecedencia da previsao
    /// do tempo, e IGNORAR e o padrao de quem nao responde. Um modal bloqueante no meio
    /// do auto-race travaria o farm, e a 13.2 e explicita: o encontro e sempre opcional e
    /// sempre resolvivel pela automacao.
    ///
    /// <b>2. O resultado mostra POSICAO E SCORE lado a lado.</b> E a unica tela do jogo
    /// onde a tensao da secao 3.5 aparece inteira: o AKIRA cruza a linha na frente e
    /// perde. Mostrar so o vencedor - de qualquer um dos dois criterios - apagaria
    /// exatamente a licao que o rival existe para ensinar.
    /// </summary>
    public sealed class RivalUI : MonoBehaviour
    {
        private const float OutcomeSeconds = 7f;

        private RivalDetected _pending;
        private bool _hasPending;

        private RivalOutcome _outcome;
        private float _outcomeUntil;

        private void OnEnable()
        {
            EventBus.Subscribe<RivalDetected>(OnDetected);
            EventBus.Subscribe<RaceFinished>(OnRaceFinished);
            EventBus.Subscribe<CarBlueprintUnlocked>(OnCarBlueprint);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RivalDetected>(OnDetected);
            EventBus.Unsubscribe<RaceFinished>(OnRaceFinished);
            EventBus.Unsubscribe<CarBlueprintUnlocked>(OnCarBlueprint);
        }

        private void OnDetected(RivalDetected evt)
        {
            _pending = evt;
            _hasPending = true;
        }

        private void OnRaceFinished(RaceFinished evt)
        {
            _hasPending = false;
            if (evt.Rival == null) return;

            _outcome = evt.Rival;
            _outcomeUntil = Time.time + OutcomeSeconds;
        }

        private void OnCarBlueprint(CarBlueprintUnlocked evt)
        {
            _carBlueprintText = "PLANTA DE CARRO: " + evt.DisplayName;
            _carBlueprintUntil = Time.time + OutcomeSeconds * 2f;
        }

        private string _carBlueprintText;
        private float _carBlueprintUntil;

        private void OnGUI()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;

            if (_hasPending && game.PendingRival != null && !game.IsRacing) DrawChallenge(game);
            if (_outcome != null && Time.time < _outcomeUntil) DrawOutcome();
            if (_carBlueprintText != null && Time.time < _carBlueprintUntil) DrawCarBlueprint();
        }

        /// <summary>O prompt da GDD 13.1, com o carro e o score do rival ocultos.</summary>
        private void DrawChallenge(GameManager game)
        {
            var area = new Rect(Screen.width * 0.5f - 190f, 90f, 380f, 186f);
            UiSkin.Fill(area, UiSkin.Panel);

            GUILayout.BeginArea(new Rect(area.x + 16f, area.y + 12f, area.width - 32f, area.height - 24f));

            GUILayout.Label("RIVAL DETECTADO", UiSkin.Title);
            GUILayout.Label("\"" + _pending.DisplayName + "\"", UiSkin.Title);

            // Carro e score ficam em "?????" ate o desafio ser aceito: e o convite. O que
            // o jogador ja sabe sobre ele - a licao - aparece so depois do primeiro duelo.
            GUILayout.Label(_pending.Defeats > 0 ? "Carro:  " + _pending.CarId : "Carro:  ?????", UiSkin.Label);
            GUILayout.Label("Score:  ?????", UiSkin.Label);

            if (_pending.Defeats > 0)
                GUILayout.Label("Derrotado " + _pending.Defeats + "x   -   faltam "
                                + _pending.DefeatsUntilCarBlueprint + " para a planta do carro dele",
                                UiSkin.Mono);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("DESAFIAR", GUILayout.Height(30f)))
            {
                game.AcceptRival();
                _hasPending = false;
            }

            if (GUILayout.Button("IGNORAR", GUILayout.Height(30f)))
            {
                game.IgnoreRival();
                _hasPending = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Posicao e score lado a lado - a tensao da secao 3.5 numa tela so.
        /// </summary>
        private void DrawOutcome()
        {
            var area = new Rect(Screen.width * 0.5f - 200f, 90f, 400f, 150f);
            UiSkin.Fill(area, UiSkin.Panel);

            GUILayout.BeginArea(new Rect(area.x + 16f, area.y + 12f, area.width - 32f, area.height - 24f));

            GUILayout.Label(_outcome.PlayerWon
                ? _outcome.RivalDisplayName + " DERROTADO"
                : _outcome.RivalDisplayName + " VENCEU", UiSkin.Title);

            GUILayout.Label("Posicao      " + Ordinal(_outcome.PlayerPosition)
                            + "   (" + _outcome.RivalDisplayName + " "
                            + Ordinal(_outcome.RivalPosition) + ")", UiSkin.Label);

            GUILayout.Label("Drift Score  " + _outcome.PlayerScore.ToString("N0")
                            + "   (" + _outcome.RivalScore.ToString("N0") + ")", UiSkin.Label);

            // A licao so e dita quando ela realmente aconteceu. Escrever "ele venceu e
            // perdeu no score" numa corrida em que ele perdeu as duas coisas ensinaria a
            // desconfiar do texto.
            if (_outcome.RivalFinishedAhead && _outcome.PlayerWon)
                GUILayout.Label("Ele chegou na frente. Voce levou o placar.", UiSkin.Mono);
            else if (!_outcome.RivalFinishedAhead && !_outcome.PlayerWon)
                GUILayout.Label("Voce chegou na frente e perdeu no placar.", UiSkin.Mono);
            else if (_outcome.RivalFailures > 0)
                GUILayout.Label("Ele quebrou o carro sozinho.", UiSkin.Mono);

            GUILayout.EndArea();
        }

        private void DrawCarBlueprint()
        {
            var area = new Rect(Screen.width * 0.5f - 170f, 250f, 340f, 62f);
            UiSkin.Fill(area, UiSkin.Panel);

            GUILayout.BeginArea(new Rect(area.x + 14f, area.y + 10f, area.width - 28f, area.height - 20f));
            GUILayout.Label(_carBlueprintText, UiSkin.Title);
            GUILayout.Label("Tres derrotas. O carro dele e seu.", UiSkin.Mono);
            GUILayout.EndArea();
        }

        private static string Ordinal(int position)
        {
            return position <= 0 ? "-" : position + "o";
        }
    }
}
