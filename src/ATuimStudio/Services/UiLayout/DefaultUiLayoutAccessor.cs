using ATuimStudio.Extensions.Core.Ui;

namespace ATuimStudio
{
	public sealed class DefaultUiLayoutAccessor : IUiLayoutAccessor
	{
		public IUiLayout UiLayout { get; internal set; } = default!;
	}
}
