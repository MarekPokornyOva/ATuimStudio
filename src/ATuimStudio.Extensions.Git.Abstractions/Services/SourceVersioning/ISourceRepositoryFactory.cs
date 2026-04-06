namespace ATuimStudio.Extensions.Git
{
	public interface ISourceRepositoryFactory
	{
		ISourceRepository Create(string path);
	}
}
