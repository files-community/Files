// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings : BaseJsonSettings
{
	private static readonly Lazy<Settings> lazyDefault = new(() => new Settings(initialize: true));
	public static Settings Default => lazyDefault.Value;

	public Settings()
		: this(initialize: false)
	{
	}

	private Settings(bool initialize)
		: base("settings.json")
	{
		if (initialize)
			Initialize();
	}
}
