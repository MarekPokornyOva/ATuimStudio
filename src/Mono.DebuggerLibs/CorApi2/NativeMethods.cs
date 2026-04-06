using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

static class NativeMethods
{
	public enum ProcessAccessOptions
	{
		PROCESS_TERMINATE = 1,
		PROCESS_CREATE_THREAD = 2,
		PROCESS_SET_SESSIONID = 4,
		PROCESS_VM_OPERATION = 8,
		PROCESS_VM_READ = 0x10,
		PROCESS_VM_WRITE = 0x20,
		PROCESS_DUP_HANDLE = 0x40,
		PROCESS_CREATE_PROCESS = 0x80,
		PROCESS_SET_QUOTA = 0x100,
		PROCESS_SET_INFORMATION = 0x200,
		PROCESS_QUERY_INFORMATION = 0x400,
		PROCESS_SUSPEND_RESUME = 0x800,
		SYNCHRONIZE = 0x100000
	}

	private const string Kernel32LibraryName = "kernel32.dll";

	private const string Ole32LibraryName = "ole32.dll";

	private const string ShimLibraryName = "mscoree.dll";

	public static Guid CLSID_CLRMetaHost = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");

	public static Guid IID_ICLRMetaHost = new Guid("D332DB9E-B9B3-4125-8207-A14884F53216");

	public static Guid IIDICorDebug = new Guid("3d6f5f61-7538-11d3-8d5b-00104b35e7ef");

	[DllImport("kernel32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	public static extern bool CloseHandle(IntPtr handle);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern ICorDebug CreateDebuggingInterfaceFromVersion(int iDebuggerVersion, string szDebuggeeVersion);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
	public static extern int GetCORVersion([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, int cchBuffer, out int dwLength);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern void GetVersionFromProcess(ProcessSafeHandle hProcess, StringBuilder versionString, int bufferSize, out int dwLength);

	[DllImport("mscoree.dll", CharSet = CharSet.Auto, PreserveSig = false, SetLastError = true)]
	public static extern void CLRCreateInstance(ref Guid clsid, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out ICLRMetaHost metahostInterface);

	[DllImport("mscoree.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	public static extern void GetRequestedRuntimeVersion(string pExe, StringBuilder pVersion, int cchBuffer, out int dwLength);

	[DllImport("kernel32.dll")]
	public static extern ProcessSafeHandle OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("ole32.dll", PreserveSig = false)]
	public static extern void CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, int dwClsContext, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out ICorDebug debuggingInterface);
}
