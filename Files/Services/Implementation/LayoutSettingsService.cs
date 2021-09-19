using Files.Enums;
using Files.Models.JsonSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Services.Implementation
{
    public sealed class LayoutSettingsService : BaseJsonSettingsModel, ILayoutSettingsService
    {
        public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public bool ShowDateColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowDateCreatedColumn
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowTypeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowSizeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFileTagColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public int DefaultGridViewSize
        {
            get => Get(Constants.Browser.GridViewBrowser.GridViewSizeSmall);
            set => Set(value);
        }

        public FolderLayoutModes DefaultLayoutMode
        {
            get => Get(FolderLayoutModes.DetailsView);
            set => Set(value);
        }

        public SortDirection DefaultDirectorySortDirection
        {
            get => Get(SortDirection.Ascending);
            set => Set(value);
        }

        public SortOption DefaultDirectorySortOption
        {
            get => Get(SortOption.Name);
            set => Set(value);
        }

        public GroupOption DefaultDirectoryGroupOption
        {
            get => Get(GroupOption.None);
            set => Set(value);
        }
    }
}
