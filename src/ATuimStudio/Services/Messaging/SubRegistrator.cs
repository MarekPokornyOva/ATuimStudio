using System.Collections.Concurrent;

namespace ATuimStudio
{
	interface ISubRegistrator<TMessage>
	{
		IEnumerable<Action<TMessage>> GetHandlers();
		void Register(Action<TMessage> handler);
		void Unregister(Action<TMessage> handler);
	}

	sealed class SubRegistrator<TMessage> : ISubRegistrator<TMessage>
	{
		readonly ConcurrentDictionary<Action<TMessage>, int> _handlers = new ConcurrentDictionary<Action<TMessage>, int>();
		public IEnumerable<Action<TMessage>> GetHandlers()
			=> _handlers.Keys;

		public void Register(Action<TMessage> handler)
			=> _handlers.AddOrUpdate(handler, default(int), static (_, x) => x);

		public void Unregister(Action<TMessage> handler)
			=> _handlers.Remove(handler, out _);
	}
}
