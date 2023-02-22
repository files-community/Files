using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Core.Services.Settings;

namespace Files.Core.ViewModels.Layouts
{
	public abstract class BaseLayoutViewModel : ObservableObject
	{
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
	}
}
