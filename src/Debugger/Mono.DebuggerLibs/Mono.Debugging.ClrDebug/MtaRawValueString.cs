using Mono.Debugging.Backend;

namespace Mono.Debugging.ClrDebug
{
	class MtaRawValueString : IRawValueString, IDebuggerBackendObject
	{
		private readonly IRawValueString source;

		public int Length => MtaThread.Run(() => source.Length);

		public string Value => MtaThread.Run(() => source.Value);

		public MtaRawValueString(IRawValueString s)
		{
			source = s;
		}

		public string Substring(int index, int length)
		{
			return MtaThread.Run(() => source.Substring(index, length));
		}
	}
}
