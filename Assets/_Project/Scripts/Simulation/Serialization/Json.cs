// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Serialization
//  GDD 0.2  secao 20.2
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FramedDrift.Simulation.Serialization
{
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    /// <summary>
    /// Leitor de JSON minimo, em C# puro.
    ///
    /// Por que nao JsonUtility: ele vive em UnityEngine, e este assembly tem
    /// noEngineReferences (GDD 20.3). Por que nao Newtonsoft: adicionar um pacote so
    /// para ler ~10 arquivos de conteudo nao se paga, e a resolucao do pacote viraria
    /// um requisito de rede para abrir o projeto.
    ///
    /// Cultura invariante em todo parse de numero - 1.25 nao pode virar 125 numa
    /// maquina configurada com virgula decimal.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonKind Kind { get; private set; }

        private readonly Dictionary<string, JsonValue> _members;
        private readonly List<JsonValue> _items;
        private readonly string _str;
        private readonly double _num;
        private readonly bool _bool;

        public static readonly JsonValue Null = new JsonValue();

        private JsonValue() { Kind = JsonKind.Null; }
        private JsonValue(bool b) { Kind = JsonKind.Bool; _bool = b; }
        private JsonValue(double d) { Kind = JsonKind.Number; _num = d; }
        private JsonValue(string s) { Kind = JsonKind.String; _str = s; }
        private JsonValue(List<JsonValue> items) { Kind = JsonKind.Array; _items = items; }
        private JsonValue(Dictionary<string, JsonValue> m) { Kind = JsonKind.Object; _members = m; }

        // --- acesso -----------------------------------------------------------

        /// <summary>Membro de objeto. Devolve Null quando ausente - nunca lanca.</summary>
        public JsonValue this[string key]
        {
            get
            {
                if (_members == null) return Null;
                JsonValue v;
                return _members.TryGetValue(key, out v) ? v : Null;
            }
        }

        public JsonValue this[int index]
        {
            get
            {
                if (_items == null || index < 0 || index >= _items.Count) return Null;
                return _items[index];
            }
        }

        public int Count
        {
            get
            {
                if (_items != null) return _items.Count;
                if (_members != null) return _members.Count;
                return 0;
            }
        }

        public bool Exists { get { return Kind != JsonKind.Null; } }

        public bool Has(string key)
        {
            return _members != null && _members.ContainsKey(key);
        }

        public IEnumerable<string> Keys
        {
            get { return _members != null ? (IEnumerable<string>)_members.Keys : new string[0]; }
        }

        public IEnumerable<JsonValue> Items
        {
            get { return _items != null ? (IEnumerable<JsonValue>)_items : new JsonValue[0]; }
        }

        // --- conversao --------------------------------------------------------

        public string AsString(string fallback)
        {
            if (Kind == JsonKind.String) return _str;
            if (Kind == JsonKind.Number) return _num.ToString(CultureInfo.InvariantCulture);
            if (Kind == JsonKind.Bool) return _bool ? "true" : "false";
            return fallback;
        }

        public string AsString() { return AsString(string.Empty); }

        public float AsFloat(float fallback)
        {
            return Kind == JsonKind.Number ? (float)_num : fallback;
        }

        public float AsFloat() { return AsFloat(0f); }

        public double AsDouble(double fallback)
        {
            return Kind == JsonKind.Number ? _num : fallback;
        }

        public double AsDouble() { return AsDouble(0.0); }

        public int AsInt(int fallback)
        {
            return Kind == JsonKind.Number ? (int)Math.Round(_num) : fallback;
        }

        public int AsInt() { return AsInt(0); }

        public long AsLong(long fallback)
        {
            return Kind == JsonKind.Number ? (long)Math.Round(_num) : fallback;
        }

        public long AsLong() { return AsLong(0L); }

        public bool AsBool(bool fallback)
        {
            if (Kind == JsonKind.Bool) return _bool;
            if (Kind == JsonKind.Number) return _num != 0.0;
            return fallback;
        }

        public bool AsBool() { return AsBool(false); }

        /// <summary>
        /// Enum por NOME, nunca por indice. Deliberado: o conteudo precisa sobreviver a
        /// uma reordenacao do enum sem virar outro valor silenciosamente.
        /// </summary>
        public T AsEnum<T>(T fallback) where T : struct
        {
            if (Kind != JsonKind.String) return fallback;
            try
            {
                return (T)Enum.Parse(typeof(T), _str, true);
            }
            catch (ArgumentException)
            {
                throw new JsonException("Valor " + _str + " nao pertence ao enum " + typeof(T).Name + ".");
            }
        }

        public float[] AsFloatArray()
        {
            if (_items == null) return new float[0];
            float[] result = new float[_items.Count];
            for (int i = 0; i < _items.Count; i++) result[i] = _items[i].AsFloat();
            return result;
        }

        public string[] AsStringArray()
        {
            if (_items == null) return new string[0];
            string[] result = new string[_items.Count];
            for (int i = 0; i < _items.Count; i++) result[i] = _items[i].AsString();
            return result;
        }

        // --- parse ------------------------------------------------------------

        public static JsonValue Parse(string text)
        {
            if (text == null) throw new JsonException("Texto nulo.");
            int i = 0;
            SkipWs(text, ref i);
            JsonValue v = ParseValue(text, ref i);
            SkipWs(text, ref i);
            if (i != text.Length) throw new JsonException("Lixo apos o fim do documento, posicao " + i + ".");
            return v;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                // O BOM escrito como escape de proposito: um BOM literal neste arquivo
                // reapareceria como mojibake no proximo editor que o abrisse.
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\uFEFF') { i++; continue; }

                // Comentarios de linha sao aceitos. Os arquivos de conteudo sao editados
                // a mao e explicar uma constante ao lado dela vale mais que a pureza.
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    while (i < s.Length && s[i] != '\n') i++;
                    continue;
                }
                break;
            }
        }

        private static JsonValue ParseValue(string s, ref int i)
        {
            if (i >= s.Length) throw new JsonException("Fim inesperado do documento.");
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return new JsonValue(ParseString(s, ref i));
                case 't': Expect(s, ref i, "true"); return new JsonValue(true);
                case 'f': Expect(s, ref i, "false"); return new JsonValue(false);
                case 'n': Expect(s, ref i, "null"); return Null;
                default: return new JsonValue(ParseNumber(s, ref i));
            }
        }

        private static void Expect(string s, ref int i, string literal)
        {
            if (i + literal.Length > s.Length || string.CompareOrdinal(s, i, literal, 0, literal.Length) != 0)
                throw new JsonException("Esperava " + literal + " na posicao " + i + ".");
            i += literal.Length;
        }

        private static JsonValue ParseObject(string s, ref int i)
        {
            var map = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            i++; // abre chave
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return new JsonValue(map); }
            while (true)
            {
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new JsonException("Esperava uma chave na posicao " + i + ".");
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new JsonException("Esperava dois-pontos na posicao " + i + ".");
                i++;
                SkipWs(s, ref i);
                map[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length) throw new JsonException("Objeto nao fechado.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return new JsonValue(map); }
                throw new JsonException("Esperava virgula ou fecha-chave na posicao " + i + ".");
            }
        }

        private static JsonValue ParseArray(string s, ref int i)
        {
            var list = new List<JsonValue>();
            i++; // abre colchete
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return new JsonValue(list); }
            while (true)
            {
                SkipWs(s, ref i);
                list.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new JsonException("Array nao fechado.");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return new JsonValue(list); }
                throw new JsonException("Esperava virgula ou fecha-colchete na posicao " + i + ".");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // aspas de abertura
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new JsonException("String nao fechada.");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw new JsonException("Escape truncado.");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new JsonException("Escape unicode truncado.");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw new JsonException("Escape desconhecido na posicao " + (i - 1) + ".");
                }
            }
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '-' || c == '+') i++;
                else break;
            }
            if (i == start) throw new JsonException("Numero invalido na posicao " + start + ".");

            double d;
            string raw = s.Substring(start, i - start);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                throw new JsonException("Numero invalido: " + raw);
            return d;
        }
    }

    public sealed class JsonException : Exception
    {
        public JsonException(string message) : base(message) { }
    }
}
