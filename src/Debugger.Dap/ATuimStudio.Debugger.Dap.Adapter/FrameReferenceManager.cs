using System.Collections.Concurrent;

namespace ATuimStudio.Debugger.Dap.Adapter
{
	sealed class FrameReferenceManager
	{
		// Storage for stateful references
		private readonly ConcurrentDictionary<long, (long ProcessId, long ThreadId, int FrameId)> _storage = new();
		private long _nextReferenceIndex = 1;

		// Bit 62 (instead of 63) = type indicator
		// Bit 63 is constant 0 (ensures positive sign)
		private const long TYPE_BIT_MASK = 1L << 62;

		// Bit 63 distinguishes FrameReference (0) from VariableReference (1)
		private const long SIGN_BIT_MASK = 1L << 63;

		/// <summary>
		/// Tells if a reference is a FrameReference.
		/// </summary>
		public static bool IsFrameReference(long reference)
			=> (reference & SIGN_BIT_MASK) == 0;

		// Stateless encoding limits
		private const int MAX_PROCESS_ID = 0xFFFF;                    // 16 bits (65,535)
		private const long MAX_THREAD_ID = 0xFFFFFFFFFF;              // 40 bits (1,099,511,627,775)
		private const int MAX_FRAME_ID = 0x3F;                        // 6 bits (63)

		/// <summary>
		/// Encodes ProcessId, ThreadId, and FrameId.
		/// Automatically chooses stateless encoding (bit 62 = 0) if values fit the limits,
		/// otherwise uses stateful encoding (bit 62 = 1) with adapter storage.
		/// Bit 63 is always 0 (positive sign).
		/// </summary>
		public long Encode(long processId, long threadId, int frameId)
		{
			// Check if values fit in stateless limits
			bool fitsStateless = processId >= 0 && processId <= MAX_PROCESS_ID &&
										threadId >= 0 && threadId <= MAX_THREAD_ID &&
										frameId >= 0 && frameId <= MAX_FRAME_ID;

			if (fitsStateless)
			{
				// Stateless: (ProcessId << 46) | (ThreadId << 6) | FrameId
				// Bit 62 remains 0 (stateless indicator)
				// Bit 63 remains 0 (positive sign)
				return (processId << 46) | (threadId << 6) | frameId;
			}
			else
			{
				// Stateful: store in adapter and return indexed reference
				long refIndex = Interlocked.Increment(ref _nextReferenceIndex);
				long reference = TYPE_BIT_MASK | refIndex;
				_storage[reference] = (processId, threadId, frameId);

				// Set bit 62 to 1 (stateful indicator), combine with index
				// Bit 63 is always 0
				return reference;
			}
		}

		/// <summary>
		/// Decodes a reference to retrieve ProcessId, ThreadId, and FrameId.
		/// Automatically detects encoding type from bit 62.
		/// </summary>
		public (long ProcessId, long ThreadId, int FrameId) Decode(long reference)
		{
			bool isStateful = (reference & TYPE_BIT_MASK) != 0;

			if (isStateful)
			{
				if (!_storage.TryGetValue(reference, out var value))
					throw new ArgumentException(
						 $"Reference index {reference} not found in storage",
						 nameof(reference));

				return value;
			}
			else
			{
				// Stateless decoding
				int frameId = (int)(reference & MAX_FRAME_ID);            // Bits 0-5 (6 bits)
				long threadId = (reference >> 6) & MAX_THREAD_ID;         // Bits 6-45 (40 bits)
				long processId = (reference >> 46) & MAX_PROCESS_ID;      // Bits 46-61 (16 bits)

				return (processId, threadId, frameId);
			}
		}

		/// <summary>
		/// Cleans up all stateful references from storage.
		/// </summary>
		public void Clear()
			=> _storage.Clear();
	}
}
