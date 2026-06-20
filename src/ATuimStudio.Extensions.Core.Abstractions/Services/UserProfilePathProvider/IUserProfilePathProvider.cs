namespace ATuimStudio.Extensions.Core
{
	public interface IUserProfilePathProvider
	{
		string GetUserSolutionProfilePath();
		string GetUserGlobalProfilePath();
	}
}
