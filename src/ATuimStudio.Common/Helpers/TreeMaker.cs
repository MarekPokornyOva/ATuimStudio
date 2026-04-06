namespace ATuimStudio.Common
{
	public static class TreeMaker
	{
		public static IEnumerable<INode> Make(IEnumerable<IEnumerable<string>> items)
		{
			Node root = new Node("");
			foreach (IEnumerable<string> segments in items)
				root.AddPath(segments.GetEnumerator());
			return ((INode)root).Children;
		}

		public static IEnumerable<INode<TData>> Make<TEnumerable, TData>(IEnumerable<(TEnumerable Segments, TData Data)> items) where TEnumerable : IEnumerable<string>
		{
			Node<TData> root = new Node<TData>("");
			foreach ((IEnumerable<string> segments, TData data) in items)
				root.AddPath(segments.GetEnumerator(), data);
			return ((INode<TData>)root).Children;
		}

		public interface INode
		{
			string Name { get; }
			IEnumerable<INode> Children { get; }
		}

		class Node : INode
		{
			public string Name { get; }
			Dictionary<string, Node>? Children { get; set; }
			IEnumerable<INode> INode.Children => Children == null ? [] : Children.Values;

			internal Node(string name)
			{
				Name = name;
			}

			internal void AddPath(IEnumerator<string> segments)
			{
				if (!segments.MoveNext())
					return;

				string segment = segments.Current;
				if (!(Children ??= []).TryGetValue(segment, out Node? partNode))
					Children.Add(segment, partNode = new Node(segment));
				partNode.AddPath(segments);
			}
		}

		public interface INode<TData>
		{
			string Name { get; }
			bool HasData { get; }
			TData Data { get; }
			IEnumerable<INode<TData>> Children { get; }
		}

		class Node<TData> : INode<TData>
		{
			public string Name { get; }
			Dictionary<string, Node<TData>>? Children { get; set; }
			IEnumerable<INode<TData>> INode<TData>.Children => Children == null ? [] : Children.Values;
			bool _hasData;
			TData? _data;
			public bool HasData => _hasData;
			public TData Data => _hasData ? _data! : throw new InvalidOperationException("Node has no data.");

			internal Node(string name)
			{
				Name = name;
			}

			internal void AddPath(IEnumerator<string> segments, TData data)
			{
				if (!segments.MoveNext())
				{
					SetData(data);
					return;
				}

				string segment = segments.Current;
				if (!(Children ??= []).TryGetValue(segment, out Node<TData>? partNode))
					Children.Add(segment, partNode = new Node<TData>(segment));
				partNode.AddPath(segments, data);
			}

			void SetData(TData data)
			{
				_data = data;
				_hasData = true;
			}
		}
	}
}
