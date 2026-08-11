using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PowerMath.PlayerData;

namespace PowerMath.Session
{
    public sealed class FirestorePatchPlan
    {
        internal FirestorePatchPlan(PatchMap root, IReadOnlyList<string> fieldPaths)
        {
            Root = root;
            FieldPaths = fieldPaths;
        }

        internal PatchMap Root { get; }
        public IReadOnlyList<string> FieldPaths { get; }
        public bool IsEmpty => FieldPaths.Count == 0;
        public string ToJson() => FirestorePatchDocumentBuilder.Serialize(Root);
    }

    public sealed class FirestorePatchDocumentBuilder
    {
        private readonly PatchMap _root = new PatchMap();
        private readonly List<string> _fieldPaths = new List<string>();

        public void AddString(IReadOnlyList<string> segments, string value) =>
            Add(segments, PatchValue.String(value ?? string.Empty));
        public void AddInteger(IReadOnlyList<string> segments, long value) =>
            Add(segments, PatchValue.FromInteger(value));
        public void AddBoolean(IReadOnlyList<string> segments, bool value) =>
            Add(segments, PatchValue.BooleanValue(value));
        public void AddEmptyArray(IReadOnlyList<string> segments) =>
            Add(segments, PatchValue.EmptyArray());
        public void AddIntegerArray(IReadOnlyList<string> segments, IReadOnlyList<long> values) =>
            Add(segments, PatchValue.IntegerArray(values));
        public void AddInventoryArray(
            IReadOnlyList<string> segments,
            IReadOnlyList<PlayerSnapshot.InventoryItemData> values) =>
            Add(segments, PatchValue.InventoryArray(values));
        public void AddNull(IReadOnlyList<string> segments) =>
            Add(segments, PatchValue.Null());

        public FirestorePatchPlan Build()
        {
            return new FirestorePatchPlan(_root, _fieldPaths.ToArray());
        }

        private void Add(IReadOnlyList<string> segments, PatchValue value)
        {
            if (segments == null || segments.Count == 0 ||
                segments.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("A non-empty Firestore field path is required.");
            }

            PatchMap current = _root;
            for (int index = 0; index < segments.Count - 1; index++)
            {
                current = current.GetOrAddMap(segments[index]);
            }

            current.AddValue(segments[segments.Count - 1], value);
            _fieldPaths.Add(string.Join(".", segments.Select(FirestoreFieldPath.EscapeSegment)));
        }

        internal static string Serialize(PatchMap root)
        {
            var builder = new StringBuilder();
            builder.Append("{\"fields\":");
            WriteFields(builder, root);
            builder.Append('}');
            return builder.ToString();
        }

        private static void WriteFields(StringBuilder builder, PatchMap map)
        {
            builder.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, PatchValue> pair in map.Values)
            {
                if (!first) builder.Append(',');
                first = false;
                WriteJsonString(builder, pair.Key);
                builder.Append(':');
                WriteValue(builder, pair.Value);
            }
            builder.Append('}');
        }

        private static void WriteValue(StringBuilder builder, PatchValue value)
        {
            switch (value.Kind)
            {
                case PatchValueKind.Map:
                    builder.Append("{\"mapValue\":{\"fields\":");
                    WriteFields(builder, value.Map);
                    builder.Append("}}");
                    break;
                case PatchValueKind.String:
                    builder.Append("{\"stringValue\":");
                    WriteJsonString(builder, value.Text);
                    builder.Append('}');
                    break;
                case PatchValueKind.Integer:
                    builder.Append("{\"integerValue\":\"")
                        .Append(value.Integer.ToString(CultureInfo.InvariantCulture))
                        .Append("\"}");
                    break;
                case PatchValueKind.Boolean:
                    builder.Append("{\"booleanValue\":")
                        .Append(value.Boolean ? "true" : "false")
                        .Append('}');
                    break;
                case PatchValueKind.EmptyArray:
                    builder.Append("{\"arrayValue\":{\"values\":[]}}");
                    break;
                case PatchValueKind.IntegerArray:
                    builder.Append("{\"arrayValue\":{\"values\":[");
                    for (int index = 0; index < value.Integers.Count; index++)
                    {
                        if (index > 0) builder.Append(',');
                        builder.Append("{\"integerValue\":\"")
                            .Append(value.Integers[index].ToString(CultureInfo.InvariantCulture))
                            .Append("\"}");
                    }
                    builder.Append("]}}");
                    break;
                case PatchValueKind.InventoryArray:
                    builder.Append("{\"arrayValue\":{\"values\":[");
                    for (int index = 0; index < value.Inventory.Count; index++)
                    {
                        if (index > 0) builder.Append(',');
                        PlayerSnapshot.InventoryItemData item = value.Inventory[index];
                        builder.Append("{\"mapValue\":{\"fields\":{")
                            .Append("\"itemId\":{\"stringValue\":");
                        WriteJsonString(builder, item.itemId ?? string.Empty);
                        builder.Append("},\"upgradeLevel\":{\"integerValue\":\"")
                            .Append(item.upgradeLevel.ToString(CultureInfo.InvariantCulture))
                            .Append("\"},\"owned\":{\"booleanValue\":")
                            .Append(item.owned ? "true" : "false")
                            .Append("}}}}");
                    }
                    builder.Append("]}}");
                    break;
                case PatchValueKind.Null:
                    builder.Append("{\"nullValue\":null}");
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Firestore patch value.");
            }
        }

