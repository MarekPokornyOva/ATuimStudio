using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Reflection;

namespace Mono.Debugging.ClrDebug
{
	class CorMethodCall : AsyncOperation
	{
		public delegate void CallCallback();
		public delegate string DescriptionCallback();

		public CallCallback OnInvoke;
		public CallCallback OnAbort;
		public DescriptionCallback OnGetDescription;

		public ManualResetEvent DoneEvent = new ManualResetEvent(false);

		public override string Description
		{
			get { return OnGetDescription(); }
		}

		public CorMethodCall(CorEvaluationContext ctx) : base(ctx)
		{
		}

		public override void Invoke()
		{
			OnInvoke();
		}

		public override void Abort()
		{
			OnAbort();
		}

		public override void Shutdown()
		{
			try
			{
				Abort();
			}
			catch
			{
			}
			DoneEvent.Set();
		}

		public override bool WaitForCompleted(int timeout)
		{
			return DoneEvent.WaitOne(timeout, false);
		}
	}
}