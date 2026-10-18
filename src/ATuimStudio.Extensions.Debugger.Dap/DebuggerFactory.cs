using ATuimStudio.Debugger.Dap.Client;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Debug;

namespace ATuimStudio.Extensions.Debugger.Dap
{
	sealed class DebuggerFactory : IDebuggerFactory
	{
		readonly IUserOptions _userOptions;
		public DebuggerFactory(IUserOptions userOptions)
		{
			_userOptions = userOptions;
		}

		public Task<IDebugger> CreateDebuggerAsync(string path, string? arguments, IEnumerable<KeyValuePair<string, string>>? environmentVariables, CancellationToken cancellationToken)
		{
			Task<DapClient> CreateDapClient(CancellationToken cancellationToken)
			{
				string adapterPath = "ATuimDbg";
				string? otherParms = _userOptions.GetBool(UserOptionsCodes.TerminalNoClose) ? "NoClose" : null;

				string interfaceType = _userOptions.GetString(UserOptionsCodes.InterfaceType);
				if (interfaceType.Equals("stdio", StringComparison.OrdinalIgnoreCase))
					return Task.FromResult(DapClient.FromStdIo(adapterPath, otherParms));
				else if (interfaceType.Equals("namedpipe", StringComparison.OrdinalIgnoreCase))
					return DapClient.FromNamedPipeAsync(adapterPath, otherParms, "ATuimDbg" + Guid.NewGuid().ToString("N"), cancellationToken);
				else if (interfaceType.Equals("tcp", StringComparison.OrdinalIgnoreCase))
					return DapClient.FromTcpAsync(adapterPath, otherParms, _userOptions.GetString(UserOptionsCodes.TcpHost), _userOptions.GetInt(UserOptionsCodes.TcpPort), cancellationToken);
				else
					throw new InvalidOperationException("Invalid DAP interface type setting.");
			}

			return Task.FromResult<IDebugger>(new DapClientDebugger(CreateDapClient, path, arguments, environmentVariables));
		}
	}
}
