#pragma warning disable IDE0042

namespace ATuimStudio.Debugger.Dap.Adapter.Tests
{
	[TestClass]
	public class FrameReferenceManagerTests
	{
		public TestContext TestContext { get; set; }

		// Type indicator bit mask (bit 62)
		private const long TYPE_BIT_MASK = 1L << 62;

		#region Encode Tests - Stateless

		[TestMethod]
		public void Encode_WithValidStatelessValues_ProducesPositiveReference()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 1234;
			long threadId = 5678;
			int frameId = 42;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsTrue(reference >= 0 && reference < TYPE_BIT_MASK,
				"Stateless reference should be in range [0, 2^62) with bit 62 = 0");
		}

		[TestMethod]
		public void Encode_WithMaxStatelessValues_ProducesValidReference()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 0xFFFF;              // 65,535
			long threadId = 0xFFFFFFFFFF;        // 1,099,511,627,775
			int frameId = 0x3F;                   // 63 (6 bits max)

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsTrue(reference >= 0 && reference < TYPE_BIT_MASK,
				"Max stateless values should produce reference with bit 62 = 0");
		}

		[TestMethod]
		public void Encode_WithZeroValues_ProducesZeroReference()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(0, 0, 0);

			// Assert
			Assert.AreEqual(0L, reference);
		}

		[TestMethod]
		[DataRow(100, 200, 50)]
		[DataRow(1, 1, 1)]
		[DataRow(512, 123456789, 32)]
		public void Encode_WithVariousStatelessValues_ProducesValidReferences(long processId, long threadId, int frameId)
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsTrue(reference >= 0 && reference < TYPE_BIT_MASK,
				$"ProcessId={processId}, ThreadId={threadId}, FrameId={frameId} should be stateless");
		}

		#endregion

		#region Encode Tests - Stateful

		[TestMethod]
		public void Encode_WhenProcessIdExceedsLimit_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 0x10000;             // 65,536 (exceeds max)
			long threadId = 100;
			int frameId = 5;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding (bit 62 = 1)");
		}

		[TestMethod]
		public void Encode_WhenThreadIdExceedsLimit_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 100;
			long threadId = 0xFFFFFFFFFF + 1;        // Exceeds 0xFFFFFFFFFF
			int frameId = 5;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding (bit 62 = 1)");
		}

		[TestMethod]
		public void Encode_WhenFrameIdExceedsLimit_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 100;
			long threadId = 200;
			int frameId = 64;                   // Exceeds max of 63 (6 bits)

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding (bit 62 = 1)");
		}

		[TestMethod]
		public void Encode_WithNegativeProcessId_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = -1;
			long threadId = 100;
			int frameId = 5;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding for negative ProcessId");
		}

		[TestMethod]
		public void Encode_WithNegativeThreadId_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 100;
			long threadId = -1;
			int frameId = 5;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding for negative ThreadId");
		}

		[TestMethod]
		public void Encode_WithNegativeFrameId_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 100;
			long threadId = 200;
			int frameId = -1;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Should use stateful encoding for negative FrameId");
		}

		[TestMethod]
		public void Encode_MultipleStatefulCalls_ProduceDifferentReferences()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 0x10000;
			long threadId = 100;
			int frameId = 5;

			// Act
			long ref1 = manager.Encode(processId, threadId, frameId);
			long ref2 = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.AreNotEqual(ref1, ref2, "Different stateful references should have different indices");
		}

		[TestMethod]
		public void Encode_WithMaxLongValues_UsesStatefulEncoding()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = long.MaxValue;
			long threadId = long.MaxValue;
			int frameId = int.MaxValue;

			// Act
			long reference = manager.Encode(processId, threadId, frameId);

			// Assert
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, "Max values should use stateful encoding");
		}

		#endregion

		#region Decode Tests - Stateless

		[TestMethod]
		public void Decode_WithStatelessReference_RecovrsValues()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long originalProcessId = 1234;
			long originalThreadId = 5678;
			int originalFrameId = 42;

			long reference = manager.Encode(originalProcessId, originalThreadId, originalFrameId);

			// Act
			var (processId, threadId, frameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(originalProcessId, processId);
			Assert.AreEqual(originalThreadId, threadId);
			Assert.AreEqual(originalFrameId, frameId);
		}

		[TestMethod]
		public void Decode_WithZeroReference_RecovrsZeros()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long reference = 0L;

			// Act
			var (processId, threadId, frameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(0L, processId);
			Assert.AreEqual(0L, threadId);
			Assert.AreEqual(0, frameId);
		}

		[TestMethod]
		public void Decode_WithMaxStatelessReference_RecovrsMaxValues()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long processId = 0xFFFF;
			long threadId = 0xFFFFFFFFFF;
			int frameId = 0x3F;                  // 6 bits max

			long reference = manager.Encode(processId, threadId, frameId);

			// Act
			var (decodedProcessId, decodedThreadId, decodedFrameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(processId, decodedProcessId);
			Assert.AreEqual(threadId, decodedThreadId);
			Assert.AreEqual(frameId, decodedFrameId);
		}

		[TestMethod]
		[DataRow(100, 200, 50)]
		[DataRow(1, 1, 1)]
		[DataRow(512, 123456789, 32)]
		public void Decode_WithVariousStatelessReferences_RecovrsValues(long processId, long threadId, int frameId)
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long reference = manager.Encode(processId, threadId, frameId);

			// Act
			var (decodedProcessId, decodedThreadId, decodedFrameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(processId, decodedProcessId);
			Assert.AreEqual(threadId, decodedThreadId);
			Assert.AreEqual(frameId, decodedFrameId);
		}

		#endregion

		#region Decode Tests - Stateful

		[TestMethod]
		public void Decode_WithStatefulReference_RecovrsValues()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long originalProcessId = 0x10000;
			long originalThreadId = 0x2000000000;
			int originalFrameId = 200;

			long reference = manager.Encode(originalProcessId, originalThreadId, originalFrameId);

			// Act
			var (processId, threadId, frameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(originalProcessId, processId);
			Assert.AreEqual(originalThreadId, threadId);
			Assert.AreEqual(originalFrameId, frameId);
		}

		[TestMethod]
		public void Decode_WithStatefulReferenceContainingNegativeValues_RecovrsCorrectly()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long originalProcessId = -1;
			long originalThreadId = -12345;
			int originalFrameId = -100;

			long reference = manager.Encode(originalProcessId, originalThreadId, originalFrameId);

			// Act
			var (processId, threadId, frameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(originalProcessId, processId);
			Assert.AreEqual(originalThreadId, threadId);
			Assert.AreEqual(originalFrameId, frameId);
		}

		[TestMethod]
		public void Decode_WithInvalidStatefulIndex_ThrowsArgumentException()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long invalidReference = TYPE_BIT_MASK | 999;  // Stateful bit set, index 999 doesn't exist

			// Act
			Assert.Throws<ArgumentException>(() => manager.Decode(invalidReference));
		}

		[TestMethod]
		public void Decode_WithInvalidStatefulIndex_ThrowsWithCorrectMessage()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long invalidReference = TYPE_BIT_MASK | 999;

			// Act & Assert
			ArgumentException exception = Assert.Throws<ArgumentException>(
			() => manager.Decode(invalidReference));

			Assert.Contains("not found in storage", exception.Message);
		}

		#endregion

		#region Round-Trip Tests

		[TestMethod]
		[DataRow(0, 0, 0)]
		[DataRow(1, 1, 1)]
		[DataRow(100, 200, 50)]
		[DataRow(0xFFFF, 0xFFFFFFFFFF, 0x3F)]
		[DataRow(512, 123456789, 32)]
		public void RoundTrip_WithValidStatelessValues_PreservesData(long processId, long threadId, int frameId)
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(processId, threadId, frameId);
			var (decodedProcessId, decodedThreadId, decodedFrameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(processId, decodedProcessId);
			Assert.AreEqual(threadId, decodedThreadId);
			Assert.AreEqual(frameId, decodedFrameId);
		}

		[TestMethod]
		[DataRow(0x10000, 100, 5)]
		[DataRow(100, 0x2000000000, 5)]
		[DataRow(100, 200, 128)]
		[DataRow(-1, -1, -1)]
		public void RoundTrip_WithStatefulValues_PreservesData(long processId, long threadId, int frameId)
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(processId, threadId, frameId);
			var (decodedProcessId, decodedThreadId, decodedFrameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(processId, decodedProcessId);
			Assert.AreEqual(threadId, decodedThreadId);
			Assert.AreEqual(frameId, decodedFrameId);
		}

		#endregion

		#region Clear Method Tests

		[TestMethod]
		public void Clear_WithStatelessReference_ReturnsTrue()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long reference = manager.Encode(100, 200, 50);

			// Act
			manager.Clear();

			// Stateless references should still be decodable after "removal"
			var decoded = manager.Decode(reference);
			Assert.AreEqual(100, decoded.ProcessId);
		}

		[TestMethod]
		public void Remove_WithStatefulReference_RemovesFromStorage()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long reference = manager.Encode(0x10000, 100, 5);

			// Act
			manager.Clear();

			// Should throw because reference was removed
			Assert.Throws<ArgumentException>(() => manager.Decode(reference));
		}

		#endregion

		#region Edge Case Tests

		[TestMethod]
		public void Encode_WithBoundaryValues_HandleCorrectly()
		{
			// Arrange & Act
			var manager = new FrameReferenceManager();

			// Just below stateless limits
			long ref1 = manager.Encode(0xFFFE, 0xFFFFFFFFFE, 0x3E);
			Assert.IsTrue(ref1 >= 0 && ref1 < TYPE_BIT_MASK, "Values just below limits should be stateless");

			// Exactly at stateless limits
			long ref2 = manager.Encode(0xFFFF, 0xFFFFFFFFFF, 0x3F);
			Assert.IsTrue(ref2 >= 0 && ref2 < TYPE_BIT_MASK, "Values at limits should be stateless");

			// Just above stateless limits
			long ref3 = manager.Encode(0x10000, 0xFFFFFFFFFF, 0x3F);
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, ref3, "ProcessId above limit should be stateful");

			long ref4 = manager.Encode(0xFFFF, 0x10000000000, 0x3F);
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, ref4, "ThreadId above limit should be stateful");

			long ref5 = manager.Encode(0xFFFF, 0xFFFFFFFFFF, 0x40);
			Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, ref5, "FrameId above limit should be stateful");
		}

		[TestMethod]
		public void Encode_WithLargeThreadIds_HandlesCorrectly()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			long[] largeThreadIds =
			[
			1000000000L,
			123456789012L,
			0xFFFFFFFFFF  // Max stateless
			];

			// Act & Assert
			foreach (long threadId in largeThreadIds)
			{
				long reference = manager.Encode(100, threadId, 50);
				var decoded = manager.Decode(reference);
				Assert.AreEqual(threadId, decoded.ThreadId, $"ThreadId {threadId} should round-trip correctly");
			}
		}

		[TestMethod]
		public void Decode_WithManuallyConstructedStatelessReference_WorksCorrectly()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Manually construct a stateless reference: (ProcessId << 46) | (ThreadId << 6) | FrameId
			long expectedProcessId = 0x1234;
			long expectedThreadId = 0x123456789;
			int expectedFrameId = 42;

			long reference = (expectedProcessId << 46) | (expectedThreadId << 6) | expectedFrameId;

			// Act
			var (processId, threadId, frameId) = manager.Decode(reference);

			// Assert
			Assert.AreEqual(expectedProcessId, processId);
			Assert.AreEqual(expectedThreadId, threadId);
			Assert.AreEqual(expectedFrameId, frameId);
		}

		[TestMethod]
		public void Encode_ZeroAndMaxSequence_MaintainsConsistency()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var testCases = new[]
			{
				(0L, 0L, 0),
				(1L, 1L, 1),
				(0xFFFF, 0xFFFFFFFFFF, 0x3F),
				(0L, 0xFFFFFFFFFF, 0x3F),
				(0xFFFF, 0L, 0)
			};

			// Act & Assert
			foreach (var (processId, threadId, frameId) in testCases)
			{
				long reference = manager.Encode(processId, threadId, frameId);
				var decoded = manager.Decode(reference);
				Assert.AreEqual(processId, decoded.ProcessId);
				Assert.AreEqual(threadId, decoded.ThreadId);
				Assert.AreEqual(frameId, decoded.FrameId);
			}
		}

		[TestMethod]
		public void Encode_WithBit62CheckForStateless_VerifiesBitIsZero()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(100, 200, 50);

			// Assert
			long bit62 = (reference & TYPE_BIT_MASK);
			Assert.AreEqual(0, bit62, "Stateless reference should have bit 62 = 0");
		}

		[TestMethod]
		public void Encode_WithBit62CheckForStateful_VerifiesBitIsOne()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long reference = manager.Encode(0x10000, 100, 5);

			// Assert
			long bit62 = (reference & TYPE_BIT_MASK);
			Assert.AreNotEqual(0, bit62, "Stateful reference should have bit 62 = 1");
		}

		[TestMethod]
		public void Encode_Bit63_AlwaysZero()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			// Act
			long ref1 = manager.Encode(100, 200, 50);        // Stateless
			long ref2 = manager.Encode(0x10000, 100, 5);     // Stateful

			// Assert
			long bit63_ref1 = (ref1 & (1L << 63));
			long bit63_ref2 = (ref2 & (1L << 63));

			Assert.AreEqual(0, bit63_ref1, "Stateless reference should have bit 63 = 0");
			Assert.AreEqual(0, bit63_ref2, "Stateful reference should have bit 63 = 0");
		}

		#endregion

		#region Thread Safety Tests

		[TestMethod]
		public void Encode_WithConcurrentStatefulEncoding_ProducesUniqueReferences()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var references = new HashSet<long>();
			var lockObj = new object();
			int threadCount = 10;
			int operationsPerThread = 100;

			// Act
			var tasks = new Task[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						long reference = manager.Encode(0x10000, j, 5);  // Force stateful (ProcessId exceeds limit)
						lock (lockObj)
						{
							references.Add(reference);
						}
					}
				}, TestContext.CancellationToken);
			}
			Task.WaitAll(tasks, TestContext.CancellationToken);

			// Assert
			Assert.HasCount(threadCount * operationsPerThread, references,
				"All concurrent encodes should produce unique references");
		}

		[TestMethod]
		public void Encode_Decode_WithConcurrentAccess_MaintainsIntegrity()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var encodeDecodeResults = new System.Collections.Concurrent.ConcurrentBag<bool>();
			int threadCount = 5;
			int operationsPerThread = 50;

			// Act
			var tasks = new Task[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				int threadIdx = i;
				tasks[i] = Task.Run(() =>
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						// Use stateful values to ensure consistent behavior
						long processId = 0x10000 + threadIdx * 1000 + j;
						long threadId = (long)(threadIdx * 10000 + j);
						int frameId = (threadIdx * 10 + j) % 32;  // Keep within stateless range

						long reference = manager.Encode(processId, threadId, frameId);
						var decoded = manager.Decode(reference);

						bool success = decoded.ProcessId == processId &&
							decoded.ThreadId == threadId &&
							decoded.FrameId == frameId;

						encodeDecodeResults.Add(success);
					}
				}, TestContext.CancellationToken);
			}
			Task.WaitAll(tasks, TestContext.CancellationToken);

			// Assert
			Assert.HasCount(threadCount * operationsPerThread, encodeDecodeResults);
			Assert.IsTrue(encodeDecodeResults.All(r => r),
				"All concurrent encode-decode operations should succeed");
		}

		[TestMethod]
		public void Encode_WithHighConcurrencyStateful_NeverRepeatsIndices()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var references = new System.Collections.Concurrent.ConcurrentBag<long>();
			int threadCount = 20;
			int operationsPerThread = 50;

			// Act
			var tasks = new Task[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						// Force stateful encoding
						long reference = manager.Encode(long.MaxValue, long.MaxValue, int.MaxValue);
						references.Add(reference);
					}
				}, TestContext.CancellationToken);
			}
			Task.WaitAll(tasks, TestContext.CancellationToken);

			// Assert
			Assert.HasCount(threadCount * operationsPerThread, references);
			Assert.AreEqual(references.Count, references.Distinct().Count(),
				"All concurrent stateful references should be unique");
		}

		[TestMethod]
		public void Encode_WithConcurrentMixedStatelessStateful_AllSucceed()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var results = new System.Collections.Concurrent.ConcurrentBag<(long Reference, bool IsValid)>();
			int threadCount = 10;
			int operationsPerThread = 30;

			// Act
			var tasks = new Task[threadCount];
			for (int i = 0; i < threadCount; i++)
			{
				int threadIdx = i;
				tasks[i] = Task.Run(() =>
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						// Alternate between stateless and stateful
						long reference;
						if (j % 2 == 0)
						{
							// Stateless
							reference = manager.Encode(100 + threadIdx, 200 + j, j % 32);
						}
						else
						{
							// Stateful
							reference = manager.Encode(0x10000 + threadIdx, 100 + j, 5);
						}

						var decoded = manager.Decode(reference);
						bool isValid = decoded.ProcessId >= 0 && decoded.ThreadId >= 0 && decoded.FrameId >= 0;
						results.Add((reference, isValid));
					}
				}, TestContext.CancellationToken);
			}
			Task.WaitAll(tasks, TestContext.CancellationToken);

			// Assert
			Assert.HasCount(threadCount * operationsPerThread, results);
			Assert.IsTrue(results.All(r => r.IsValid),
				"All concurrent encode-decode operations should produce valid results");
		}

		#endregion

		#region Integration Tests

		[TestMethod]
		public void MultipleEncodingsAndDecodings_WithMixedStatelessStateful_WorkCorrectly()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			var data = new[]
			{
				(100L, 200L, 50),              // Stateless
				(0x10000L, 100L, 5),           // Stateful (ProcessId exceeds limit)
				(500L, 0x2000000000L, 10),     // Stateful (ThreadId exceeds limit)
				(0xFFFF, 0xFFFFFFFFFF, 0x3F),  // Max stateless (updated FrameId to 0x3F = 63)
			};

			var references = new List<long>();

			// Act
			foreach (var (processId, threadId, frameId) in data)
			{
				long reference = manager.Encode(processId, threadId, frameId);
				references.Add(reference);
			}

			// Assert - verify all can be decoded
			for (int i = 0; i < data.Length; i++)
			{
				var decoded = manager.Decode(references[i]);
				Assert.AreEqual(data[i].Item1, decoded.ProcessId, $"ProcessId mismatch at index {i}");
				Assert.AreEqual(data[i].Item2, decoded.ThreadId, $"ThreadId mismatch at index {i}");
				Assert.AreEqual(data[i].Item3, decoded.FrameId, $"FrameId mismatch at index {i}");
			}
		}

		[TestMethod]
		public void Integration_StatelessAndStatefulMix_EncodingTypeMattersForBoundaries()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			var statelessCases = new[]
			{
				(1L, 1L, 1),
				(100L, 500L, 32),
				(0xFFFF, 0xFFFFFFFFFF, 0x3F),  // Max stateless
			};

			var statefulCases = new[]
			{
				(0x10000L, 1L, 1),             // ProcessId exceeds
				(1L, 0x100000000000L, 1),      // ThreadId exceeds
				(1L, 1L, 0x40),                // FrameId exceeds
			};

			// Act & Assert for stateless
			foreach (var (processId, threadId, frameId) in statelessCases)
			{
				long reference = manager.Encode(processId, threadId, frameId);
				Assert.IsTrue(reference >= 0 && reference < TYPE_BIT_MASK,
					$"({processId}, {threadId}, {frameId}) should be stateless encoded");

				var decoded = manager.Decode(reference);
				Assert.AreEqual(processId, decoded.ProcessId);
				Assert.AreEqual(threadId, decoded.ThreadId);
				Assert.AreEqual(frameId, decoded.FrameId);
			}

			// Act & Assert for stateful
			foreach (var (processId, threadId, frameId) in statefulCases)
			{
				long reference = manager.Encode(processId, threadId, frameId);
				Assert.IsGreaterThanOrEqualTo(TYPE_BIT_MASK, reference, $"({processId}, {threadId}, {frameId}) should be stateful encoded");

				var decoded = manager.Decode(reference);
				Assert.AreEqual(processId, decoded.ProcessId);
				Assert.AreEqual(threadId, decoded.ThreadId);
				Assert.AreEqual(frameId, decoded.FrameId);
			}
		}

		[TestMethod]
		public void Integration_Clear_RemovesOnlyStatefulReferences()
		{
			// Arrange
			var manager = new FrameReferenceManager();

			long statelessRef = manager.Encode(100, 200, 50);
			long statefulRef = manager.Encode(0x10000, 100, 5);

			// Act
			manager.Clear();

			// Assert - stateless should still work
			var decodedStateless = manager.Decode(statelessRef);
			Assert.AreEqual(100, decodedStateless.ProcessId);
			Assert.AreEqual(200, decodedStateless.ThreadId);
			Assert.AreEqual(50, decodedStateless.FrameId);

			// Stateful should fail
			Assert.Throws<ArgumentException>(() => manager.Decode(statefulRef),
				"Cleared stateful reference should not be decodable");
		}

		[TestMethod]
		public void Integration_LargeScaleOperations_WithThousandsOfStatefulReferences()
		{
			// Arrange
			var manager = new FrameReferenceManager();
			int operationCount = 5000;
			var references = new List<long>();

			// Act
			for (int i = 0; i < operationCount; i++)
			{
				long reference = manager.Encode(0x10000 + i, (long)i * 1000, i % 32);
				references.Add(reference);
			}

			// Verify all can be decoded
			for (int i = 0; i < operationCount; i++)
			{
				var decoded = manager.Decode(references[i]);
				Assert.AreEqual(0x10000 + i, decoded.ProcessId);
				Assert.AreEqual((long)i * 1000, decoded.ThreadId);
				Assert.AreEqual(i % 32, decoded.FrameId);
			}

			// Assert
			Assert.HasCount(operationCount, references);
			Assert.AreEqual(references.Count, references.Distinct().Count(),
				"All stateful references should be unique");
		}

		#endregion
	}
}
