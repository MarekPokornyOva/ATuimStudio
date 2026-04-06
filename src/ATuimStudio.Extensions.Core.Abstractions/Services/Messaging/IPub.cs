namespace ATuimStudio.Extensions.Core
{
	public interface IPub<in TMessage>
	{
		void Raise(TMessage message);
	}
}
