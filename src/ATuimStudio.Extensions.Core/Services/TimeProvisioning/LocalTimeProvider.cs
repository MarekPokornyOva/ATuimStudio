namespace ATuimStudio.Extensions.Core
{
	sealed class LocalTimeProvider : ITimeProvider
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
	}
}
