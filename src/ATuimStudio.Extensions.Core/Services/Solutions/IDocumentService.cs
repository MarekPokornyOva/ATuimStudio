using Microsoft.CodeAnalysis;

namespace ATuimStudio.Extensions.Core
{
	public interface IDocumentService
	{
		Document? GetDocument(string path);
	}
}
