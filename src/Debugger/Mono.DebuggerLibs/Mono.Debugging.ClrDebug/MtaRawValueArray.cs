
using Mono.Debugging.Backend;

namespace Mono.Debugging.ClrDebug
{
	class MtaRawValueArray : IRawValueArray, IDebuggerBackendObject
	{
		private readonly IRawValueArray source;

		public int[] Dimensions => MtaThread.Run(() => source.Dimensions);

		public MtaRawValueArray(IRawValueArray s)
		{
			source = s;
		}

		public Array GetValues(int[] index, int count)
		{
			return MtaThread.Run(() => source.GetValues(index, count));
		}

		public object GetValue(int[] index)
		{
			return MtaThread.Run(() => source.GetValue(index));
		}

		public void SetValue(int[] index, object value)
		{
			MtaThread.Run(delegate
			{
				source.SetValue(index, value);
			});
		}

		public Array ToArray()
		{
			return MtaThread.Run(() => source.ToArray());
		}
	}
}
