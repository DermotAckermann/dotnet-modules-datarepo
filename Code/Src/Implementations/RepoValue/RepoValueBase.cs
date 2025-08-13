using System;

namespace AA.Modules.DataRepoModule;

public abstract class RepoValueBase
{
    public string? PassDelete { get; set; }
    public string? PassWrite { get; set; }
    public string? PassRead { get; set; }
    public bool TypeEnforcement { get; set; }

    public RepoValueType ValueType { get; }

    protected RepoValueBase(RepoValueType valueType)
    {
        ValueType = valueType;
    }

    /// <summary>
    /// Returns a snapshot of the current value.
    /// The returned object must be thread-safe and detached from internal state.
    /// </summary>
    public abstract object? GetValue();

    /// <summary>
    /// Updates the internal value by replacing it with a new snapshot.
    /// </summary>
    public abstract void SetValue(object? value, string? writePass = null);

    /// <summary>
    /// Gets the timestamp of the last write operation.
    /// </summary>
    public abstract DateTime LastWriteUtc { get; }
}
