using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace AA.Modules.DataRepoModule;

public class DataRepo
{
    //***Class data
    #region Fields & Properties
    private readonly ConcurrentDictionary<string, RepoValueBase> _data = new();
    public DateTime LastStructureChangeUtc { get; private set; } = DateTime.UtcNow;
    #endregion

    //***Constructors


    //***Methods public

    public void CreateKey(string key, RepoValueType type, string? passDelete = null, string? passWrite = null, string? passRead = null, bool typeEnforcement = false)
    {
        RepoValueBase valueObj = type switch
        {
            RepoValueType.Integer => new RepoValueInteger(),
            RepoValueType.Float => new RepoValueFloat(),
            RepoValueType.String => new RepoValueString(),
            RepoValueType.Bool => new RepoValueBool(),
            RepoValueType.Json => new RepoValueJson(),
            RepoValueType.TimestampUtc => new RepoValueTimestampUtc(),
            _ => throw new ArgumentException("Unknown type")
        };

        valueObj.PassDelete = passDelete;
        valueObj.PassWrite = passWrite;
        valueObj.PassRead = passRead;
        valueObj.TypeEnforcement = typeEnforcement;

        if (!_data.TryAdd(key, valueObj))
            throw new InvalidOperationException($"Key '{key}' already exists.");

        LastStructureChangeUtc = DateTime.UtcNow;
    }

    public void CreateKeyMulti(IEnumerable<(string key, RepoValueType type)> entryData, string? passDelete = null, string? passWrite = null, string? passRead = null, bool typeEnforcement = false)
    {
        foreach (var (key, type) in entryData)
        {
            CreateKey(key, type, passDelete, passWrite, passRead, typeEnforcement);
        }
    }

    public void CreateAndWriteKey(string key, RepoValueType type, object value, string? deleteAndWritePass = null, bool typeEnforcement = false)
    {

        CreateKey(key, type, deleteAndWritePass, deleteAndWritePass, null, typeEnforcement);

        WriteKey(key, value, deleteAndWritePass);
        
    }

    public void CreateAndWriteKeyMulti(IEnumerable<(string key, RepoValueType type, object value)> entryData, string? deleteAndWritePass = null, bool typeEnforcement = false)
    {
        foreach (var (key, type, value) in entryData)
        {
            CreateKey(key, type, deleteAndWritePass, deleteAndWritePass, null, typeEnforcement);

            WriteKey(key, value, deleteAndWritePass);
        }
    }

    public void CreateFromJson(string json, string baseKey = "", string deletePass = null, string writePass = null, string readPass = null, bool typeEnforcement = false)
    {
        using var doc = JsonDocument.Parse(json);
        var entries = new List<(string key, RepoValueType type, object value)>();

        string normalizedBase = string.IsNullOrWhiteSpace(baseKey) ? "" : baseKey.TrimEnd('.');

        FlattenJsonElement(normalizedBase, doc.RootElement, entries);

        // Create all keys and write values
        CreateAndWriteKeyMulti(entries, deletePass, typeEnforcement);
    }

    public RepoValueBase ReadKey(string key, string? pass = null)
    {
        if (!_data.TryGetValue(key, out var obj))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        if (!string.IsNullOrEmpty(obj.PassRead) && obj.PassRead != pass)
            throw new UnauthorizedAccessException("Read password mismatch.");

        return obj;
    }

    public String ReadKeyString(string key, string? pass = null)
    {
        var repoValue = ReadKey(key, pass);
        return repoValue?.GetValue()?.ToString() ?? string.Empty;
    }

    public void WriteKey(string key, object value, string? writePass = null)
    {
        if (!_data.TryGetValue(key, out var obj))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        if (!string.IsNullOrEmpty(obj.PassWrite) && obj.PassWrite != writePass)
            throw new UnauthorizedAccessException("Write password mismatch.");

        obj.SetValue(value, writePass);
    }

    public void WriteKeyMulti(IEnumerable<(string key, object value)> writeData, string? writePass = null)
    {
        foreach (var (key, value) in writeData)
        {
            WriteKey(key, value, writePass);
        }
    }

    
    public void SetKeyNull(string key, string? writePass = null)
    {
        if (!_data.TryGetValue(key, out var obj))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        if (!string.IsNullOrEmpty(obj.PassWrite) && obj.PassWrite != writePass)
            throw new UnauthorizedAccessException("Write password mismatch.");

        obj.SetValue(null, writePass);
    }

    public void SetKeyNullMulti(IEnumerable<string> keys, string? writePass = null)
    {
        foreach (var key in keys)
        {
            SetKeyNull(key, writePass);
        }
    }

    public void DeleteKey(string key, string? deletePass = null)
    {
        if (!_data.TryGetValue(key, out var obj))
            throw new KeyNotFoundException($"Key '{key}' does not exist.");

        if (!string.IsNullOrEmpty(obj.PassDelete) && obj.PassDelete != deletePass)
            throw new UnauthorizedAccessException("Delete password mismatch.");

        _data.TryRemove(key, out _);
        LastStructureChangeUtc = DateTime.UtcNow;
    }

    public void DeleteKeyMulti(IEnumerable<string> keys, string? deletePass = null)
    {
        foreach (var key in keys)
        {
            DeleteKey(key, deletePass);
        }
    }

    public IEnumerable<(string Key, RepoValueBase Value)> GetAll()
    {
        foreach (var kvp in _data)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }

    public IEnumerable<string> FindKeys(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(filter));

