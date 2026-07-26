using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Core.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATuimStudio.Extensions.TextEditReferences;

sealed partial class AllReferencesViewModel : ObservableObject, IDisposable
{
	[ObservableProperty]
	IReadOnlyCollection<ReferenceItem> _references = [];

	readonly ISub<IAllReferencesResult> _subReferences;
	readonly ISub<IFindImplementationsResult> _subImplementations;
	readonly IUiDocumentService _documentService;
	public AllReferencesViewModel(ISub<IAllReferencesResult> subReferences, ISub<IFindImplementationsResult> subImplementations, IUiDocumentService documentService)
	{
		_subReferences = subReferences;
		_subImplementations = subImplementations;
		_documentService = documentService;

		subReferences.Register(SetReferences);
		subImplementations.Register(SetImplementations);
	}

	void SetReferences(IAllReferencesResult references)
		=> References = references.References;

	void SetImplementations(IFindImplementationsResult implementations)
		=> References = implementations.Implementations;

	[RelayCommand]
	void RowDoubleClick(ReferenceItem parameter)
	{
		_documentService.SetActiveDocument(parameter.FilePath, parameter.Position);
	}

	public void Dispose()
	{
		_subReferences.Unregister(SetReferences);
		_subImplementations.Unregister(SetImplementations);
	}
}
