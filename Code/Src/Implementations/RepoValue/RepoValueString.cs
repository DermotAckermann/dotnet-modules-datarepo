using System;

namespace AA.Modules.DataRepoModule
{
    public class RepoValueString : RepoValueBase
    {
        // Immutable container for value and timestamp
        private record StringSnapshot(string? Value, DateTime Timestamp);

        // Atomic reference to the current value
        private volatile StringSnapshot _data = new(null, DateTime.MinValue);

        public RepoValueString() : base(RepoValueType.String)
        {
        }

        public override object? GetValue()
        {
            var snapshot = _data;
            return snapshot.Value;
        }

        public override void SetValue(object? value, string? writePass = null)
        {
            string? result;

            if (value == null)
            {
                result = null;
            }
            else if (value is string str)
            {
                result = str;
            }
            else if (!TypeEnforcement)
            {
                result = value.ToString();
            }
            else
            {
                throw new ArgumentException("Type enforcement: only string allowed.");
            }

            _data = new StringSnapshot(result, DateTime.UtcNow);
        }

        public override DateTime LastWriteUtc => _data.Timestamp;
    }
}
