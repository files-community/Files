using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesLibrary : PropertiesTab, INotifyPropertyChanged
    {
        public ObservableCollection<LibraryLocation> Folders { get; set; } = new ObservableCollection<LibraryLocation>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int selectedFolderIndex = -1;

        public int SelectedFolderIndex
        {
            get => selectedFolderIndex;
            set
            {
                if (selectedFolderIndex != value)
                {
                    selectedFolderIndex = value;
                    NotifyPropertyChanged(nameof(SelectedFolderIndex));
                    NotifyPropertyChanged(nameof(IsNotDefaultFolderSelected));
                }
            }
        }

        public bool IsNotDefaultFolderSelected => selectedFolderIndex >= 0 && !Folders[selectedFolderIndex].IsDefault;

        public RelayCommand AddLocationCommand => new RelayCommand(async () =>
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Folders.Add(new LibraryLocation { Path = folder.Path });
            }
        });

        public RelayCommand SetDefaultLocationCommand => new RelayCommand(async () =>
        {
            int index = SelectedFolderIndex;
            if (index >= 0)
            {
                foreach (var f in Folders)
                {
                    f.IsDefault = false;
                }
                Folders[index].IsDefault = true;
            }
        });

        public RelayCommand RemoveLocationCommand => new RelayCommand(async () =>
        {
            int index = SelectedFolderIndex;
            if (index >= 0)
            {
                Folders.RemoveAt(index);
                if (index > 0)
                {
                    SelectedFolderIndex = index - 1;
                }
                else if (Folders.Count > 0)
                {
                    SelectedFolderIndex = 0;
                }
            }
        });

        public PropertiesLibrary()
        {
            InitializeComponent();
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties is LibraryProperties props)
            {
                if (props.Item is LibraryItem lib)
                {
                    Folders.Clear();
                    if (lib.Paths != null)
                    {
                        foreach (var path in lib.Paths)
                        {
                            Folders.Add(new LibraryLocation
                            {
                                Path = path,
                                IsDefault = string.Equals(path, lib.DefaultSavePath, StringComparison.InvariantCultureIgnoreCase),
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <returns>Returns true if properties have been saved successfully.</returns>
        public async Task<bool> SaveChangesAsync()
        {
            while (true)
            {
                using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
                try
                {
                    // TODO: send library updates to Shell via FullTrust
                    //await (BaseProperties as FileProperties).SyncPropertyChangesAsync();
                    return true;
                }
                catch
                {
                    // Attempting to open more than one ContentDialog at a time will throw an error
                    if (Interacts.Interaction.IsAnyContentDialogOpen())
                    {
                        return false;
                    }
                    await dialog.ShowAsync();
                    switch (dialog.DynamicResult)
                    {
                        case DynamicDialogResult.Primary:
                            break;

                        case DynamicDialogResult.Secondary:
                            return true;

                        case DynamicDialogResult.Cancel:
                            return false;
                    }
                }
            }
        }

        public class LibraryLocation
        {
            public string Path { get; set; }

            public bool IsDefault { get; set; }
        }
    }
}