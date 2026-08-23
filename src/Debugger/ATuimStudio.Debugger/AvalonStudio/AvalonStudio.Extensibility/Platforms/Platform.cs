using System.Runtime.InteropServices;

#nullable disable

namespace AvalonStudio.Platforms
{
	public static class Platform
	{
		public static string DLLExtension
		{
			get
			{
				switch (PlatformIdentifier)
				{
					case PlatformID.Unix:
					return ".so";

					case PlatformID.MacOSX:
					return ".dylib";

					case PlatformID.Win32NT:
					return ".dll";

					default:
					throw new NotImplementedException("Not implemented for your platform.");
				}
			}
		}

		public static string ExecutableExtension
		{
			get
			{
				switch (Platform.PlatformIdentifier)
				{
					case PlatformID.Unix:
					case PlatformID.MacOSX:
					{
						return string.Empty;
					}

					case PlatformID.Win32NT:
					{
						return ".exe";
					}

					default:
					throw new NotImplementedException("Not implemented for your platform.");
				}
			}
		}

		public static PlatformID PlatformIdentifier
		{
			get
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					return PlatformID.Win32NT;
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					return PlatformID.Unix;
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					return PlatformID.MacOSX;
				}

				throw new Exception("Unknow platform");
			}
		}
	}
}