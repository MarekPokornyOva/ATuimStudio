using Mono.Debugging.Client;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ATuimStudio.Debugger.Dap.Adapter
{
	sealed class VariableReferenceManager
	{
		const long _initialValue = 0;
		private long _nextId = _initialValue;

		private readonly ConcurrentDictionary<long, ObjectValue> _storage = new();

		/// <summary>
		/// Bit 63 mask to differentiate VariableReference from FrameReference.
		/// FrameReference has bit 63 = 0; VariableReference has bit 63 = 1.
		/// </summary>
		private const long VARIABLE_REFERENCE_BIT = 1L << 63;

		public long Add(ObjectValue value)
		{
			var id = Interlocked.Increment(ref _nextId);
			_storage[id] = value;
			// Set bit 63 to differentiate from FrameReference
			return id | VARIABLE_REFERENCE_BIT;
		}

		public bool TryGet(long id, [NotNullWhen(true)] out ObjectValue? value)
		{
			// Extract original ID by clearing bit 63
			var originalId = id & ~VARIABLE_REFERENCE_BIT;
			return _storage.TryGetValue(originalId, out value);
		}

		public void Clear()
		{
			_storage.Clear();
			_nextId = _initialValue;
		}
	}
}