// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
