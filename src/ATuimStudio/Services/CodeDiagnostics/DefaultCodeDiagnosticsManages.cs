using ATuimStudio.Extensions.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ATuimStudio
{
	sealed class DefaultCodeDiagnosticsManages : ICodeDiagnosticsManager, IDisposable
	{
		readonly IDocumentService _documentService;
		public DefaultCodeDiagnosticsManages(IDocumentService documentService)
		{
			_documentService = documentService;
		}

		readonly List<DiagnosticsItem> _diagnosticsItems = new List<DiagnosticsItem>();
		public IReadOnlyCollection<IDiagnosticsItem> DiagnosticsItems => _diagnosticsItems.AsReadOnly();

		public event EventHandler? Updated;

		readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsCache = new ConcurrentDictionary<string, CancellationTokenSource>();
		public async void Refresh(string path)
		{
			Document? document = _documentService.GetDocument(path);
			if (document == null)
				return;

			CancellationTokenSource cts = _ctsCache.AddOrUpdate(path, static _ => new CancellationTokenSource(), static (_, ctsOld) => { ctsOld.Cancel(); ctsOld.Dispose(); return new CancellationTokenSource(); });

			await Task.Run(async () =>
			{
				//update must be synchrnonized somehow - more threads can access the shared _diagnosticsItems.
				_diagnosticsItems.RemoveAll(x => x.Path.EqualsOrdinal(path));
				Project project = document.Project;
				string projectName = project.Name;
				Compilation? compilation = await project.GetCompilationAsync(cts.Token);
				if (compilation == null)
					return;
				//Probably need adhoc compilation which validates the requested document only. Of course it needs to handle other documents as additional ones.
				//That needs to get notification when an document gets opened/closed, another document gets added/removed.
				//Let's keep things simple for now.
				ImmutableArray<Diagnostic> diags = compilation.GetDiagnostics();
				if (diags.Length == 0)
					return;
				foreach (Diagnostic dia in diags)
					if (dia.Severity != DiagnosticSeverity.Hidden)
					{
						LinePosition startLinePosition = dia.Location.GetLineSpan().StartLinePosition;
						_diagnosticsItems.Add(new DiagnosticsItem(dia.Id, dia.GetMessage(), (DiagnosticsItemSeverity)(int)dia.Severity - 1, path, startLinePosition.Line, startLinePosition.Character, projectName));
					}
				Updated?.Invoke(null!, EventArgs.Empty);

				_ctsCache.TryRemove(path, out _);
				cts.Dispose();
			}, cts.Token);
		}

		public void Dispose()
		{
			foreach (KeyValuePair<string, CancellationTokenSource> ctsItem in _ctsCache)
				ctsItem.Value.Dispose();
		}

		sealed record DiagnosticsItem(string Code, string Description, DiagnosticsItemSeverity Severity, string Path, int Line, int Character, string ProjectName) : IDiagnosticsItem;
	}
}
