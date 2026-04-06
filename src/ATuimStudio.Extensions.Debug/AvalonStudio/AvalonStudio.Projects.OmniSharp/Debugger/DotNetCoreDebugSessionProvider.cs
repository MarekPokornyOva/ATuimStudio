using AvalonStudio.Platforms;
using Mono.Debugging.Client;
using Mono.Debugging.ClrDebug;
using System.Text;

namespace AvalonStudio.Debugging.DotNetCore
{
	static class DotNetCoreDebugger
	{
		public static DebuggerSession CreateSession()
		{
			string dbgShimName = "dbgshim";
			if (Platform.PlatformIdentifier != PlatformID.Win32NT)
				dbgShimName = "lib" + dbgShimName;

			var dbgShimPath = $@"{Path.GetDirectoryName(typeof(DotNetCoreDebugger).Assembly.Location)}\runtimes\{(Platform.PlatformIdentifier == PlatformID.Win32NT ? "win" : "linux")}-x64\native\{dbgShimName}{Platform.DLLExtension}";
			if (Platform.PlatformIdentifier != PlatformID.Win32NT)
				dbgShimPath = dbgShimPath.Replace('\\', '/');

			CoreClrDebuggerSession result = new CoreClrDebuggerSession(Path.GetInvalidPathChars(), dbgShimPath)
			{
				CustomSymbolReaderFactory = new PdbSymbolReaderFactory()
			};

			return result;
		}

		public static DebuggerSessionOptions GetDebuggerSessionOptions()
		{
			var evaluationOptions = EvaluationOptions.DefaultOptions.Clone();

			evaluationOptions.EllipsizeStrings = false;
			evaluationOptions.GroupPrivateMembers = false;
			evaluationOptions.EvaluationTimeout = 1000;

			return new DebuggerSessionOptions { EvaluationOptions = evaluationOptions };
		}

		public static DebuggerStartInfo GetDebuggerStartInfo(string executable, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables)
		{
			var startInfo = new DebuggerStartInfo()
			{
				Command = "dotnet" + Platform.ExecutableExtension,
				Arguments = string.IsNullOrEmpty(arguments) ? executable : string.Concat(executable, ' ', arguments),
				WorkingDirectory = Path.GetDirectoryName(executable),
				UseExternalConsole = false,
				CloseExternalConsoleOnExit = true
			};

			if (environmentVariables != null)
				startInfo.EnvironmentVariables.AddRange(environmentVariables);

			return startInfo;
		}
	}
}