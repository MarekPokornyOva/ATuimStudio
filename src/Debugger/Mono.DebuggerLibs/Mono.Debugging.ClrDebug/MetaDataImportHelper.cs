using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;

namespace Mono.Debugging.ClrDebug
{
	static class MetaDataImportHelper
	{
		internal static IMetaDataImport CreateMetaDataImport(this CorModule managedModule)
			=> //GetMetaDataInterface<IMetaDataImport> provided implementation doesn't work well on Linux
				/*Environment.OSVersion.Platform != PlatformID.Win32NT
					? new ATuimMetaDataImport(managedModule.Name)
					: */managedModule.GetMetaDataInterface<IMetaDataImport>();
	}
}
