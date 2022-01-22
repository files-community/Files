using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;

#nullable enable

namespace Files.Backend.ViewModels.ItemListing
{
    public abstract class ListedItemViewModel : ObservableObject
    {
        protected static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

        protected static IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();


    }
}
