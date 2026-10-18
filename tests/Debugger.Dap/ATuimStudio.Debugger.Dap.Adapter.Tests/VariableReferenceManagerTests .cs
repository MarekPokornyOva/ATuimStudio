using Mono.Debugging.Client;

namespace ATuimStudio.Debugger.Dap.Adapter.Tests
{
	[TestClass]
	public class VariableReferenceManagerTests
	{
		private VariableReferenceManager _manager = default!;

		[TestInitialize]
		public void Setup()
		{
			_manager = new VariableReferenceManager();
		}

		#region Add Method Tests

		[TestMethod]
		public void Add_WithValidValue_ReturnsBit63SetToOne()
		{
			// Arrange
			var value = new ObjectValue();

			// Act
			var id = _manager.Add(value);

			// Assert
			Assert.IsLessThan(0, id, "ID should be negative (bit 63 = 1)");
			// Verify bit 63 is set
			long bit63Mask = 1L << 63;
			Assert.AreNotEqual(0, id & bit63Mask, "Bit 63 should be set");
		}

		[TestMethod]
		public void Add_MultipleValues_ReturnsUniqueEncodedIds()
		{
			// Arrange
			var value1 = new ObjectValue();
			var value2 = new ObjectValue();
			var value3 = new ObjectValue();

			// Act
			var id1 = _manager.Add(value1);
			var id2 = _manager.Add(value2);
			var id3 = _manager.Add(value3);

			// Assert
			Assert.AreNotEqual(id1, id2);
			Assert.AreNotEqual(id2, id3);
			Assert.AreNotEqual(id1, id3);

			// Verify all have bit 63 set
			long bit63Mask = 1L << 63;
			Assert.AreNotEqual(0, id1 & bit63Mask, "ID1 should have bit 63 set");
			Assert.AreNotEqual(0, id2 & bit63Mask, "ID2 should have bit 63 set");
			Assert.AreNotEqual(0, id3 & bit63Mask, "ID3 should have bit 63 set");

			// Verify underlying IDs increment (when bit 63 is cleared)
			long underlying1 = id1 & ~bit63Mask;
			long underlying2 = id2 & ~bit63Mask;
			long underlying3 = id3 & ~bit63Mask;
			Assert.AreEqual(1, underlying1);
			Assert.AreEqual(2, underlying2);
			Assert.AreEqual(3, underlying3);
		}

		[TestMethod]
		public void Add_SameValueTwice_StoresWithDifferentEncodedIds()
		{
			// Arrange
			var value = new ObjectValue();

			// Act
			var id1 = _manager.Add(value);
			var id2 = _manager.Add(value);

			// Assert
			Assert.AreNotEqual(id1, id2);
			Assert.IsTrue(_manager.TryGet(id1, out var retrieved1));
			Assert.IsTrue(_manager.TryGet(id2, out var retrieved2));
			Assert.AreEqual(retrieved1, retrieved2);
		}

		[TestMethod]
		public void Add_ConcurrentCalls_AllEncodedIdsAreUnique()
		{
			// Arrange
			var ids = new System.Collections.Concurrent.ConcurrentBag<long>();
			var value = new ObjectValue();

			// Act
			Parallel.For(0, 1000, _ =>
			{
				var id = _manager.Add(value);
				ids.Add(id);
			});

			// Assert
			var uniqueIds = new HashSet<long>(ids);
			Assert.HasCount(1000, uniqueIds, "All IDs should be unique");

			// Verify all have bit 63 set
			long bit63Mask = 1L << 63;
			Assert.IsTrue(ids.All(id => (id & bit63Mask) != 0),
				"All returned IDs should have bit 63 set");
		}

		#endregion

		#region TryGet Method Tests

		[TestMethod]
		public void TryGet_WithEncodedId_ReturnsTrue()
		{
			// Arrange
			var value = new ObjectValue();
			var id = _manager.Add(value);

			// Act
			var result = _manager.TryGet(id, out var retrieved);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(value, retrieved);
		}

