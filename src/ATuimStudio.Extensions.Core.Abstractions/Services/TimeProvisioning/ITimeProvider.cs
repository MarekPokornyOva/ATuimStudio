namespace ATuimStudio.Extensions.Core
{
	public interface ITimeProvider
	{
		public DateTimeOffset UtcNow { get; }
	}
}