        private static void WriteJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32)
                        {
                            builder.Append("\\u")
                                .Append(((int)character).ToString("x4"));
                        }
                        else builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
        }
    }

    public static class FirestoreFieldPath
    {
        public static string EscapeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) throw new ArgumentException("Field segment is required.");
            bool simple = (char.IsLetter(segment[0]) || segment[0] == '_') &&
                segment.All(character => char.IsLetterOrDigit(character) || character == '_');
            if (simple) return segment;
            return "`" + segment.Replace("\\", "\\\\").Replace("`", "\\`") + "`";
        }
    }

    internal sealed class PatchMap
    {
        private readonly SortedDictionary<string, PatchValue> _values =
            new SortedDictionary<string, PatchValue>(StringComparer.Ordinal);
        public IEnumerable<KeyValuePair<string, PatchValue>> Values => _values;

        public PatchMap GetOrAddMap(string name)
        {
            if (_values.TryGetValue(name, out PatchValue existing))
            {
                if (existing.Kind != PatchValueKind.Map)
                    throw new InvalidOperationException("A patch path overlaps a scalar value.");
                return existing.Map;
            }
            var map = new PatchMap();
            _values.Add(name, PatchValue.MapValue(map));
            return map;
        }

        public void AddValue(string name, PatchValue value)
        {
            if (_values.ContainsKey(name)) throw new InvalidOperationException("Duplicate patch field.");
            _values.Add(name, value);
        }
    }

    internal enum PatchValueKind { Map, String, Integer, Boolean, EmptyArray, IntegerArray, InventoryArray, Null }

    internal sealed class PatchValue
    {
        private PatchValue(PatchValueKind kind) { Kind = kind; }
        public PatchValueKind Kind { get; }
        public PatchMap Map { get; private set; }
        public string Text { get; private set; }
        public long Integer { get; private set; }
        public bool Boolean { get; private set; }
        public IReadOnlyList<long> Integers { get; private set; }
        public IReadOnlyList<PlayerSnapshot.InventoryItemData> Inventory { get; private set; }
        public static PatchValue MapValue(PatchMap map) => new PatchValue(PatchValueKind.Map) { Map = map };
        public static PatchValue String(string value) => new PatchValue(PatchValueKind.String) { Text = value };
        public static PatchValue FromInteger(long value) => new PatchValue(PatchValueKind.Integer) { Integer = value };
        public static PatchValue BooleanValue(bool value) => new PatchValue(PatchValueKind.Boolean) { Boolean = value };
        public static PatchValue EmptyArray() => new PatchValue(PatchValueKind.EmptyArray);
        public static PatchValue IntegerArray(IReadOnlyList<long> values) =>
            new PatchValue(PatchValueKind.IntegerArray)
            {
                Integers = values == null ? Array.Empty<long>() : values.ToArray()
            };
        public static PatchValue InventoryArray(IReadOnlyList<PlayerSnapshot.InventoryItemData> values) =>
            new PatchValue(PatchValueKind.InventoryArray)
            {
                Inventory = values == null
                    ? Array.Empty<PlayerSnapshot.InventoryItemData>()
                    : values.Where(value => value != null).ToArray()
            };
        public static PatchValue Null() => new PatchValue(PatchValueKind.Null);
    }
}
