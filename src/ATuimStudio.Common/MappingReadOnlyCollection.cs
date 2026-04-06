using System.Collections;
using System.Collections.Immutable;

namespace ATuimStudio
{
	public sealed class MappingReadOnlyCollection<TSource, TResult> : IReadOnlyCollection<TResult>
	{
		readonly IReadOnlyCollection<TSource> _source;
		readonly Func<TSource, TResult> _mapper;
		public MappingReadOnlyCollection(IReadOnlyCollection<TSource> source, Func<TSource, TResult> mapper)
		{
			_source = source;
			_mapper = mapper;
		}

		public MappingReadOnlyCollection(ImmutableArray<TSource> source, Func<TSource, TResult> mapper)
		{
			_source = source;
			_mapper = mapper;
		}

		public int Count => _source.Count;

		public IEnumerator<TResult> GetEnumerator()
			=> _source.Select(_mapper).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
}
