using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface IReleaseNotesService : INotifyPropertyChanged
	{
		/// <summary>
		/// Release notes for the latest release
		/// </summary>
		string? ReleaseNotes { get; }

		/// <summary>
		/// Gets a value indicating if release notes are available
		/// </summary>
		bool IsReleaseNotesAvailable { get; }

		/// <summary>
		/// Downloads release notes for the latest release
		/// </summary>
		Task DownloadReleaseNotes();
	}
}
