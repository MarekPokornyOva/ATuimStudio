namespace Mono.Debugging.ClrDebug
{
	static class MtaThread
	{
		//Note: ClrDebug states debuggin has to run in MTA however development experience shows it's not necessary.
		//Uncomment the content if threading problem occurs.

		//private static readonly AutoResetEvent wordDoneEvent = new AutoResetEvent(initialState: false);
		//private static Action workDelegate;
		//private static readonly object workLock = new object();
		//private static Thread workThread;
		//private static Exception workError;
		//private static readonly object threadLock = new object();

		public static R Run<R>(Func<R> ts, int timeout = 15000)
		{
			//if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
			//{
				return ts();
			//}
			//R res = default(R);
			//Run(delegate
			//{
			//	res = ts();
			//}, timeout);
			//return res;
		}

		public static void Run(Action ts, int timeout = 15000)
		{
			//if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
			//{
				ts();
			//	return;
			//}
			//lock (workLock)
			//{
			//	lock (threadLock)
			//	{
			//		workDelegate = ts;
			//		workError = null;
			//		if (workThread == null)
			//		{
			//			workThread = new Thread(MtaRunner);
			//			workThread.Name = "Win32 Debugger MTA Thread";
			//			workThread.IsBackground = true;
			//			workThread.Start();
			//		}
			//		else
			//		{
			//			Monitor.Pulse(threadLock);
			//		}
			//	}
			//	if (!wordDoneEvent.WaitOne(timeout))
			//	{
			//		//workThread.Abort();
			//		throw new Exception("Debugger operation timeout on MTA thread.");
			//	}
			//}
			//if (workError == null)
			//{
			//	return;
			//}
			//throw workError;
		}

		//private static void MtaRunner()
		//{
		//	try
		//	{
		//		lock (threadLock)
		//		{
		//			do
		//			{
		//				try
		//				{
		//					workDelegate();
		//				}
		//				catch (ThreadAbortException)
		//				{
		//					break;
		//				}
		//				catch (Exception ex2)
		//				{
		//					workError = ex2;
		//				}
		//				finally
		//				{
		//					workDelegate = null;
		//				}
		//				wordDoneEvent.Set();
		//			}
		//			while (Monitor.Wait(threadLock, 60000));
		//		}
		//	}
		//	catch
		//	{
		//	}
		//	finally
		//	{
		//		workThread = null;
		//	}
		//}
	}
}
