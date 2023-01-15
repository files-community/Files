using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.UI;

namespace Files.App.Views.SettingsPages.Appearance
{
	public static class AppThemeResourceFactory
	{
		public static ObservableCollection<AppThemeResource> AppThemeResources { get; } = new ObservableCollection<AppThemeResource>()
		{
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(0, 0, 0, 0), /* Transparent */
				PreviewColor = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), /* Transparent */
				Name = "Default"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 255, 185, 0), /* #FFB900 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)), /* #FFB900 */
				Name = "Yellow Gold"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 255, 140, 0), /* #FF8C00 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)), /* #FF8C00 */
				Name = "Gold"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 247, 99, 12), /* #F7630C */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 247, 99, 12)), /* #F7630C */
				Name = "Orange Bright"
			},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 202, 80, 16), /* #CA5010 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(255, 202, 80, 16)), /* #CA5010 */
			//	Name = "Orange Dark"
			//},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 218, 59, 1), /* #DA3B01 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 218, 59, 1)), /* #DA3B01 */
				Name = "Rust",
			},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 239, 105, 80), /* #EF6950 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(255, 239, 105, 80)), /* #EF6950 */
			//	Name = "Pale Rust"
			//},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 209, 52, 56), /* #D13438 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 209, 52, 56)), /* #D13438 */
				Name = "Brick Red"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 255, 67, 67), /* #FF4343 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 255, 67, 67)), /* #FF4343 */
				Name = "Mod Red"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 231, 72, 86), /* #E74856 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 231, 72, 86)), /* #E74856 */
				Name = "Pale Red"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 232, 17, 35), /* #E81123 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(100, 232, 17, 35)), /* #E81123 */
				Name = "Red"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 234, 0, 94), /* #EA005E */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 234, 0, 94)), /* #EA005E */
				Name = "Rose Bright"
			},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 195, 0, 82), /* #C30052 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(255, 195, 0, 82)), /* #C30052 */
			//	Name = "Rose"
			//},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 227, 0, 140), /* #E3008C */
				PreviewColor = new SolidColorBrush(Color.FromArgb(100, 227, 0, 140)), /* #E3008C */
				Name = "Plum Light"
			},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 191, 0, 119), /* #BF0077 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(100, 191, 0, 119)), /* #BF0077 */
			//	Name = "Plum"
			//},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 194, 57, 179), /* #C239B3 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(255, 194, 57, 179)), /* #C239B3 */
			//	Name = "Orchid Light"
			//},
			//new AppThemeResource
			//{
			//	BackgroundColor = Color.FromArgb(50, 154, 0, 137), /* #9A0089 */
			//	PreviewColor = new SolidColorBrush(Color.FromArgb(255, 154, 0, 137)), /* #9A0089 */
			//	Name = "Orchid"
			//},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 120, 215), /* #0078D7 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)), /* #0078D7 */
				Name = "Blue"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 99, 177), /* #0063B1 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 99, 177)), /* #0063B1 */
				Name = "Navy Blue"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 142, 140, 216), /* #8E8CD8 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 142, 140, 216)), /* #8E8CD8 */
				Name = "Purple Shaddow"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 107, 105, 214), /* #6B69D6 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 107, 105, 214)), /* #6B69D6 */
				Name = "Purple Shaddow Dark"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 135, 100, 184), /* #8764B8 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 135, 100, 184)), /* #8764B8 */
				Name = "Iris Pastel"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 116, 77, 169), /* #744DA9 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 116, 77, 169)), /* #744DA9 */
				Name = "Iris Spring"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 177, 70, 194), /* #B146C2 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 177, 70, 194)), /* #B146C2 */
				Name = "Violet Red Light"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 136, 23, 152), /* #881798 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 136, 23, 152)), /* #881798 */
				Name = "Violet Red"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 153, 188), /* #0099BC */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 153, 188)), /* #0099BC */
				Name = "Cool Blue Bright"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 45, 125, 154), /* #2D7D9A */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 45, 125, 154)), /* #2D7D9A */
				Name = "Cool Blue"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 183, 195), /* #00B7C3 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 183, 195)), /* #00B7C3 */
				Name = "Seafoam"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 3, 131, 135), /* #038387 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 3, 131, 135)), /* #038387 */
				Name = "Seafoam Teal"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 178, 148), /* #00B294 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 178, 148)), /* #00B294 */
				Name = "Mint Light"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 1, 133, 116), /* #018574 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 1, 133, 116)), /* #018574 */
				Name = "Mint Dark"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 0, 204, 106), /* #00CC6A */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 0, 204, 106)), /* #00CC6A */
				Name = "Turf Green"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 16, 137, 62), /* #10893E */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 16, 137, 62)), /* #10893E */
				Name = "Sport Green"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 122, 117, 116), /* #7A7574 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 122, 117, 116)), /* #7A7574 */
				Name = "Gray"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 93, 90, 80), /* #5D5A58 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 93, 90, 80)), /* #5D5A58 */
				Name = "Gray Brown"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 104, 118, 138), /* #68768A */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 104, 118, 138)), /* #68768A */
				Name = "Steel Blue"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 81, 92, 107), /* #515C6B */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 81, 92, 107)), /* #515C6B */
				Name = "Metal Blue"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 86, 124, 115), /* #567C73 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 86, 124, 115)), /* #567C73 */
				Name = "Pale Moss"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 72, 104, 96), /* #486860 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 72, 104, 96)), /* #486860 */
				Name = "Moss"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 73, 130, 5), /* #498205 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 73, 130, 5)), /* #498205 */
				Name = "Meadow Green"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 16, 124, 16), /* #107C10 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16)), /* #107C10 */
				Name = "Green"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 118, 118, 118), /* #767676 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 118, 118, 118)), /* #767676 */
				Name = "Overcast"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 76, 74, 72), /* #4C4A48 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 76, 74, 72)), /* #4C4A48 */
				Name = "Storm"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 105, 121, 126), /* #69797E */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 105, 121, 126)), /* #69797E */
				Name = "Blue Gray"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 74, 84, 89), /* #4A5459 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 74, 84, 89)), /* #4A5459 */
				Name = "Gray Dark"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 100, 124, 100), /* #647C64 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 100, 124, 100)), /* #647C64 */
				Name = "Liddy Green"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 82, 94, 84), /* #525E54 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 82, 94, 84)), /* #525E54 */
				Name = "Sage"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 132, 117, 69), /* #847545 */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 132, 117, 69)), /* #847545 */
				Name = "Camouflage Desert"
			},
			new AppThemeResource
			{
				BackgroundColor = Color.FromArgb(50, 126, 115, 95), /* #7E735F */
				PreviewColor = new SolidColorBrush(Color.FromArgb(255, 126, 115, 95)), /* #7E735F */
				Name = "Camouflage"
			}
		};
	}
}