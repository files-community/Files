// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Factories
{
	public static class AppThemeResourceFactory
	{
		public static ObservableCollection<AppThemeResourceItem> AppThemeResources { get; } =
		[
			new AppThemeResourceItem
			{
				BackgroundColor = "#00000000", /* Transparent */
				Name = Strings.Default.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FFB900", /* #FFB900 */
				Name = Strings.YellowGold.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32F7630C", /* #F7630C */
				Name = Strings.OrangeBright.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32D13438", /* #D13438 */
				Name = Strings.BrickRed.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FF4343", /* #FF4343 */
				Name = Strings.ModRed.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = Strings.Red.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = Strings.RoseBright.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320078D7", /* #0078D7 */
				Name = Strings.Blue.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#328764B8", /* #8764B8 */
				Name = Strings.IrisPastel.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32B146C2", /* #B146C2 */
				Name = Strings.VioletRedLight.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320099BC", /* #0099BC */
				Name = Strings.CoolBlueBright.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B7C3", /* #00B7C3 */
				Name = Strings.Seafoam.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B294", /* #00B294 */
				Name = Strings.MintLight.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327A7574", /* #7A7574 */
				Name = Strings.Gray.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32107C10", /* #107C10 */
				Name = Strings.Green.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32767676", /* #767676 */
				Name = Strings.Overcast.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324C4A48", /* #4C4A48 */
				Name = Strings.Storm.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3269797E", /* #69797E */
				Name = Strings.BlueGray.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324A5459", /* #4A5459 */
				Name = Strings.GrayDark.GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327E735F", /* #7E735F */
				Name = Strings.Camouflage.GetLocalizedResource()
			}
		];
	}
}
