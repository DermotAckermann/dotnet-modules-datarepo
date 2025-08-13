using System;

namespace AA.Modules.DataRepoModule
{
    public class RepoValueInteger : RepoValueBase
    {
        // Immutable container for value and timestamp
        private record IntegerSnapshot(long? Value, DateTime Timestamp);

        // Atomic snapshot holder
        private volatile IntegerSnapshot _data = new(null, DateTime.MinValue);

        public RepoValueInteger() : base(RepoValueType.Integer)
        {
        }

        public override object? GetValue()
        {
            var snapshot = _data;
            return snapshot.Value;
        }

        public override void SetValue(object? value, string? writePass = null)
        {
            long? result;

            if (value == null)
            {
                result = null;
            }
            else if (value is long l)
            {
                result = l;
            }
            else if (value is int i)
            {
                result = i;
            }
            else if (value is short s)
            {
                result = s;
            }
            else if (value is sbyte sb)
            {
                result = sb;
            }
            else if (value is byte b)
            {
                result = b;
            }
            else if (value is ushort us)
            {
                result = us;
            }
            else if (value is uint ui)
            {
                if (ui > long.MaxValue)
                    throw new OverflowException($"Unsigned 32-bit value {ui} cannot fit into signed 64-bit long.");
                result = (long)ui;
            }
            else if (value is ulong ul)
            {
                if (ul > long.MaxValue)
                    throw new OverflowException($"Unsigned 64-bit value {ul} cannot fit into signed 64-bit long.");
                result = (long)ul;
            }
            else if (value is string str)
            {
                if (!long.TryParse(str, out var parsed))
                    throw new ArgumentException($"String '{str}' is not a valid signed 64-bit integer.");
                result = parsed;
            }
            else if (!TypeEnforcement)
            {
                // Try generic conversion
                result = Convert.ToInt64(value);
            }
            else
            {
                throw new ArgumentException("Type enforcement: only integer-compatible types allowed.");
            }

            _data = new IntegerSnapshot(result, DateTime.UtcNow);
        }

        public override DateTime LastWriteUtc => _data.Timestamp;
    }
}
