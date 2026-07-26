namespace ATuimStudio.Extensions.TextEditReferences
{
	public interface IReferencesFinder
	{
		Task<IAllReferencesResult> FindAllReferencesAsync(string filename, int offset, CancellationToken cancellationToken);
		Task<IFindDefinitionResult> FindDefinitionAsync(string filename, int offset, CancellationToken cancellationToken);
		Task<IFindImplementationsResult> FindImplementationsAsync(string filename, int offset, CancellationToken cancellationToken);
	}
}
