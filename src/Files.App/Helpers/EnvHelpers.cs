using Files.Backend.Enums;
using Microsoft.Extensions.Hosting;
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


			var env =
#if STORE
				AppEnvironment.Store;
#elif PREVIEW
				AppEnvironment.Preview;
#elif STABLE
				AppEnvironment.Stable;
#else
				AppEnvironment.Dev;
#endif

			var path = env switch
			{
				AppEnvironment.Dev => Constants.AssetPaths.DevLogo,
				AppEnvironment.Preview => Constants.AssetPaths.PreviewLogo,
				_ => Constants.AssetPaths.StableLogo,
			};

			return (env, path);
		}
	}
}
