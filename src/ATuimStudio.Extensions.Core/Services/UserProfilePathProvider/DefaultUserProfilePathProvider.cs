using System.Text;

namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultUserProfilePathProvider : IUserProfilePathProvider
	{
		readonly ISolutionService _solutionService;
		readonly IUserInfoProvider _userInfoProvider;
		//This should support some FileProvisioning instead of writing directly to disk. That's something to be refactored in whole application.
		public DefaultUserProfilePathProvider(ISolutionService solutionService, IUserInfoProvider userInfoProvider)
		{
			_solutionService = solutionService;
			_userInfoProvider = userInfoProvider;
		}

		string? _userGlobalProfilePath;
		public string GetUserGlobalProfilePath()
			=> _userGlobalProfilePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ATuim", "options.sqlite");

		string? _lastSolutionPath;
		string? _lastUserId;
		string? _lastUserProfileDirName;
		string? _lastUserProfilePath;
		public string GetUserSolutionProfilePath()
		{
			string solutionPath = (_solutionService.CurrentSolution ?? throw new InvalidOperationException("No solution loaded.")).Path;
			if (_lastSolutionPath != solutionPath)
			{
				_lastSolutionPath = solutionPath;

				string userId = _userInfoProvider.GetUserId();
				if (_lastUserId != userId)
				{
					_lastUserId = userId;
					_lastUserProfileDirName = Convert.ToBase64String(Encoding.UTF8.GetBytes(userId));
				}

				_lastUserProfilePath = Path.Combine(solutionPath, ".ATuim", _lastUserProfileDirName!, "options.sqlite");
				EnsureProfileCreated(_lastUserProfilePath, userId);
			}

			return _lastUserProfilePath!;
		}

		static void EnsureProfileCreated(string fullPath, string userId)
		{
			if (Directory.Exists(fullPath))
				return;
			Directory.CreateDirectory(fullPath);
			using (StreamWriter sw = File.CreateText(Path.Combine(fullPath, "userid.txt")))
				sw.Write(userId);
		}
	}
}
