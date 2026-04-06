using ClrDebug;
using System.Runtime.InteropServices;

namespace Mono.Debugging.ClrDebug
{
	public sealed class DbgShimInterop : DbgShim
	{
		//readonly DbgShim _dbgshim;
		public DbgShimInterop(string dbgShimPath) : base(NativeLibrary.Load(dbgShimPath))
		{
			//var dbgshim = new DbgShim(NativeLibrary.Load("dbgshim", typeof(DebuggerHost).Assembly, null));
		}
	}
}
