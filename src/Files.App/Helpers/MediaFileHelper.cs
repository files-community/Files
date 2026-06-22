using TagLib;

namespace Files.App.Helpers
{
	internal sealed class MediaFileHelper
	{
		/// <summary>
		/// Changes the album cover for a given <paramref name="filePath"/>.
		/// </summary>
		/// <param name="filePath">The file path to change the album cover.</param>
		/// <param name="albumCover">The album cover to use, or <c>null</c> to clear any embedded cover.</param>
		public static bool ChangeAlbumCover(string filePath, Picture albumCover)
		{
			try
			{
				File mediaFile = File.Create(filePath);
				mediaFile.Tag.Pictures = albumCover is null ? [] : [albumCover];
				mediaFile.Save();
				mediaFile.Dispose();

				return true;
			}
			catch (Exception)
			{
				return false;
			}

		}
	}
}