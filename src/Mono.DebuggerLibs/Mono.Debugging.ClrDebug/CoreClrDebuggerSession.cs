using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;
using System.Collections;

namespace Mono.Debugging.ClrDebug
{
	public class CoreClrDebuggerSession : CorDebuggerSession
	{
		private readonly DbgShimInterop dbgShimInterop;

		private static readonly TimeSpan RuntimeLoadTimeout = TimeSpan.FromSeconds(1.0);

		public CoreClrDebuggerSession(char[] badPathChars, string dbgShimPath)
			: base(badPathChars)
		{
			dbgShimInterop = new DbgShimInterop(dbgShimPath);
		}

		protected override void OnRun(DebuggerStartInfo startInfo)
		{
			MtaThread.Run(delegate
			{
				string workingDir = PrepareWorkingDirectory(startInfo);
				Dictionary<string, string> env = PrepareEnvironment(startInfo);
				string command = PrepareCommandLine(startInfo);
				int procId;
				ICorDebug corDebug = CoreClrShimUtil.CreateICorDebugForCommand(dbgShimInterop, command, workingDir, env, RuntimeLoadTimeout, out procId);
				dbg = new CorDebugger(corDebug);
				process = dbg.DebugActiveProcess(procId, win32Attach: false);
				processId = process.Id;
				SetupProcess(process);
				process.Continue(outOfBand: false);
			});
			OnStarted();
		}

		static string PrepareWorkingDirectory(DebuggerStartInfo startInfo)
		{
			string text = startInfo.WorkingDirectory;
			if (string.IsNullOrEmpty(text))
			{
				text = Path.GetDirectoryName(startInfo.Command);
			}
			return text;
		}

		static string PrepareCommandLine(DebuggerStartInfo startInfo)
		{
			return "\"" + startInfo.Command + "\" " + startInfo.Arguments;
		}

		static Dictionary<string, string> PrepareEnvironment(DebuggerStartInfo startInfo)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
			{
				dictionary[(string)environmentVariable.Key] = (string)environmentVariable.Value;
			}
			foreach (KeyValuePair<string, string> environmentVariable2 in startInfo.EnvironmentVariables)
			{
				dictionary[environmentVariable2.Key] = environmentVariable2.Value;
			}
			return dictionary;
		}

		protected override void OnAttachToProcess(long procId)
		{
			AttachToProcessImpl((int)procId);
		}

		protected override void OnAttachToProcess(ProcessInfo processInfo)
		{
			AttachToProcessImpl((int)processInfo.Id);
		}

		private void AttachToProcessImpl(int procId)
		{
			attaching = true;
			MtaThread.Run(delegate
			{
				ICorDebug corDebug = CoreClrShimUtil.CreateICorDebugForProcess(dbgShimInterop, procId, RuntimeLoadTimeout);
				dbg = new CorDebugger(corDebug);
				process = dbg.DebugActiveProcess(procId, win32Attach: false);
				SetupProcess(process);
				process.Continue(outOfBand: false);
			});
			OnStarted();
		}
	}
}
