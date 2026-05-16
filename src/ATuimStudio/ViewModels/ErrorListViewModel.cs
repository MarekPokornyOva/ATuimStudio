using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Threading;
using Microsoft.Extensions.ObjectPool;

namespace ATuimStudio.ViewModels
{
	public sealed partial class ErrorListViewModel : ViewModelBase
	{
		readonly SupressableObservableCollection<DiagnosticsItem> _diagnosticsItems = [];
		readonly ObjectPool<DiagnosticsItem> _diagnosticsItemsPool = ObjectPool.Create<DiagnosticsItem>();

		public FlatTreeDataGridSource<DiagnosticsItem> Source { get; }

		[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CS8618
		public ErrorListViewModel()
		{
			Source = new FlatTreeDataGridSource<DiagnosticsItem>(_diagnosticsItems)
			{
				Columns =
				{
					new TextColumn<DiagnosticsItem, string>("", static x => x.Severity),
					new TextColumn<DiagnosticsItem, string>("Code", static x => x.Code),
					new TextColumn<DiagnosticsItem, string>("Description", static x => x.Description),
					new TextColumn<DiagnosticsItem, string>("Project", static x => x.ProjectName),
					new TextColumn<DiagnosticsItem, string>("File", static x => x.FileName),
					new TextColumn<DiagnosticsItem, int>("Line", static x => x.Line),
				},
			};
		}
#pragma warning restore CS8618

		readonly ICodeDiagnosticsManager _codeDiagnosticsManager;
		public ErrorListViewModel(ICodeDiagnosticsManager codeDiagnosticsManager) : this()
		{
			_codeDiagnosticsManager = codeDiagnosticsManager;

			_codeDiagnosticsManager.Updated += CodeDiagnosticsManager_Updated;
		}

		void CodeDiagnosticsManager_Updated(object? sender, EventArgs e)
		{
			Dispatcher.UIThread.Invoke(() =>
			{
				ObjectPool<DiagnosticsItem> diagnosticsItemsPool = _diagnosticsItemsPool;
				SupressableObservableCollection<DiagnosticsItem> diagnosticsItems = _diagnosticsItems;
				IReadOnlyCollection<IDiagnosticsItem> newItems = _codeDiagnosticsManager.DiagnosticsItems;

				diagnosticsItems.SupressNotification();

				int actualCount = diagnosticsItems.Count;
				int required = newItems.Count;

				int i;
				if (required < actualCount)
					for (i = actualCount - 1; i >= required; i--)
					{
						diagnosticsItemsPool.Return(diagnosticsItems[i]);
						diagnosticsItems.RemoveAt(i);
					}
				else if (required > actualCount)
					for (i = actualCount; i < required; i++)
						diagnosticsItems.Add(diagnosticsItemsPool.Get());

				i = 0;
				foreach (IDiagnosticsItem dia in newItems)
				{
					DiagnosticsItem item = diagnosticsItems[i++];
					item.Severity = dia.Severity.ToString();
					item.Code = dia.Code;
					item.Description = dia.Description;
					item.ProjectName = dia.ProjectName;
					item.FileName = Path.GetFileName(dia.Path);
					item.Line = dia.Line;
				}

				diagnosticsItems.RestoreNotification();
			});
		}

		public class DiagnosticsItem
		{
			public string Severity { get; internal set; } = default!;
			public string Code { get; internal set; } = default!;
			public string Description { get; internal set; } = default!;
			public string ProjectName { get; internal set; } = default!;
			public string FileName { get; internal set; } = default!;
			public int Line { get; internal set; }
		}
	}
}