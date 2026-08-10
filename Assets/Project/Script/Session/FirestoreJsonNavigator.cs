using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PowerMath.Session
{
    public enum JsonValueKind
    {
        Null,
        Boolean,
        Number,
        String,
        Object,
        Array
    }

    public sealed class JsonValue
    {
        private JsonValue(JsonValueKind kind)
        {
            Kind = kind;
        }

        public JsonValueKind Kind { get; }
        public string Text { get; private set; }
        public bool Boolean { get; private set; }
        public IReadOnlyDictionary<string, JsonValue> Object { get; private set; }
        public IReadOnlyList<JsonValue> Array { get; private set; }

        public bool TryGet(string name, out JsonValue value)
        {
            value = null;
            return Kind == JsonValueKind.Object && Object != null &&
                Object.TryGetValue(name, out value);
        }

        internal static JsonValue Null() => new JsonValue(JsonValueKind.Null);
        internal static JsonValue FromBoolean(bool value) =>
            new JsonValue(JsonValueKind.Boolean) { Boolean = value };
        internal static JsonValue FromText(JsonValueKind kind, string value) =>
            new JsonValue(kind) { Text = value };
        internal static JsonValue FromObject(Dictionary<string, JsonValue> value) =>
            new JsonValue(JsonValueKind.Object) { Object = value };
        internal static JsonValue FromArray(List<JsonValue> value) =>
            new JsonValue(JsonValueKind.Array) { Array = value };
    }

    public static class FirestoreJsonNavigator
    {
        public static bool TryParse(string json, out JsonValue root, out string error)
        {
            root = null;
            error = string.Empty;
            try
            {
                var parser = new Parser(json);
                root = parser.Parse();
                return true;
            }
            catch (FormatException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryGetDocumentFields(JsonValue document, out JsonValue fields)
        {
            return TryGetObject(document, "fields", out fields);
        }

        public static bool TryGetMapFields(JsonValue value, out JsonValue fields)
        {
            fields = null;
            return TryGetObject(value, "mapValue", out JsonValue map) &&
                TryGetObject(map, "fields", out fields);
        }

        public static bool TryGetArrayValues(
            JsonValue value,
            out IReadOnlyList<JsonValue> values)
        {
            values = Array.Empty<JsonValue>();
            if (!TryGetObject(value, "arrayValue", out JsonValue arrayValue))
            {
                return false;
            }

            if (!arrayValue.TryGet("values", out JsonValue array))
            {
                return true;
            }

            if (array.Kind != JsonValueKind.Array)
            {
                return false;
            }

            values = array.Array;
            return true;
        }

        public static bool TryReadString(JsonValue value, out string result)
        {
            result = string.Empty;
            if (!TryGetScalar(value, "stringValue", JsonValueKind.String, out JsonValue leaf))
            {
                return false;
            }

            result = leaf.Text ?? string.Empty;
            return true;
        }

        public static bool TryReadInteger(JsonValue value, out long result)
        {
            result = 0L;
            if (TryGetScalar(value, "integerValue", JsonValueKind.String, out JsonValue text))
            {
                return long.TryParse(
                    text.Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            if (TryGetScalar(value, "integerValue", JsonValueKind.Number, out JsonValue number))
            {
                return long.TryParse(
                    number.Text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result
                );
            }

            return false;
        }

        public static bool TryReadBoolean(JsonValue value, out bool result)
        {
            result = false;
            if (!TryGetScalar(value, "booleanValue", JsonValueKind.Boolean, out JsonValue leaf))
            {
                return false;
            }

            result = leaf.Boolean;
            return true;
        }

        public static bool IsNull(JsonValue value)
        {
            return value != null && value.TryGet("nullValue", out _);
        }

        private static bool TryGetObject(
            JsonValue source,
            string name,
            out JsonValue value)
        {
            value = null;
            return source != null && source.TryGet(name, out value) &&
                value.Kind == JsonValueKind.Object;
        }

        private static bool TryGetScalar(
            JsonValue source,
            string name,
            JsonValueKind kind,
            out JsonValue value)
        {
            value = null;
            return source != null && source.TryGet(name, out value) && value.Kind == kind;
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json ?? throw new FormatException("JSON is required.");
            }

            public JsonValue Parse()
            {
                SkipWhitespace();
                JsonValue result = ParseValue();
                SkipWhitespace();
                if (_index != _json.Length)
                {
                    throw Error("Unexpected trailing JSON content.");
                }
                return result;
            }

            private JsonValue ParseValue()
            {
                SkipWhitespace();
                if (_index >= _json.Length) throw Error("Unexpected end of JSON.");
                switch (_json[_index])
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return JsonValue.FromText(JsonValueKind.String, ParseString());
                    case 't': ReadLiteral("true"); return JsonValue.FromBoolean(true);
                    case 'f': ReadLiteral("false"); return JsonValue.FromBoolean(false);
                    case 'n': ReadLiteral("null"); return JsonValue.Null();
                    default: return ParseNumber();
                }
            }

            private JsonValue ParseObject()
            {
                Expect('{');
                var values = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryConsume('}')) return JsonValue.FromObject(values);
                while (true)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    if (!values.TryAdd(key, ParseValue()))
                    {
                        throw Error($"Duplicate JSON property '{key}'.");
                    }
                    SkipWhitespace();
                    if (TryConsume('}')) return JsonValue.FromObject(values);
                    Expect(',');
                }
            }

            private JsonValue ParseArray()
            {
                Expect('[');
                var values = new List<JsonValue>();
                SkipWhitespace();
                if (TryConsume(']')) return JsonValue.FromArray(values);
                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']')) return JsonValue.FromArray(values);
                    Expect(',');
                }
            }

            private JsonValue ParseNumber()
            {
                int start = _index;
                if (TryConsume('-')) { }
                ReadDigits();
                if (TryConsume('.')) ReadDigits();
                if (TryConsume('e') || TryConsume('E'))
                {
                    if (TryConsume('+') || TryConsume('-')) { }
                    ReadDigits();
                }
                if (_index == start) throw Error("Invalid JSON value.");
                return JsonValue.FromText(
                    JsonValueKind.Number,
                    _json.Substring(start, _index - start)
                );
            }

            private void ReadDigits()
            {
                int start = _index;
                while (_index < _json.Length && char.IsDigit(_json[_index])) _index++;
                if (_index == start) throw Error("Expected a digit.");
            }

            private string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (_index < _json.Length)
                {
                    char character = _json[_index++];
                    if (character == '"') return builder.ToString();
                    if (character != '\\')
                    {
                        builder.Append(character);
                        continue;
                    }

                    if (_index >= _json.Length) throw Error("Incomplete JSON escape.");
                    char escape = _json[_index++];
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ParseUnicode()); break;
                        default: throw Error("Unsupported JSON escape.");
                    }
                }
                throw Error("Unterminated JSON string.");
            }

            private char ParseUnicode()
            {
                if (_index + 4 > _json.Length) throw Error("Incomplete Unicode escape.");
                if (!int.TryParse(
                    _json.Substring(_index, 4),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out int value))
                {
                    throw Error("Invalid Unicode escape.");
                }
                _index += 4;
                return (char)value;
            }

            private void ReadLiteral(string literal)
            {
                if (_index + literal.Length > _json.Length ||
                    !string.Equals(
                        _json.Substring(_index, literal.Length),
                        literal,
                        StringComparison.Ordinal))
                {
                    throw Error($"Expected '{literal}'.");
                }
                _index += literal.Length;
            }

            private void Expect(char character)
            {
                SkipWhitespace();
                if (_index >= _json.Length || _json[_index] != character)
                {
                    throw Error($"Expected '{character}'.");
                }
                _index++;
            }

            private bool TryConsume(char character)
            {
                if (_index < _json.Length && _json[_index] == character)
                {
                    _index++;
                    return true;
                }
                return false;
            }

            private void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index])) _index++;
            }

            private FormatException Error(string message)
            {
                return new FormatException($"{message} Position {_index}.");
            }
        }
    }
}
