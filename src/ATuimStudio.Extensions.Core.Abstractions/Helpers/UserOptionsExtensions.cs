namespace ATuimStudio.Extensions.Core
{
	public static class UserOptionsExtensions
	{
		public static void SetValues(this IUserOptions userOptions, Action<IUserOptionsEdit> setter)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			setter(edit);
			edit.Apply();
		}

		public static void SetValue(this IUserOptions userOptions, string key, string value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
		public static void SetValue(this IUserOptions userOptions, string key, int value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
		public static void SetValue(this IUserOptions userOptions, string key, long value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
		public static void SetValue(this IUserOptions userOptions, string key, double value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
		public static void SetValue(this IUserOptions userOptions, string key, bool value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
		public static void SetValue(this IUserOptions userOptions, string key, byte[] value)
		{
			IUserOptionsEdit edit = userOptions.GetEdit();
			edit.SetValue(key, value);
			edit.Apply();
		}
	}
}
