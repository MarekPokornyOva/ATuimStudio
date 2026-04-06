using Mono.Debugging.Evaluation;
using Mono.Debugging.Client;
using Microsoft.Samples.Debugging.CorDebug;

namespace Mono.Debugging.ClrDebug
{
	public class CorEvaluationContext : EvaluationContext
	{
		private CorEval corEval;

		private CorFrame frame;

		private CorChain activeChain;

		private int frameIndex;

		private int evalTimestamp;

		private readonly CorBacktrace backtrace;

		private CorThread thread;

		private int threadId;

		public CorDebuggerSession Session { get; set; }

		public new CorObjectAdaptor Adapter => (CorObjectAdaptor)base.Adapter;

		public CorThread Thread
		{
			get
			{
				CheckTimestamp();
				if (thread == null)
				{
					thread = Session.GetThread(threadId);
				}
				return thread;
			}
			set
			{
				thread = value;
				threadId = thread.Id;
			}
		}

		public CorChain ActiveChain
		{
			get
			{
				CheckTimestamp();
				if (activeChain == null)
				{
					activeChain = Thread.ActiveChain;
				}
				return activeChain;
			}
		}

		public CorFrame Frame
		{
			get
			{
				CheckTimestamp();
				if (frame == null)
				{
					frame = backtrace.FrameList[frameIndex];
				}
				return frame;
			}
		}

		public CorEval Eval
		{
			get
			{
				CheckTimestamp();
				if (corEval == null)
				{
					corEval = Thread.CreateEval();
				}
				return corEval;
			}
		}

		internal CorEvaluationContext(CorDebuggerSession session, CorBacktrace backtrace, int index, EvaluationOptions ops)
			: base(ops)
		{
			Session = session;
			base.Adapter = session.ObjectAdapter;
			frameIndex = index;
			this.backtrace = backtrace;
			evalTimestamp = CorDebuggerSession.EvaluationTimestamp;
			base.Evaluator = session.GetEvaluator(CorBacktrace.CreateFrame(session, Frame));
		}

		private void CheckTimestamp()
		{
			if (evalTimestamp != CorDebuggerSession.EvaluationTimestamp)
			{
				thread = null;
				frame = null;
				corEval = null;
				activeChain = null;
			}
		}

		public override void CopyFrom(EvaluationContext ctx)
		{
			base.CopyFrom(ctx);
			frame = ((CorEvaluationContext)ctx).frame;
			frameIndex = ((CorEvaluationContext)ctx).frameIndex;
			evalTimestamp = ((CorEvaluationContext)ctx).evalTimestamp;
			Thread = ((CorEvaluationContext)ctx).Thread;
			Session = ((CorEvaluationContext)ctx).Session;
		}

		public override void WriteDebuggerError(Exception ex)
		{
			Session.Frontend.NotifyDebuggerOutput(isStderr: true, ex.Message);
		}

		public override void WriteDebuggerOutput(string message, params object[] values)
		{
			Session.Frontend.NotifyDebuggerOutput(isStderr: false, string.Format(message, values));
		}

		public CorValue RuntimeInvoke(CorFunction function, CorType[] typeArgs, CorValue thisObj, CorValue[] arguments)
		{
			return Session.RuntimeInvoke(this, function, typeArgs, thisObj, arguments);
		}
	}
}
