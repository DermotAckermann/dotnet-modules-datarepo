using System;

namespace AA.Modules.DataRepoModule;

public class RepoValueBool : RepoValueBase
{
    // Immutable snapshot container
    private record BoolSnapshot(bool? Value, DateTime Timestamp);

    // Volatile snapshot reference
    private volatile BoolSnapshot _data = new(null, DateTime.MinValue);

    public RepoValueBool() : base(RepoValueType.Bool)
    {
    }

    public override object? GetValue()
    {
        var snapshot = _data;
        return snapshot.Value;
    }

    public override void SetValue(object? value, string? writePass = null)
    {
        bool? result;

        if (value == null)
        {
            result = null;
        }
        else if (value is bool b)
        {
            result = b;
        }
        else if (value is string str && bool.TryParse(str, out var parsed))
        {
            result = parsed;
        }
        else if (value is int i)
        {
            result = i != 0;
        }
        else if (!TypeEnforcement)
        {
            // Fallback: try to convert using standard Convert
            result = Convert.ToBoolean(value);
        }
        else
        {
            throw new ArgumentException("Type enforcement: only bool or convertible types allowed.");
        }

        _data = new BoolSnapshot(result, DateTime.UtcNow);
    }

    public override DateTime LastWriteUtc => _data.Timestamp;
}
