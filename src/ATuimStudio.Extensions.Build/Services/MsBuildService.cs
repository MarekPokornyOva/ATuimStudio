using ATuimStudio.Extensions.Core;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;

namespace ATuimStudio.Extensions.Build
{
	sealed class MsBuildService : IBuildService
	{
		readonly ISolutionService _solutionService;
		public MsBuildService(ISolutionService solutionService)
		{
			_solutionService = solutionService;
		}

		public Task<IBuildResult> BuildAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<IBuildResult> BuildProjectAsync(string name, CancellationToken cancellationToken)
		{
			if (_solutionService.CurrentSolution?.RawData is not Solution solution)
				throw new EngineException("SolutionDataTypeUnsupported", "Solution data type is not supported.");

			Microsoft.CodeAnalysis.Project proj = solution.Projects.First(x => x.Name.EqualsOrdinal(name));
			ProjectCollection projectCollection = new ProjectCollection();

			//https://stackoverflow.com/questions/7264682/running-msbuild-programmatically
			List<BuildResultItem> messages = new List<BuildResultItem>();
			Logger logger = new Logger(messages.Add);
			BuildParameters buildParamters = new BuildParameters(projectCollection)
			{
				DisableInProcNode = true,
				Loggers = [logger]
			};
			Dictionary<string, string?> globalProperty = new Dictionary<string, string?>()
			{
				 { "Configuration", "Debug"},
				 { "Platform", "AnyCPU" },
			};
			BuildManager.DefaultBuildManager.ResetCaches();
			BuildRequestData buildRequest = new BuildRequestData(proj.FilePath!, globalProperty, null, ["Restore", "Build"], null);
			Microsoft.Build.Execution.BuildResult buildResult = BuildManager.DefaultBuildManager.Build(buildParamters, buildRequest);

			bool success = buildResult.OverallResult == BuildResultCode.Success;
			return Task.FromResult<IBuildResult>(new BuildResult(success, messages, success ? proj.OutputFilePath : null));
		}

		sealed record BuildResult(bool Success, IReadOnlyCollection<IBuildResultItem> Items, string? OutputPath) : IBuildResult;
		sealed record BuildResultItem(BuildResultItemSeverity Severity, string Code, string Message, BuildResultItemLocation Location) : IBuildResultItem;

		sealed class Logger : ILogger
		{
			readonly Action<BuildResultItem> _messageCallback;

			public LoggerVerbosity Verbosity { get; set; }
			public string? Parameters { get; set; }

			internal Logger(Action<BuildResultItem> messageCallback)
			{
				_messageCallback = messageCallback;
			}

			public void Initialize(IEventSource eventSource)
			{
				void Handle(LazyFormattedBuildEventArgs args, string file, int line, int column, string code, BuildResultItemSeverity severity)
				{
					_messageCallback(new BuildResultItem(severity, code, args.Message ?? "", new BuildResultItemLocation(file, line, column)));
				}

				eventSource.HandleErrorRaised((sender, e) => Handle(e, e.File, e.LineNumber, e.ColumnNumber, e.Code, BuildResultItemSeverity.Error));
				eventSource.HandleWarningRaised((sender, e) => Handle(e, e.File, e.LineNumber, e.ColumnNumber, e.Code, BuildResultItemSeverity.Warning));
				//eventSource.HandleMessageRaised((sender, e) => Handle(e, e.File, e.LineNumber, e.ColumnNumber, e.Code, BuildResultItemSeverity.Info));
			}

			public void Shutdown()
			{
			}
		}
	}
}
