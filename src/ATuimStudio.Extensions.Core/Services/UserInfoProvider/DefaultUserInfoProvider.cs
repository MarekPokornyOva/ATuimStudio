namespace ATuimStudio.Extensions.Core
{
	sealed class DefaultUserInfoProvider : IUserInfoProvider
	{
		readonly string _userId;
		public DefaultUserInfoProvider()
		{
			_userId = $"{Environment.UserDomainName}\\{Environment.UserName}";
		}

		public string GetUserId()
			=> _userId;
	}
}
