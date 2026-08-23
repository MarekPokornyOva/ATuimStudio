using System.Diagnostics.SymbolStore;

namespace Mono.Debugging.ClrDebug
{
	public interface ICustomCorSymbolReaderFactory
	{
		ISymbolReader CreateCustomSymbolReader(string assemblyInfo);
	}
}
