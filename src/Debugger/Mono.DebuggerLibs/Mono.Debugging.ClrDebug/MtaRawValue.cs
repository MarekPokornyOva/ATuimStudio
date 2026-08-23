using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace Mono.Debugging.ClrDebug
{
	class MtaRawValue : IRawValue, IDebuggerBackendObject
	{
		private readonly IRawValue source;

		public MtaRawValue(IRawValue s)
		{
			source = s;
		}

		public object CallMethod(string name, object[] parameters, EvaluationOptions options)
		{
			return MtaThread.Run(() => source.CallMethod(name, parameters, options));
		}

		public object CallMethod(string name, object[] parameters, out object[] outArgs, EvaluationOptions options)
		{
			object[] tempOutArgs = null;
			object result = MtaThread.Run(() => source.CallMethod(name, parameters, out tempOutArgs, options));
			outArgs = tempOutArgs;
			return result;
		}

		public object GetMemberValue(string name, EvaluationOptions options)
		{
			return MtaThread.Run(() => source.GetMemberValue(name, options));
		}

		public void SetMemberValue(string name, object value, EvaluationOptions options)
		{
			MtaThread.Run(delegate
			{
				source.SetMemberValue(name, value, options);
			});
		}
	}
}
