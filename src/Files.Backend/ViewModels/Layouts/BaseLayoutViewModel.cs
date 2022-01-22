using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
