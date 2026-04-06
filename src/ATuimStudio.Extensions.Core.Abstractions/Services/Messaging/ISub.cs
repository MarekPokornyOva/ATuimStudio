namespace ATuimStudio.Extensions.Core
{
	public interface ISub<out TMessage>
	{
		void Register(Action<TMessage> handler);
		void Unregister(Action<TMessage> handler);
	}
}
