// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 18.5, 19.1
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// Paleta e primitivas de desenho da UI de placeholder.
    ///
    /// <b>Toda a UI do MVP e IMGUI.</b> E uma escolha deliberada e provisoria: IMGUI nao
    /// precisa de nenhum asset autorado, entao o jogo RODA antes de existir arte - que e
    /// a condicao para as fases 1 a 6 poderem ser jogadas e medidas.
    ///
    /// D-07 e o argumento a favor: a assinatura do produto (modo Taskbar) e feature de
    /// apresentacao e entra na Fase 15, DEPOIS de o jogo ser divertido numa janela normal.
    /// Construir a UI final agora travaria decisoes antes de saber quais informacoes
    /// importam. Esta camada existe para descobrir isso.
    ///
    /// Os quatro principios da secao 18.5 ja valem aqui, porque sao de design e nao de
    /// tecnologia:
    ///   - comparacao sempre visivel;
    ///   - nunca mostrar uma stat sem a frase que a explica;
    ///   - sem confirmacao para acoes reversiveis;
    ///   - cor codifica raridade e NADA MAIS.
    /// </summary>
    public static class UiSkin
    {
        public static readonly Color Background = new Color(0.06f, 0.06f, 0.09f, 0.92f);
        public static readonly Color Panel = new Color(0.10f, 0.10f, 0.14f, 0.95f);
        public static readonly Color Text = new Color(0.92f, 0.92f, 0.95f);
        public static readonly Color Dim = new Color(0.55f, 0.56f, 0.62f);
        public static readonly Color Accent = new Color(1f, 0.36f, 0.55f);
        public static readonly Color Positive = new Color(0.35f, 0.92f, 0.62f);
        public static readonly Color Negative = new Color(1f, 0.42f, 0.38f);
        public static readonly Color BarTrack = new Color(1f, 1f, 1f, 0.10f);

        private static Texture2D _white;
        private static GUIStyle _label;
        private static GUIStyle _title;
        private static GUIStyle _big;
        private static GUIStyle _mono;
        private static GUIStyle _box;

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                    _white.hideFlags = HideFlags.HideAndDontSave;
                }
                return _white;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                if (_label == null)
                    _label = new GUIStyle(GUI.skin.label)
                    { fontSize = 13, normal = { textColor = Text }, richText = true };
                return _label;
            }
        }

        public static GUIStyle Title
        {
            get
            {
                if (_title == null)
                    _title = new GUIStyle(GUI.skin.label)
                    { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = Text } };
                return _title;
            }
        }

        /// <summary>O contador principal. Sobe com tweening, nunca salta (GDD 6.4).</summary>
        public static GUIStyle Big
        {
            get
            {
                if (_big == null)
                    _big = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 40,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Text },
                        alignment = TextAnchor.MiddleLeft,
                    };
                return _big;
            }
        }

        public static GUIStyle Mono
        {
            get
            {
                if (_mono == null)
                    _mono = new GUIStyle(GUI.skin.label)
                    { fontSize = 12, normal = { textColor = Dim }, richText = true };
                return _mono;
            }
        }

        public static GUIStyle Box
        {
            get
            {
                if (_box == null)
                {
                    _box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
                }
                return _box;
            }
        }

        public static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, White);
            GUI.color = previous;
        }

        /// <summary>Barra 0-1 com rotulo e valor. E o formato das 4 barras da garagem.</summary>
        public static void Bar(Rect rect, string label, float value01, string valueText, Color fill)
        {
            var labelRect = new Rect(rect.x, rect.y, 118f, rect.height);
            GUI.Label(labelRect, label, Label);

            var track = new Rect(rect.x + 122f, rect.y + 4f, rect.width - 200f, rect.height - 8f);
            Fill(track, BarTrack);
            Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(value01), track.height), fill);

            var valueRect = new Rect(track.xMax + 8f, rect.y, 70f, rect.height);
            GUI.Label(valueRect, valueText, Label);
        }

        /// <summary>
        /// Cor por raridade - e SO por raridade.
        ///
        /// A qualidade da rolagem nao entra na cor (GDD 18.5); ela e comunicada por
        /// icone. Misturar as duas coisas faria o jogador parar de ler as cores, que e
        /// exatamente o motivo de existirem cinco raridades e nao sete (GDD 10.2).
        /// </summary>
        public static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return new Color(0.72f, 0.74f, 0.78f);
                case Rarity.Uncommon: return new Color(0.44f, 0.86f, 0.52f);
                case Rarity.Rare: return new Color(0.36f, 0.66f, 1f);
                case Rarity.Epic: return new Color(0.76f, 0.45f, 1f);
                default: return new Color(1f, 0.68f, 0.22f);
            }
        }

        public static Color RiskColor(RiskBand band)
        {
            switch (band)
            {
                case RiskBand.Low: return Positive;
                case RiskBand.Medium: return new Color(0.95f, 0.85f, 0.35f);
                case RiskBand.High: return new Color(1f, 0.6f, 0.25f);
                default: return Negative;
            }
        }

        public static string RiskLabel(RiskBand band)
        {
            switch (band)
            {
                case RiskBand.Low: return "BAIXO";
                case RiskBand.Medium: return "MEDIO";
                case RiskBand.High: return "ALTO";
                default: return "EXTREMO";
            }
        }

        /// <summary>
        /// Numero grande com separador. Nenhum numero exibido passa de 10 digitos
        /// (GDD, risco R6) - acima disso vira notacao curta.
        /// </summary>
        public static string Number(long value)
        {
            if (value >= 1000000000L) return (value / 1000000000.0).ToString("0.00") + "B";
            if (value >= 10000000L) return (value / 1000000.0).ToString("0.0") + "M";
            return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string Number(double value)
        {
            return Number((long)value);
        }
    }
}
