// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public static class AppThemeResourceFactory
	{
		public static ObservableCollection<AppThemeResourceItem> AppThemeResources { get; } = new ObservableCollection<AppThemeResourceItem>()
		{
			new AppThemeResourceItem
			{
				BackgroundColor = "#00000000", /* Transparent */
				Name = "Default"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FFB900", /* #FFB900 */
				Name = "Yellow Gold"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32F7630C", /* #F7630C */
				Name = "Orange Bright"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32D13438", /* #D13438 */
				Name = "Brick Red"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FF4343", /* #FF4343 */
				Name = "Mod Red"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = "Red"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = "Rose Bright"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320078D7", /* #0078D7 */
				Name = "Blue"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#328764B8", /* #8764B8 */
				Name = "Iris Pastel"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32B146C2", /* #B146C2 */
				Name = "Violet Red Light"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320099BC", /* #0099BC */
				Name = "Cool Blue Bright"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B7C3", /* #00B7C3 */
				Name = "Seafoam"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B294", /* #00B294 */
				Name = "Mint Light"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327A7574", /* #7A7574 */
				Name = "Gray"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32107C10", /* #107C10 */
				Name = "Green"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32767676", /* #767676 */
				Name = "Overcast"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324C4A48", /* #4C4A48 */
				Name = "Storm"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3269797E", /* #69797E */
				Name = "Blue Gray"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324A5459", /* #4A5459 */
				Name = "Gray Dark"
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327E735F", /* #7E735F */
				Name = "Camouflage"
			}
		};
	}
}
