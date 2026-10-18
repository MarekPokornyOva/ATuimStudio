namespace System.Threading.Tasks
{
	public static class TaskExtensions
	{
		public static void AwaitSync(this Task task, bool continueOnCapturedContext)
			=> task.ConfigureAwait(continueOnCapturedContext).GetAwaiter().GetResult();

		public static T AwaitSync<T>(this Task<T> task, bool continueOnCapturedContext)
			=> task.ConfigureAwait(continueOnCapturedContext).GetAwaiter().GetResult();

		public static void ContinueWith<T>(this Task<T> task, Action<T> continuation)
			=> task.ContinueWith(t => continuation(t.Result), TaskScheduler.Default);
	}
}
