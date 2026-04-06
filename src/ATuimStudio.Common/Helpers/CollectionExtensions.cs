public static class CollectionExtensions
{
	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
	{
		if (collection is List<T> list && elements is IReadOnlyCollection<T> srcColl)
		{
			int reqCap = list.Count + srcColl.Count;
			if (list.Capacity < reqCap)
				list.Capacity = reqCap;
		}

		foreach (T element in elements)
			collection.Add(element);
	}
}
