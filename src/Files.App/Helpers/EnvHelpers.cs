using Files.Backend.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Files.App.Helpers
{
	public static class EnvHelpers
	{
	
		public static (AppEnvironment, string) GetAppEnvironmentAndLogo()
		{
			var env = Package.Current.DisplayName switch
			{
				"Files - Dev" => AppEnvironment.WindowsDev,
				"Files (Preview)" => AppEnvironment.WindowsSideloadPreview,
				_ =>
#if SIDELOAD
					AppEnvironment.WindowsSideload,
#else
					AppEnvironment.WindowsStore,
#endif
			};

			var path = env switch
			{
				AppEnvironment.WindowsDev => Constants.AssetPaths.DevLogo,
				AppEnvironment.WindowsSideloadPreview => Constants.AssetPaths.PreviewLogo,
				_ => Constants.AssetPaths.StableLogo,
			};

			return (env, path);
		}
	}
}
