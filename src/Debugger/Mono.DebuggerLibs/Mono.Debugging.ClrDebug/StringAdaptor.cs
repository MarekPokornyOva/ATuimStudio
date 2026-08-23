using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.ClrDebug
{
	public class StringAdaptor : IStringAdaptor
	{
		private readonly CorEvaluationContext ctx;

		private readonly CorStringValue str;

		private readonly CorValRef obj;

		public int Length => str.Length;

		public string Value => str.String;

		public StringAdaptor(EvaluationContext ctx, CorValRef obj, CorStringValue str)
		{
			this.ctx = (CorEvaluationContext)ctx;
			this.str = str;
			this.obj = obj;
		}

		public string Substring(int index, int length)
		{
			return str.String.Substring(index, length);
		}
	}
}
