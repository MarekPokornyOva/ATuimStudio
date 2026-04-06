using ATuimStudio.Extensions.Build;
using ATuimStudio.Extensions.Core;
using ATuimStudio.Extensions.Debug;
using Microsoft.Extensions.DependencyInjection;

ServiceProvider sp = new ServiceCollection()
	.AddExtensionsCore()
	.AddBuild()
	.AddDebug()
	.BuildServiceProvider();
using (sp)
{
	CancellationToken cancellationToken = CancellationToken.None;

	Console.WriteLine("Loading solution...");
	ISolutionService solutionService = sp.GetRequiredService<ISolutionService>();
	await solutionService.LoadSolutionAsync(SettingsHelper.GetSolutionFile(), cancellationToken);
	//ISolutionData solution = solutionService.CurrentSolution!;
	Console.WriteLine("Solution loaded.");
	
	Console.WriteLine("Building project...");
	IBuildResult buildResult = await sp.GetRequiredService<IBuildService>()
		.BuildProjectAsync(SettingsHelper.GetProjectToBuild(), cancellationToken);
	Console.WriteLine("Project built.");
	if (!buildResult.Success)
		throw new InvalidOperationException("Build failed.");

	Console.WriteLine("Starting debug...");
	using (IDebugger debugger = await sp.GetRequiredService<IDebuggerService>()
		.CreateDebuggerAsync(buildResult.OutputPath!, null, null, cancellationToken))
		//.StartAsync("dotnet", [buildResult.OutputPath!], cancellationToken))
	{
		debugger.OnStandardOut += (_, text) => Console.WriteLine(text);
		debugger.StandardInput.WriteLine();
	
		debugger.OnStarted += (sender, e) =>
		{
			debugger.ToggleBreakpoint(SettingsHelper.GetToDebugProgramCs(), new SourcePosition(7, 1));
			((IDebugger)sender!).Continue();
		};
		debugger.OnBreakpoint += (sender, e) =>
		{
			IStackFrame frame = e.CallStack[0];
			Console.WriteLine($"{frame.SourceFilePath}::{frame.Range!.Value.Start.Line}x{frame.Range!.Value.Start.Column}");
			((IDebugger)sender!).Continue();
		};

		debugger.Start();

		ManualResetEvent mre = new ManualResetEvent(false);
		debugger.OnTerminated += (sender, e) => mre.Set();
		mre.WaitOne();
	}
	Console.WriteLine("Done.");
}

static class SettingsHelper
{
	internal static string GetSolutionFile()
		=> throw new NotImplementedException("No solution file configured.");

	internal static string GetProjectToBuild()
		=> throw new NotImplementedException("No project to build configured.");

	internal static string GetToDebugProgramCs()
		=> throw new NotImplementedException("No file to debug configured.");
}