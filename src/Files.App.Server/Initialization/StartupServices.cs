// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal static class StartupServices
{
	internal static Task InitializeAsync() => InitializationCache.InitializeAsync();
}
