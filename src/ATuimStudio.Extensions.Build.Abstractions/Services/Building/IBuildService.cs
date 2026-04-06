namespace ATuimStudio.Extensions.Build
{
	public interface IBuildService
	{
		Task<IBuildResult> BuildAsync(CancellationToken cancellationToken);
		Task<IBuildResult> BuildProjectAsync(string name, CancellationToken cancellationToken);
	}
}
