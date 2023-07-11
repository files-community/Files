namespace Files.Core.Services
{
	/// <summary>
	/// A service that interacts with common app-related APIs.
	/// </summary>
	public interface IApplicationService
	{
		/// <summary>
		/// Gets the application build environment.
		/// </summary>
		AppEnvironment Environment { get; }

		/// <summary>
		/// Gets the version of the app.
		/// </summary>
		Version AppVersion { get; }

		/// <summary>
		/// Gets the path at which the App Logo is located.
		/// </summary>
		[Obsolete("This is a bad way of accessing the logo. Use something more abstract instead, and ideally move it out of this interface.")]
		string AppIcoPath { get; }
	}
}
