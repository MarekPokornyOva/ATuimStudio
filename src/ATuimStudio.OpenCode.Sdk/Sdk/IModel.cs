namespace ATuimStudio.OpenCode.Sdk
{
	public interface IModel
	{
		string Id { get; }
		string ProviderId { get; }
		string Name { get; }
		bool Enabled { get; }
	}
}
