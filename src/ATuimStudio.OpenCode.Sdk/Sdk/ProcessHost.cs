using System.Diagnostics;

namespace ATuimStudio.OpenCode.Sdk
{
	public static class ProcessHost
	{
		public static Process CreateOpenCodeProcess(string path, int port)
		{
			ProcessStartInfo parms = new ProcessStartInfo(path)
			{
				Arguments = "serve" + (port == 0 ? "" : $" --port {port}"),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
			};
			return Process.Start(parms) ?? throw new InvalidOperationException("OpenCode process did not started.");
		}

		public static (string Connection, string Password) ValidateOpenCodeStart(Process process)
		{
			using StreamReader output = process.StandardOutput;

			string? s = output.ReadLine();
			if (s == null || !s.StartsWith("server listening on "))
				throw new InvalidOperationException("OpenCode does not listen.");
			string conn = s[20..];
			s = output.ReadLine();
			if (s == null || !s.StartsWith("server password "))
				throw new InvalidOperationException("OpenCode does not share a password.");

			return (conn, s[16..]);
		}

	}
}
