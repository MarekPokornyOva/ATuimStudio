using ATuimStudio.Extensions.Core;

namespace ATuimStudio
{
	sealed class Pub<TMessage> : IPub<TMessage>
	{
		readonly ISubRegistrator<TMessage> _subRegistrator;
		public Pub(ISubRegistrator<TMessage> subRegistrator)
		{
			_subRegistrator = subRegistrator;
		}

		public void Raise(TMessage message)
		{
			foreach (Action<TMessage> handler in _subRegistrator.GetHandlers())
				handler(message);
		}
	}
}
