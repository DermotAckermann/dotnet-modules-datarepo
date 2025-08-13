using System;

namespace AA.Modules.DataRepoModule
{
    public class RepoValueTimestampUtc : RepoValueBase
    {
        // Immutable container for timestamp value and write time
        private record TimestampSnapshot(DateTime? Value, DateTime Timestamp);

        // Atomic reference to the current snapshot
        private volatile TimestampSnapshot _data = new(null, DateTime.MinValue);

        public RepoValueTimestampUtc() : base(RepoValueType.TimestampUtc)
        {
        }

        public override object? GetValue()
        {
            var snapshot = _data;
            return snapshot.Value;
        }

        public override void SetValue(object? value, string? writePass = null)
        {
            DateTime? result;

            if (value == null)
            {
                result = null;
            }
            else if (value is DateTime dt)
            {
                result = dt.ToUniversalTime();
            }
            else if (value is string str && DateTime.TryParse(str, out var parsed))
            {
                result = parsed.ToUniversalTime();
            }
            else if (!TypeEnforcement)
            {
                result = Convert.ToDateTime(value).ToUniversalTime();
            }
            else
            {
                throw new ArgumentException("Type enforcement: only DateTime or convertible string allowed.");
            }

            _data = new TimestampSnapshot(result, DateTime.UtcNow);
        }

        public override DateTime LastWriteUtc => _data.Timestamp;
    }
}
