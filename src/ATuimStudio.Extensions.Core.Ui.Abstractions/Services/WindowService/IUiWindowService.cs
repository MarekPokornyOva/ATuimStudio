namespace ATuimStudio.Extensions.Core.Ui
{
	public interface IUiWindowService
	{
		Task<object>? OpenPane(Guid type, string parentId, string id, string title);
	}
}
