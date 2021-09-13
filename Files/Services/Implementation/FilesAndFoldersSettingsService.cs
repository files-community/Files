using Files.Models.JsonSettings;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Services.Implementation
{
    public class FilesAndFoldersSettingsService : BaseJsonSettingsModel, IFilesAndFoldersSettingsService
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public FilesAndFoldersSettingsService()
        {
            // Initialize settings
            ISettingsSharingContext context = this.UserSettingsService.GetContext();
            this.RegisterSettingsContext(context);
        }

        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreHiddenItemsVisible
        {
            get => Get(false);
            set => Set(value);
        }

        public bool AreSystemItemsHidden
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get => Get(false);
            set => Set(value);
        }

        public bool OpenItemsWithOneclick
        {
            get => Get(false);
            set => Set(value);
        }

        public bool SearchUnindexedItems
        {
            get => Get(false);
            set => Set(value);
        }

        public bool AreLayoutPreferencesPerFolder
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AdaptiveLayoutEnabled
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreFileTagsEnabled
        {
            get => Get(false);
            set => Set(value);
        }
    }
}
