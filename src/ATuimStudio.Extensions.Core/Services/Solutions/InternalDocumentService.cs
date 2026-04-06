using Microsoft.CodeAnalysis;

namespace ATuimStudio.Extensions.Core
{
	sealed class InternalDocumentService : IDocumentService
	{
		readonly ISolutionService _solutionService;
		public InternalDocumentService(ISolutionService solutionService)
		{
			_solutionService = solutionService;
		}

		public Document? GetDocument(string path)
		{
			if (_solutionService.CurrentSolution?.RawData is not Solution slnData)
				return null;
			return slnData.Projects.SelectMany(static p => p.Documents).FirstOrDefault(d => d.FilePath.EqualsOrdinal(path));
		}
	}
}
