namespace ATuimStudio.Extensions.TextEditReferences
{
	public interface IFindImplementationsResult
	{
		IReadOnlyCollection<ReferenceItem> Implementations { get; }
	}
}
