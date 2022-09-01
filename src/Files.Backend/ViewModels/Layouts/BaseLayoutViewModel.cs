using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;

namespace Files.Backend.ViewModels.Layouts
{
    public abstract class BaseLayoutViewModel : ObservableObject
    {
        protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
    }
}
