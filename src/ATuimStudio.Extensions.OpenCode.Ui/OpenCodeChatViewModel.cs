using ATuimStudio.Extensions.Core;
using ATuimStudio.OpenCode.Sdk;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ATuimStudio.Extensions.OpenCode;

sealed partial class OpenCodeChatViewModel : ObservableObject, IDisposable
{
	private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:4096/") };
	private Connection? _connection;
	private Process? _openCodeProcess;
	private CancellationTokenSource? _cts;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
	[NotifyCanExecuteChangedFor(nameof(NewSessionCommand))]
	public partial bool IsAvailable { get; set; }

	[ObservableProperty]
	public partial bool IsExecuted { get; set; }

	[ObservableProperty]
	public partial string MdText { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
	public partial string? Prompt { get; set; }

	public ObservableCollection<IModel> AvailableModels { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(NewSessionCommand))]
	public partial IModel? SelectedModel { get; set; }

	/// <summary>
	/// Currently selected session in the list (bound to the ListBox). Setting this also
	/// refreshes <see cref="DeleteSessionCommand"/>'s CanExecute.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DeleteSessionCommand))]
	public partial ISession? SelectedSession { get; set; }

	/// <summary>
	/// All known sessions: loaded via <see cref="Connection.GetSessions"/> plus any created
	/// by the user through <see cref="CreateSessionCommand"/>.
	/// </summary>
	public ObservableCollection<ISession> Sessions { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
	public partial bool IsModelSelectionEnabled { get; set; } = true;

	readonly ISolutionService _solutionService;
	public OpenCodeChatViewModel(ISolutionService solutionService, IUserOptionsManager userOptionsManager)
	{
		_solutionService = solutionService;

		_ = InitializeAsync(
			solutionService.CurrentSolution,
			userOptionsManager.GetString(UserOptionsCodes.ServerPath),
			userOptionsManager.GetInt(UserOptionsCodes.ServerPort),
			userOptionsManager.GetString(UserOptionsCodes.PreferedProvider),
			userOptionsManager.GetString(UserOptionsCodes.PreferedModel)
			);

		solutionService.OnSolutionLoaded += (sender, e) => SolutionChanged(e.Solution);
		solutionService.OnSolutionUnloaded += (sender, e) => SolutionChanged(null);
	}

	private async Task InitializeAsync(ISolutionData? solutionData, string openCodePath, int openCodePort, string preferedProvider, string preferedModel)
	{
		try
		{
			// All blocking process/IO work runs off the UI thread.
			await Task.Run(async () =>
			{
				// ---- Start OpenCode process ----
				_openCodeProcess = ProcessHost.CreateOpenCodeProcess(openCodePath, openCodePort);
				(string connection, string password) = ProcessHost.ValidateOpenCodeStart(_openCodeProcess);

				// ---- Build the SDK connection ----
				var requestAdapter = new DefaultRequestAdapter(new BasicAuthenticationProvider("opencode", password), httpClient: _httpClient);
				_connection = new Connection(requestAdapter);
				_connection.EventReceived += OnEventReceived;
				_connection.StartEventReader();

				_cts = new CancellationTokenSource();

				await _connection.GetProvidersAsync(_cts.Token);
				// ---- Load available models ----
				var models = await _connection.GetModelsAsync(_cts.Token);
				Dispatcher.UIThread.Post(() =>
				{
					AvailableModels.AddRange(models);
					SelectedModel = (preferedProvider == "" || preferedModel == ""
							? null
							: models.FirstOrDefault(x => x.ProviderId == preferedProvider && x.Id == preferedModel))
						?? models.FirstOrDefault();
				});

				_initialized = true;
				SolutionChanged(solutionData);

				Dispatcher.UIThread.Post(() => IsAvailable = true);
			});
		}
		catch
		{
			Dispatcher.UIThread.Post(() => IsAvailable = false);
		}
	}

	bool _initialized;
	void SolutionChanged(ISolutionData? solutionData)
	{
		if (!_initialized)
			return;

		async Task LoadAsync()
		{
			IReadOnlyCollection<ISession> sessions;
			if (solutionData == null)
				sessions = [];
			else
				// Load existing sessions
				try
				{
					sessions = await _connection!.GetSessions(solutionData.Path, CancellationToken.None);
				}
				catch
				{
					sessions = [];
				}
			Dispatcher.UIThread.Post(() =>
			{
				Sessions.Clear();
				Sessions.AddRange(sessions);
			});

		}
		_ = LoadAsync();
	}

	private void OnEventReceived(object? sender, OpenCodeEvent e)
	{
		// Connection-level event handler (kept for future diagnostics).
	}

	private void OnSessionTextEnded(object? sender, SessionTextEndedEventArgs e)
	{
		if (!string.IsNullOrEmpty(e.Text))
			Dispatcher.UIThread.Post(() =>
				MdText += $"{Environment.NewLine}{e.Text}{Environment.NewLine}"
			);
	}

	void OnSessionExecutionStarted(object? sender, SessionExecutionStartedEventArgs e)
		=> SetIsExecuted(true);

	void OnSessionExecutionSucceeded(object? sender, SessionExecutionSucceededEventArgs e)
		=> SetIsExecuted(false);

	void OnSessionExecutionFailed(object? sender, SessionExecutionFailedEventArgs e)
		=> SetIsExecuted(false);

	void SetIsExecuted(bool isExecuted)
		=> Dispatcher.UIThread.Post(() => IsExecuted = isExecuted);

	[RelayCommand]
	async Task NewSession()
	{
		SelectedSession = null;
	}

	[RelayCommand]
	async Task DeleteSession()
	{
		if (_connection is null || _cts is null || SelectedSession is null)
			return;

		await _connection.DeleteSession(SelectedSession, _cts.Token);

		Dispatcher.UIThread.Post(() =>
			Sessions.Remove(SelectedSession)
		);
	}

	[RelayCommand(CanExecute = nameof(CanSubmit))]
	private async Task Submit(string? text)
	{
		ISolutionData? currentSolution = _solutionService.CurrentSolution;

		if (!CanSubmit(text) || _connection is null || _cts is null || currentSolution == null)
			return;

		string userText = text!;

		ISession? session = SelectedSession;
		// Create the session on the first prompt, using the currently selected model.
		if (session is null)
		{
			if (SelectedModel is null)
				return;

			session = await _connection.CreateSessionAsync(SelectedModel, currentSolution.Path, _cts.Token);
			session.TextEnded += OnSessionTextEnded;
			session.ExecutionStarted += OnSessionExecutionStarted;
			session.ExecutionSucceeded += OnSessionExecutionSucceeded;
			session.ExecutionFailed += OnSessionExecutionFailed;

			Dispatcher.UIThread.Post(() =>
			{
				MdText += $"**Session model:** {SelectedModel.ProviderId}:{SelectedModel.Name}{Environment.NewLine}{Environment.NewLine}";
				IsModelSelectionEnabled = false;
				Sessions.Add(session);
				SelectedSession = session;
			});
		}

		await _connection.SendPromptAsync(session, userText, _cts.Token);

		Dispatcher.UIThread.Post(() =>
		{
			MdText += Environment.NewLine + "**You:** " + userText + Environment.NewLine + "====" + Environment.NewLine;
			Prompt = string.Empty;
		});
	}

	private bool CanSubmit(string? text)
		=> IsAvailable && !string.IsNullOrWhiteSpace(text);

	public void Dispose()
	{
		try { _cts?.Cancel(); } catch { }
		try { _connection?.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
		try { _openCodeProcess?.Kill(true); } catch { }
		try { _openCodeProcess?.Dispose(); } catch { }
		try { _httpClient.Dispose(); } catch { }
	}
}
