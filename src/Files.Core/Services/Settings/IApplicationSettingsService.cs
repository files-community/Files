namespace Files.Core.Services.Settings
{
	public interface IApplicationSettingsService : IBaseSettingsService
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not the user clicked to review the app.
		/// </summary>
		bool ClickedToReviewApp { get; set; }

	}
}
