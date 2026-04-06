using ClrDebug;
using Microsoft.Samples.Debugging.Extensions;
using System.Runtime.InteropServices;

namespace Mono.Debugging.ClrDebug
{
	internal class CoreClrShimUtil
	{
		public unsafe static ICorDebug CreateICorDebugForCommand(DbgShimInterop dbgShimInterop, string command, string workingDir, IDictionary<string, string> env, TimeSpan runtimeLoadTimeout, out int procId)
		{
			IntPtr lpEnvironment = IntPtr.Zero;
			try
			{
				lpEnvironment = DebuggerExtensions.SetupEnvironment(env);
				var hResults = dbgShimInterop.CreateProcessForLaunch(command, bSuspendProcess: true, lpEnvironment, workingDir);
				int num = hResults.ProcessId;
				if (num == 0)
				{
					throw new InvalidOperationException("Can't create process.");
				}
				procId = num;
				return CreateICorDebugImpl(dbgShimInterop, num, runtimeLoadTimeout, hResults.ResumeHandle);
			}
			finally
			{
				if (lpEnvironment != IntPtr.Zero)
				{
					DebuggerExtensions.TearDownEnvironment(lpEnvironment);
				}
			}
		}

		public static ICorDebug CreateICorDebugForProcess(DbgShimInterop dbgShimInterop, int processId, TimeSpan runtimeLoadTimeout)
		{
			return CreateICorDebugImpl(dbgShimInterop, processId, runtimeLoadTimeout, nint.Zero);
		}

		private static ICorDebug CreateICorDebugImpl(DbgShimInterop dbgShimInterop, int processId, TimeSpan runtimeLoadTimeout, nint resumeHandle)
		{
			ManualResetEvent waiter = new ManualResetEvent(initialState: false);
			ICorDebug corDebug = null;
			Exception callbackException = null;
			RuntimeStartupCallback runtimeStartupCallback = (CorDebug pCordb, nint parameter, HRESULT hr) =>
			{
				try
				{
					if (hr < 0)
						Marshal.ThrowExceptionForHR((int)hr);
					corDebug = pCordb.Raw;
				}
				catch (Exception ex)
				{
					callbackException = ex;
				}
				waiter.Set();
			};
			nint hResults = dbgShimInterop.RegisterForRuntimeStartup(processId, runtimeStartupCallback, 0);
			if (hResults == 0)
			{
				throw new DebugException($"Failed call RegisterForRuntimeStartup: {hResults}", (HRESULT)hResults);
			}
			if (resumeHandle != nint.Zero)
			{
				dbgShimInterop.ResumeProcess(resumeHandle);
			}
			//if (!waiter.WaitOne(runtimeLoadTimeout))
			//{
			//	throw new TimeoutException($".NET core load awaiting timed out for {runtimeLoadTimeout}");
			//}
			waiter.WaitOne();
			GC.KeepAlive(runtimeStartupCallback);
			if (callbackException != null)
			{
				throw callbackException;
			}
			return corDebug;
		}
	}
}
