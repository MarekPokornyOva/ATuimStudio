namespace ATuimStudio.OpenCode.Sdk
{
	public interface ISession
	{
		string Id { get; }
		string Title { get; }

		#region Events
		// ---- Assistant text ----
		event EventHandler<SessionTextStartedEventArgs>? TextStarted;
		event EventHandler<SessionTextDeltaEventArgs>? TextDelta;
		event EventHandler<SessionTextEndedEventArgs>? TextEnded;

		// ---- Reasoning ----
		event EventHandler<SessionReasoningStartedEventArgs>? ReasoningStarted;
		event EventHandler<SessionReasoningDeltaEventArgs>? ReasoningDelta;
		event EventHandler<SessionReasoningEndedEventArgs>? ReasoningEnded;

		// ---- Tool calls ----
		event EventHandler<SessionToolCalledEventArgs>? ToolCalled;

		// ---- Steps ----
		event EventHandler<SessionStepStartedEventArgs>? StepStarted;
		event EventHandler<SessionStepEndedEventArgs>? StepEnded;

		// ---- Execution lifecycle ----
		event EventHandler<SessionExecutionStartedEventArgs>? ExecutionStarted;
		event EventHandler<SessionExecutionSucceededEventArgs>? ExecutionSucceeded;
		event EventHandler<SessionExecutionFailedEventArgs>? ExecutionFailed;

		// ---- Status ----
		event EventHandler<SessionStatusEventArgs>? StatusChanged;
		#endregion Events
	}
}
