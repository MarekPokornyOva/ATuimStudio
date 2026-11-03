using ATuimStudio.OpenCode.Client;
using ATuimStudio.OpenCode.Client.Api.Session;
using ATuimStudio.OpenCode.Client.Api.Session.Item;
using ATuimStudio.OpenCode.Client.Api.Session.Item.Prompt;
using Microsoft.Kiota.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ATuimStudio.OpenCode.Sdk
{
	public sealed class Connection : IAsyncDisposable
	{
		readonly OpenCodeClient _client;

		/// <summary>
		/// Fires for every event received from the SSE stream, regardless of the session it belongs to.
		/// Use this as a catch-all for event types that are not exposed as typed session events.
		/// </summary>
		public event EventHandler<OpenCodeEvent>? EventReceived;

		public Connection(IRequestAdapter requestAdapter)
		{
			//NSwag
			//https://baundev.com/posts/api-clients-kiota/
			_client = new OpenCodeClient(requestAdapter);
		}

		#region Events
		readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
		readonly Lock _startLock = new Lock();
		Task? _readerTask;
		CancellationTokenSource? _readerCts;

		/// <summary>
		/// Starts a background task that reads the SSE event stream and dispatches events to sessions.
		///
		/// The event stream is volatile by contract: a slow consumer overflows and fails the stream, and
		/// events during disconnection are missed. The reader reconnects automatically after a short pause.
		/// Calling this while a reader is already running is a no-op.
		/// </summary>
		/// <param name="cancellationToken">Token used to cancel the background reader.</param>
		public void StartEventReader(CancellationToken cancellationToken = default)
		{
			lock (_startLock)
			{
				if (_readerTask is not null && !_readerTask.IsCompleted)
					return; // already running

				_readerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				_readerTask = Task.Run(async () =>
				{
					while (!_readerCts.Token.IsCancellationRequested)
					{
						Stream? stream = null;
						try
						{
							stream = await _client.Api.Event.GetAsync(cancellationToken: _readerCts.Token)
								.ConfigureAwait(false);
							if (stream is null)
								break;

							await using (stream.ConfigureAwait(false))
							{
								await foreach (OpenCodeEvent evt in SseEventReader.ReadAsync(stream, _readerCts.Token))
								{
									EventReceived?.Invoke(this, evt);
									DispatchEvent(evt);
								}
							}
						}
						catch (OperationCanceledException)
						{
							break;
						}
						catch
						{
							// Transient failure or stream closed; reconnect after a pause.
						}

						try { await Task.Delay(1_000, _readerCts.Token).ConfigureAwait(false); }
						catch (OperationCanceledException) { break; }
					}
				}, _readerCts.Token);
			}
		}

		/// <summary>
		/// Stops the background event reader and waits for it to finish.
		/// </summary>
		public async Task StopEventReaderAsync()
		{
			CancellationTokenSource? cts = Interlocked.Exchange(ref _readerCts, null);
			if (cts is null)
				return;

			cts.Cancel();
			Task? task = Interlocked.Exchange(ref _readerTask, null);
			if (task is not null)
			{
				try { await task.ConfigureAwait(false); }
				catch { /* reader faults are handled internally */ }
			}
			cts.Dispose();
		}
		#endregion Events

		public async ValueTask DisposeAsync()
		{
			await StopEventReaderAsync().ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}

		public async Task<IReadOnlyCollection<IProvider>> GetProvidersAsync(CancellationToken cancellationToken = default)
		{
			List<Client.Models.Provider.Info>? resp = await LoopAttempts(async () => (await _client.Api.Provider.GetAsProviderGetResponseAsync(cancellationToken: cancellationToken))?.Data, cancellationToken);
			return resp == null || resp.Count == 0
				? []
				: new MappingReadOnlyCollection<Client.Models.Provider.Info, IProvider>(resp, static x => new Provider(x.Id!, x.Name!));
		}

		public async Task<IReadOnlyCollection<IModel>> GetModelsAsync(CancellationToken cancellationToken)
		{
			List<Client.Models.Model.Info>? resp = await LoopAttempts(async () => (await _client.Api.Model.GetAsModelGetResponseAsync(cancellationToken: cancellationToken))?.Data, cancellationToken);
			return resp == null || resp.Count == 0
				? []
				: new MappingReadOnlyCollection<Client.Models.Model.Info, IModel>(resp, static x => new Model(x.ProviderID!, x.ModelID!, x.Name!, x.Enabled ?? false));
		}

		static async Task<List<TResult>?> LoopAttempts<TResult>(Func<Task<List<TResult>?>> taskFactory, CancellationToken cancellationToken)
		{
			int attempt = 10;
			int delayMs = 200;
			while (attempt > 0)
			{
				List<TResult>? resp = await taskFactory();
				if (resp == null)
					return resp;
				if (resp.Count != 0)
					return resp;
				attempt--;
				if (attempt > 0)
				{
					await Task.Delay(delayMs, cancellationToken);
					delayMs *= 2;
				}
			}
			return null;
		}

		public async Task<ISession> CreateSessionAsync(IModel model, string directory, CancellationToken cancellationToken)
		{
			Client.Models.Session.Info resp = (await _client.Api.Session.PostAsSessionPostResponseAsync(new SessionPostRequestBody { Model = new Client.Models.Model.Ref { Id = model.Id, ProviderID = model.ProviderId }, Location = new Client.Models.Location.PublicRef { Directory = directory } }, cancellationToken: cancellationToken))?.Data
				?? throw new InvalidOperationException("Session not created.");
			return MapSession(resp);
		}

		public async Task<IReadOnlyCollection<ISession>> GetSessions(string directory, CancellationToken cancellationToken)
		{
			List<Client.Models.Session.Info>? resp = (await _client.Api.Session.GetAsync(x => x.QueryParameters.Directory = directory, cancellationToken))?.Data;
			if (resp == null)
				return [];
			return resp == null || resp.Count == 0
				? []
				: new MappingReadOnlyCollection<Client.Models.Session.Info, ISession>(resp, MapSession);
		}

		public Task DeleteSession(ISession session, CancellationToken cancellationToken)
			=> _client.Api.Session[session.Id].DeleteAsync(cancellationToken: cancellationToken);

		Session MapSession(Client.Models.Session.Info info)
		{
			string id = info.Id!;
			Session session = new Session(info.Id!, info.Title ?? id);
			_sessions[session.Id] = session;
			return session;
		}

		public async Task<IPrompt> SendPromptAsync(ISession session, string text, CancellationToken cancellationToken)
		{
			WithSessionItemRequestBuilder sessionReqBuilder = _client.Api.Session[session.Id];
			Client.Models.Session.Inbox.User resp = (await sessionReqBuilder.Prompt.PostAsPromptPostResponseAsync(new PromptPostRequestBody { Text = text }, cancellationToken: cancellationToken))?.Data
				?? throw new InvalidOperationException("Prompt not created.");
			return new Prompt(resp.Id!);
		}

		void DispatchEvent(OpenCodeEvent evt)
		{
			string? sessionId = GetSessionId(evt);
			if (sessionId is null || !_sessions.TryGetValue(sessionId, out var session))
				return;
			session.OnEvent(evt);
		}

		static string? GetSessionId(OpenCodeEvent evt) => evt switch
		{
			SessionStatusEvent s => s.Data.SessionId,
			SessionTextStartedEvent t => t.Data.SessionId,
			SessionTextDeltaEvent t => t.Data.SessionId,
			SessionTextEndedEvent t => t.Data.SessionId,
			SessionReasoningStartedEvent r => r.Data.SessionId,
			SessionReasoningDeltaEvent r => r.Data.SessionId,
			SessionReasoningEndedEvent r => r.Data.SessionId,
			SessionToolCalledEvent t => t.Data.SessionId,
			SessionStepStartedEvent s => s.Data.SessionId,
			SessionStepEndedEvent s => s.Data.SessionId,
			SessionExecutionStartedEvent s => s.Data.SessionId,
			SessionExecutionSucceededEvent s => s.Data.SessionId,
			SessionExecutionFailedEvent s => s.Data.SessionId,
			_ => null
		};

		sealed record Provider(string Id, string Name) : IProvider;

		sealed record Model(string ProviderId, string Id, string Name, bool Enabled) : IModel;

		sealed record Session(string Id, string Title) : ISession
		{
			// ---- Assistant text ----
			public event EventHandler<SessionTextStartedEventArgs>? TextStarted;
			public event EventHandler<SessionTextDeltaEventArgs>? TextDelta;
			public event EventHandler<SessionTextEndedEventArgs>? TextEnded;

			// ---- Reasoning ----
			public event EventHandler<SessionReasoningStartedEventArgs>? ReasoningStarted;
			public event EventHandler<SessionReasoningDeltaEventArgs>? ReasoningDelta;
			public event EventHandler<SessionReasoningEndedEventArgs>? ReasoningEnded;

			// ---- Tool calls ----
			public event EventHandler<SessionToolCalledEventArgs>? ToolCalled;

			// ---- Steps ----
			public event EventHandler<SessionStepStartedEventArgs>? StepStarted;
			public event EventHandler<SessionStepEndedEventArgs>? StepEnded;

			// ---- Execution lifecycle ----
			public event EventHandler<SessionExecutionStartedEventArgs>? ExecutionStarted;
			public event EventHandler<SessionExecutionSucceededEventArgs>? ExecutionSucceeded;
			public event EventHandler<SessionExecutionFailedEventArgs>? ExecutionFailed;

			// ---- Status ----
			public event EventHandler<SessionStatusEventArgs>? StatusChanged;

			internal void OnEvent(OpenCodeEvent evt)
			{
				switch (evt)
				{
					case SessionTextStartedEvent t:
						TextStarted?.Invoke(this, new SessionTextStartedEventArgs(t));
						break;
					case SessionTextDeltaEvent t:
						TextDelta?.Invoke(this, new SessionTextDeltaEventArgs(t));
						break;
					case SessionTextEndedEvent t:
						TextEnded?.Invoke(this, new SessionTextEndedEventArgs(t));
						break;
					case SessionReasoningStartedEvent r:
						ReasoningStarted?.Invoke(this, new SessionReasoningStartedEventArgs(r));
						break;
					case SessionReasoningDeltaEvent r:
						ReasoningDelta?.Invoke(this, new SessionReasoningDeltaEventArgs(r));
						break;
					case SessionReasoningEndedEvent r:
						ReasoningEnded?.Invoke(this, new SessionReasoningEndedEventArgs(r));
						break;
					case SessionToolCalledEvent t:
						ToolCalled?.Invoke(this, new SessionToolCalledEventArgs(t));
						break;
					case SessionStepStartedEvent s:
						StepStarted?.Invoke(this, new SessionStepStartedEventArgs(s));
						break;
					case SessionStepEndedEvent s:
						StepEnded?.Invoke(this, new SessionStepEndedEventArgs(s));
						break;
					case SessionExecutionStartedEvent s:
						ExecutionStarted?.Invoke(this, new SessionExecutionStartedEventArgs(s));
						break;
					case SessionExecutionSucceededEvent s:
						ExecutionSucceeded?.Invoke(this, new SessionExecutionSucceededEventArgs(s));
						break;
					case SessionExecutionFailedEvent s:
						ExecutionFailed?.Invoke(this, new SessionExecutionFailedEventArgs(s));
						break;
					case SessionStatusEvent s:
						StatusChanged?.Invoke(this, new SessionStatusEventArgs(s));
						break;
				}
			}
		}

		record Prompt(string Id) : IPrompt;
	}

	// ========================================================================
	// Flattened, consumer-friendly event args for known event types.
	// ========================================================================

	public sealed class SessionTextStartedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }

		internal SessionTextStartedEventArgs(SessionTextStartedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
		}
	}

	public sealed class SessionTextDeltaEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public int Ordinal { get; }
		public string Delta { get; }

		internal SessionTextDeltaEventArgs(SessionTextDeltaEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Ordinal = evt.Data.Ordinal;
			Delta = evt.Data.Delta;
		}
	}

	public sealed class SessionTextEndedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public int Ordinal { get; }
		public string? Text { get; }

		internal SessionTextEndedEventArgs(SessionTextEndedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Ordinal = evt.Data.Ordinal;
			Text = evt.Data.Text;
		}
	}

	public sealed class SessionReasoningStartedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public int Ordinal { get; }
		public string? Text { get; }

		internal SessionReasoningStartedEventArgs(SessionReasoningStartedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Ordinal = evt.Data.Ordinal;
			Text = evt.Data.Text;
		}
	}

	public sealed class SessionReasoningDeltaEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public int Ordinal { get; }
		public string Delta { get; }

		internal SessionReasoningDeltaEventArgs(SessionReasoningDeltaEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Ordinal = evt.Data.Ordinal;
			Delta = evt.Data.Delta;
		}
	}

	public sealed class SessionReasoningEndedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public int Ordinal { get; }
		public string? Text { get; }

		internal SessionReasoningEndedEventArgs(SessionReasoningEndedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Ordinal = evt.Data.Ordinal;
			Text = evt.Data.Text;
		}
	}

	public sealed class SessionToolCalledEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public string ToolId { get; }
		public IReadOnlyDictionary<string, JsonElement>? Input { get; }
		public bool Executed { get; }

		internal SessionToolCalledEventArgs(SessionToolCalledEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			ToolId = evt.Data.ToolId;
			Input = evt.Data.Input is { Count: > 0 } ? new Dictionary<string, JsonElement>(evt.Data.Input) : null;
			Executed = evt.Data.Executed;
		}
	}

	public sealed class SessionStepStartedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public string Agent { get; }
		public string? ModelProviderId { get; }
		public string? ModelId { get; }
		public long Started { get; }

		internal SessionStepStartedEventArgs(SessionStepStartedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Agent = evt.Data.Agent;
			ModelProviderId = evt.Data.Model?.ProviderId;
			ModelId = evt.Data.Model?.ModelId;
			Started = evt.Data.Started;
		}
	}

	public sealed class SessionStepEndedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string AssistantMessageId { get; }
		public string? Finish { get; }
		public double? Cost { get; }

		internal SessionStepEndedEventArgs(SessionStepEndedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			AssistantMessageId = evt.Data.AssistantMessageId;
			Finish = evt.Data.Finish;
			Cost = evt.Data.Cost;
		}
	}

	public sealed class SessionExecutionStartedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }

		internal SessionExecutionStartedEventArgs(SessionExecutionStartedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
		}
	}

	public sealed class SessionExecutionSucceededEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }

		internal SessionExecutionSucceededEventArgs(SessionExecutionSucceededEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
		}
	}

	public sealed class SessionExecutionFailedEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public JsonElement Error { get; }

		internal SessionExecutionFailedEventArgs(SessionExecutionFailedEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			Error = evt.Data.Error;
		}
	}

	public sealed class SessionStatusEventArgs : EventArgs
	{
		public OpenCodeEvent SourceEvent { get; }
		public string SessionId { get; }
		public string StatusKind { get; }
		public int? Attempt { get; }
		public string? Message { get; }
		public long? Next { get; }

		internal SessionStatusEventArgs(SessionStatusEvent evt)
		{
			SourceEvent = evt;
			SessionId = evt.Data.SessionId;
			StatusKind = evt.Data.Status.Kind;
			Attempt = evt.Data.Status.Attempt;
			Message = evt.Data.Status.Message;
			Next = evt.Data.Status.Next;
		}
	}
}
