using ATuimStudio.Extensions.Core;

namespace ATuimStudio
{
	sealed class Sub<TMessage> : ISub<TMessage>
	{
		readonly ISubRegistrator<TMessage> _subRegistrator;
		public Sub(ISubRegistrator<TMessage> subRegistrator)
		{
			_subRegistrator = subRegistrator;
		}

		public void Register(Action<TMessage> handler)
		{
			_subRegistrator.Register(handler);
		}

		public void Unregister(Action<TMessage> handler)
		{
			_subRegistrator.Unregister(handler);
		}
	}
}
