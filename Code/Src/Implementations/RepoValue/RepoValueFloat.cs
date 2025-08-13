using System;

namespace AA.Modules.DataRepoModule;

public class RepoValueFloat : RepoValueBase
{
    // Immutable container for value and timestamp
    private record FloatSnapshot(double? Value, DateTime Timestamp);

    // Volatile field to hold the current snapshot
    private volatile FloatSnapshot _data = new(null, DateTime.MinValue);

    public RepoValueFloat() : base(RepoValueType.Float)
    {
    }

    public override object? GetValue()
    {
        var snapshot = _data;
        return snapshot.Value;
    }

    public override void SetValue(object? value, string? writePass = null)
    {
        double? result;

        if (value == null)
        {
            result = null;
        }
        else if (value is double d)
        {
            result = d;
        }
        else if (value is float f)
        {
            result = (double)f;
        }
        else if (value is int i)
        {
            result = (double)i;
        }
        else if (value is long l)
        {
            result = (double)l;
        }
        else if (value is string str && double.TryParse(str, out var parsed))
        {
            result = parsed;
        }
        else if (!TypeEnforcement)
        {
            // Try generic conversion if type enforcement is off
            result = Convert.ToDouble(value);
        }
        else
        {
            throw new ArgumentException("Type enforcement: only float/double or convertible types allowed.");
        }

        _data = new FloatSnapshot(result, DateTime.UtcNow);
    }

    public override DateTime LastWriteUtc => _data.Timestamp;
}