		[TestMethod]
		public void TryGet_WithNonExistentId_ReturnsFalse()
		{
			// Act
			long bit63Mask = 1L << 63;
			long fakeId = 999 | bit63Mask;  // Create a fake encoded ID
			var result = _manager.TryGet(fakeId, out var value);

			// Assert
			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[TestMethod]
		public void TryGet_WithUnencodedId_ReturnsFalse()
		{
			// Act - try to get with a positive ID (no bit 63)
			var result = _manager.TryGet(1, out var value);

			// Assert
			Assert.IsFalse(result, "Unencoded ID should not be found");
			Assert.IsNull(value);
		}

		[TestMethod]
		public void TryGet_WithZeroId_ReturnsFalse()
		{
			// Act
			var result = _manager.TryGet(0, out var value);

			// Assert
			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[TestMethod]
		public void TryGet_MultipleTimesForSameEncodedId_ReturnsConsistentValue()
		{
			// Arrange
			var value = new ObjectValue();
			var id = _manager.Add(value);

			// Act
			var result1 = _manager.TryGet(id, out var retrieved1);
			var result2 = _manager.TryGet(id, out var retrieved2);

			// Assert
			Assert.IsTrue(result1 && result2);
			Assert.AreEqual(retrieved1, retrieved2);
		}

		[TestMethod]
		public void TryGet_WithModifiedEncodedId_ReturnsFalse()
		{
			// Arrange
			var value = new ObjectValue();
			var id = _manager.Add(value);

			// Act - flip a bit in the lower 62 bits
			long modifiedId = id ^ 0x1;  // Flip bit 0
			var result = _manager.TryGet(modifiedId, out var _);

			// Assert
			Assert.IsFalse(result, "Modified ID should not be found");
		}

		#endregion

		#region Clear Method Tests

		[TestMethod]
		public void Clear_WithStoredValues_RemovesAllValues()
		{
			// Arrange
			var value1 = new ObjectValue();
			var value2 = new ObjectValue();
			var id1 = _manager.Add(value1);
			var id2 = _manager.Add(value2);

			// Act
			_manager.Clear();

			// Assert
			Assert.IsFalse(_manager.TryGet(id1, out _));
			Assert.IsFalse(_manager.TryGet(id2, out _));
		}

		[TestMethod]
		public void Clear_ResetsIdSequence()
		{
			// Arrange
			var value = new ObjectValue();
			_manager.Add(value);
			_manager.Add(value);
			_manager.Add(value);

			// Act
			_manager.Clear();
			var newId = _manager.Add(value);

			// Assert
			long bit63Mask = 1L << 63;
			long underlying = newId & ~bit63Mask;
			Assert.AreEqual(1, underlying, "Underlying ID sequence should restart at 1 after clear");
		}

		[TestMethod]
		public void Clear_ThenAdd_AllowsNewValues()
		{
			// Arrange
			var value1 = new ObjectValue();
			_manager.Add(value1);
			_manager.Clear();

			// Act
			var value2 = new ObjectValue();
			var newId = _manager.Add(value2);

			// Assert
			long bit63Mask = 1L << 63;
			Assert.AreNotEqual(0, newId & bit63Mask, "New ID should have bit 63 set");

			Assert.IsTrue(_manager.TryGet(newId, out var retrieved));
			Assert.AreEqual(value2, retrieved);
		}

		[TestMethod]
		public void Clear_OnEmptyManager_Succeeds()
		{
			// Act & Assert - should not throw
			_manager.Clear();
			var id = _manager.Add(new ObjectValue());
			Assert.IsLessThan(0, id, "ID should have bit 63 set");
		}

		[TestMethod]
		public void Clear_MultipleTimes_Succeeds()
		{
			// Arrange
			var value = new ObjectValue();
			_manager.Add(value);

			// Act & Assert - should not throw
			_manager.Clear();
			_manager.Clear();

			var newId = _manager.Add(value);
			long bit63Mask = 1L << 63;
			long underlying = newId & ~bit63Mask;
			Assert.AreEqual(1, underlying);
		}

		#endregion

		#region Bit 63 Differentiation Tests

		[TestMethod]
		public void Add_AllReturnedIds_HaveBit63SetToOne()
		{
			// Arrange
			long bit63Mask = 1L << 63;
			var ids = new List<long>();

			// Act
			for (int i = 0; i < 100; i++)
			{
				var id = _manager.Add(new ObjectValue());
				ids.Add(id);
			}

			// Assert
			Assert.IsTrue(ids.All(id => (id & bit63Mask) != 0),
				"All VariableReference IDs should have bit 63 = 1");
			Assert.IsTrue(ids.All(id => id < 0),
				"All VariableReference IDs should be negative");
		}

		[TestMethod]
		public void Add_DifferentiatesFromFrameReference_Bit63IsOne()
		{
			// Arrange & Act
			var id = _manager.Add(new ObjectValue());
			long bit63Mask = 1L << 63;

			// Assert
			// VariableReference has bit 63 = 1 (negative when interpreted as signed long)
			Assert.AreNotEqual(0, id & bit63Mask, "VariableReference bit 63 must be 1");
			Assert.IsLessThan(0, id, "VariableReference should be negative");

			// FrameReference would have bit 63 = 0 (non-negative)
			// This ensures they can be differentiated at call sites:
			// if (reference < 0) => VariableReference
			// if (reference >= 0) => FrameReference
		}

		#endregion

		#region Integration Tests

		[TestMethod]
		public void CompleteWorkflow_AddGetClear_Succeeds()
		{
			// Arrange
			var value1 = new ObjectValue();
			var value2 = new ObjectValue();
			long bit63Mask = 1L << 63;

			// Act & Assert
			var id1 = _manager.Add(value1);
			Assert.AreNotEqual(0, id1 & bit63Mask);
			Assert.IsTrue(_manager.TryGet(id1, out var retrieved1));
			Assert.AreEqual(value1, retrieved1);

			var id2 = _manager.Add(value2);
			Assert.AreNotEqual(0, id2 & bit63Mask);
			Assert.IsTrue(_manager.TryGet(id2, out var retrieved2));
			Assert.AreEqual(value2, retrieved2);

			_manager.Clear();
			Assert.IsFalse(_manager.TryGet(id1, out _));
			Assert.IsFalse(_manager.TryGet(id2, out _));

			var id3 = _manager.Add(value1);
			long underlying3 = id3 & ~bit63Mask;
			Assert.AreEqual(1, underlying3);
		}

		[TestMethod]
		public void ConcurrentAddAndGet_WithBit63Encoding_MaintainsIntegrity()
		{
			// Arrange
			var idsAndValues = new System.Collections.Concurrent.ConcurrentBag<(long Id, ObjectValue Value)>();
			int threadCount = 10;
			int operationsPerThread = 50;

			// Act
			Parallel.For(0, threadCount, _ =>
			{
				for (int i = 0; i < operationsPerThread; i++)
				{
					var value = new ObjectValue();
					var id = _manager.Add(value);
					idsAndValues.Add((id, value));
				}
			});

			// Assert
			Assert.HasCount(threadCount * operationsPerThread, idsAndValues);

			long bit63Mask = 1L << 63;
			foreach (var (id, value) in idsAndValues)
			{
				Assert.AreNotEqual(0, id & bit63Mask, "All IDs should have bit 63 set");
				Assert.IsTrue(_manager.TryGet(id, out var retrieved));
				Assert.AreEqual(value, retrieved);
			}
		}

		#endregion
	}

}