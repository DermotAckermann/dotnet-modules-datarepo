using System;

namespace AA.Modules.DataRepoModule
{
    public class RepoValueJson : RepoValueBase
    {
        // Immutable snapshot of JSON string and timestamp
        private record JsonSnapshot(string? Value, DateTime Timestamp);

        // Atomic reference to current snapshot
        private volatile JsonSnapshot _data = new(null, DateTime.MinValue);

        public RepoValueJson() : base(RepoValueType.Json)
        {
        }

        public override object? GetValue()
        {
            var snapshot = _data;
            return snapshot.Value;
        }

        public override void SetValue(object? value, string? writePass = null)
        {
            string? json;

            if (value == null)
            {
                json = null;
            }
            else if (value is string str)
            {
                json = str;
            }
            else if (!TypeEnforcement)
            {
                json = value.ToString(); // fallback to ToString() if enforcement is off
            }
            else
            {
                throw new ArgumentException("Type enforcement: only string allowed for JSON.");
            }

            _data = new JsonSnapshot(json, DateTime.UtcNow);
        }

        public override DateTime LastWriteUtc => _data.Timestamp;
    }
}
