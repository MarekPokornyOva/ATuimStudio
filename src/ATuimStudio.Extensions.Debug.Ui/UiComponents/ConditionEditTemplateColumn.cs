using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace ATuimStudio.UiComponents
{
	public class ConditionEditTemplateColumn<TModel> : TemplateColumn<TModel>
	{
		readonly Func<TModel, bool> _canEditCondition;
		readonly Func<Control, IDataTemplate> _getCellTemplate;
		readonly Func<Control, IDataTemplate>? _getEditingCellTemplate;
		public ConditionEditTemplateColumn(Func<TModel, bool> canEditCondition, object? header, object cellTemplateResourceKey, object? cellEditingTemplateResourceKey = null, GridLength? width = null, TemplateColumnOptions<TModel>? options = null) : base(header, cellTemplateResourceKey, cellEditingTemplateResourceKey, width, options)
		{
			_canEditCondition = canEditCondition;

			static Func<Control, IDataTemplate>? GetTemplate(Type type, string name, TemplateColumn<TModel> instance)
				=> (Func<Control, IDataTemplate>?)type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(instance);

			Type baseType = typeof(TemplateColumn<TModel>);
			_getCellTemplate = GetTemplate(baseType, nameof(_getCellTemplate), this)!;
			_getEditingCellTemplate = GetTemplate(baseType, nameof(_getEditingCellTemplate), this);
		}

		public override ICell CreateCell(IRow<TModel> row)
			=> new TemplateCell(
					row.Model,
					_getCellTemplate,
					_canEditCondition(row.Model)
						? _getEditingCellTemplate
						: null,
					Options);
		
	}
}
