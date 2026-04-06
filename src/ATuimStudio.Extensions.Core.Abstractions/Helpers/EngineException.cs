namespace ATuimStudio.Extensions.Core
{
	public sealed class EngineException : Exception
	{
		public EngineException(string reason, string message) : base(string.Concat(reason, " - ", message))
		{
			Reason = reason;
		}

		public string Reason { get; }
	}
}
