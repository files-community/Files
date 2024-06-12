// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils
{
	/// <summary>
	/// Represents standard item resides in Recycle Bin on Windows to be shown on UI.
	/// </summary>
	public sealed class StandardRecycleBinItem : StandardStorageItem
	{
		/// <summary>
		/// Gets or sets originally resided path for this removed item placed in Recycle Bin on Windows.
		/// </summary>
		public string OriginalPath { get; set; } = "";

		/// <summary>
		/// Gets humanized string of <see cref="DateDeleted"/>.
		/// </summary>
		public string DateDeletedHumanized { get; private set; } = "";

		/// <summary>
		/// Gets parent path of originally resided path for this removed item placed in Recycle Bin on Windows.
		/// </summary>
		public string OriginalParentFolderPath
			=> SystemIO.Path.IsPathRooted(OriginalPath) ? SystemIO.Path.GetDirectoryName(OriginalPath) ?? "" : OriginalPath;

		/// <summary>
		/// Gets parent folder name of originally resided path for this removed item placed in Recycle Bin on Windows.
		/// </summary>
		public string OriginalParentFolderName
			=> SystemIO.Path.GetFileName(OriginalParentFolderPath);

		private DateTimeOffset _DateDeleted;
		/// <summary>
		/// Gets or sets deleted date.
		/// </summary>
		public DateTimeOffset DateDeleted
		{
			get => _DateDeleted;
			set
			{
				DateDeletedHumanized = dateTimeFormatter.ToShortLabel(value);
				_DateDeleted = value;
			}
		}

		/// <summary>
		/// Initializes an instance of <see cref="StandardRecycleBinItem"/> class.
		/// </summary>
		/// <param name="folderRelativeId"></param>
		public StandardRecycleBinItem(string folderRelativeId) : base()
		{
		}
	}
}