        foreach (var key in _data.Keys)
        {
            if (key.StartsWith(filter, StringComparison.Ordinal))
            {
                yield return key;
            }
        }
    }

    public IEnumerable<string> FindKeysWithWildcards(string wildcardFilter)
    {
        if (string.IsNullOrEmpty(wildcardFilter))
            throw new ArgumentException("Filter cannot be null or empty.", nameof(wildcardFilter));

        // Escape all regex special chars, then replace * with .*
        string pattern = "^" + System.Text.RegularExpressions.Regex.Escape(wildcardFilter)
            .Replace("\\*", ".*") + "$";

        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (var key in _data.Keys)
        {
            if (regex.IsMatch(key))
            {
                yield return key;
            }
        }
    }

    public IEnumerable<string> GetEntityIds(string baseKey)
    {
        if (string.IsNullOrEmpty(baseKey))
            throw new ArgumentException("Base key cannot be null or empty.", nameof(baseKey));

        var entityIds = new HashSet<string>();

        string pattern = $"^{System.Text.RegularExpressions.Regex.Escape(baseKey)}\\.\\{{(.*?)\\}}";

        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

        foreach (var key in _data.Keys)
        {
            var match = regex.Match(key);
            if (match.Success && match.Groups.Count > 1)
            {
                entityIds.Add(match.Groups[1].Value);
            }
        }

        return entityIds;
    }

    public int GetArrayIndexCount(string baseKeyWithBrackets)
    {
        if (string.IsNullOrWhiteSpace(baseKeyWithBrackets))
            throw new ArgumentException("Base key cannot be null or empty.", nameof(baseKeyWithBrackets));

        if (!baseKeyWithBrackets.EndsWith(".[]"))
            throw new ArgumentException("Base key must end with .[] to indicate array placeholder.", nameof(baseKeyWithBrackets));

        string baseKey = baseKeyWithBrackets.Substring(0, baseKeyWithBrackets.Length - 3); // remove ".[]"

        // Regex: ^<baseKey>\.\[(\d+)\]
        string pattern = $"^{System.Text.RegularExpressions.Regex.Escape(baseKey)}\\.\\[(\\d+)\\]";

        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

        var indices = new HashSet<int>();

        foreach (var key in _data.Keys)
        {
            var match = regex.Match(key);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
            {
                indices.Add(index);
            }
        }

        return indices.Count;
    }

    public IEnumerable<string> GetChildKeys(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
            throw new ArgumentException("Base key cannot be null or empty.", nameof(baseKey));

        string prefix = baseKey.TrimEnd('.') + ".";
        int baseLength = prefix.Length;

        HashSet<string> childKeys = new();        
        
        foreach (var key in _data.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                string remainder = key.Substring(baseLength);
                int dotIndex = remainder.IndexOf('.');
                if (dotIndex == -1)
                {
                    childKeys.Add(remainder); // direct child
                }
                else
                {
                    childKeys.Add(remainder.Substring(0, dotIndex)); // group child by next segment
                }
            }
        }        

        return childKeys;
    }


    public string AddArrayElement(string arrayKeyTemplate, RepoValueType type, object? value = null,
                              string? deletePass = null, string? writePass = null, string? readPass = null, bool typeEnforcement = false)
    {
        if (string.IsNullOrWhiteSpace(arrayKeyTemplate))
            throw new ArgumentException("Array key template cannot be null or empty.", nameof(arrayKeyTemplate));

        // Expect template to include: "[]."
        const string marker = "[].";
        int markerIndex = arrayKeyTemplate.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex == -1)
            throw new ArgumentException("Array key template must contain '[].'", nameof(arrayKeyTemplate));

        // Extract prefix and suffix
        string baseKey = arrayKeyTemplate.Substring(0, markerIndex);
        string suffix = arrayKeyTemplate.Substring(markerIndex + marker.Length); // after "[]."

        // Regex to find existing indices
        string pattern = $"^{System.Text.RegularExpressions.Regex.Escape(baseKey)}\\.\\[(\\d+)\\]\\.{System.Text.RegularExpressions.Regex.Escape(suffix)}$";
        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

        int maxIndex = -1;
        foreach (var key in _data.Keys)
        {
            var match = regex.Match(key);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
            {
                if (index > maxIndex)
                    maxIndex = index;
            }
        }

        int nextIndex = maxIndex + 1;

        // Construct the new key
        string newKey = $"{baseKey}.[{nextIndex}].{suffix}";

        // Create and write the key
        CreateKey(newKey, type, deletePass, writePass, readPass, typeEnforcement);
        if (value != null)
        {
            WriteKey(newKey, value, writePass);
        }

        return newKey;
    }

    //***Public methods static

    public static string AppendKeyEntityId(string baseKey, string id, bool addDot)
    {
        return $"{baseKey}.{{{id}}}" + (addDot ? "." : "");
    }

    public static string AppendKeyArrayIndex(string baseKey, int index, bool addDot)
    {
        return $"{baseKey}.[{index}]" + (addDot ? "." : "");
    }

    //Methods private

    private void FlattenJsonElement(string prefix, JsonElement element, List<(string key, RepoValueType type, object value)> entries)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    string childKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenJsonElement(childKey, prop.Value, entries);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    string arrayKey = $"{prefix}.[{index}]";
                    FlattenJsonElement(arrayKey, item, entries);
                    index++;
                }
                break;

            case JsonValueKind.Number:
                // Always treat numbers as float
                if (element.TryGetDouble(out double d))
                    entries.Add((prefix, RepoValueType.Float, (float)d));
                else
                    entries.Add((prefix, RepoValueType.Float, 0f));
                break;

            case JsonValueKind.String:
                entries.Add((prefix, RepoValueType.String, element.GetString()));
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                entries.Add((prefix, RepoValueType.Bool, element.GetBoolean()));
                break;

            case JsonValueKind.Null:
                // Create string key but null value
                entries.Add((prefix, RepoValueType.String, null));
                break;

            default:
                // Fallback as string
                entries.Add((prefix, RepoValueType.String, element.ToString()));
                break;
        }
    }
}