using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

namespace Files.Views
{
    public sealed partial class PropertiesLibrary : PropertiesTab, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<LibraryFolder> Folders { get; } = new ObservableCollection<LibraryFolder>();

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

        private bool isPinned;

        public bool IsPinned
        {
            get => isPinned;
            set
            {
                if (isPinned != value)
                {
                    isPinned = value;
                    NotifyPropertyChanged(nameof(IsPinned));
                }
            }
        }

        public RelayCommand AddLocationCommand => new RelayCommand(AddLocation);

        public RelayCommand SetDefaultLocationCommand => new RelayCommand(SetDefaultLocation);

        public RelayCommand RemoveLocationCommand => new RelayCommand(RemoveLocation);

        public PropertiesLibrary()
        {
            InitializeComponent();
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties is LibraryProperties props)
            {
                Folders.Clear();
                if (!props.Library.IsEmpty)
                {
                    foreach (var path in props.Library.Folders)
                    {
                        Folders.Add(new LibraryFolder
                        {
                            Path = path,
                            IsDefault = string.Equals(path, props.Library.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase),
                        });
                    }
                }
            }
        }

        private async void AddLocation()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Folders.Add(new LibraryFolder { Path = folder.Path });
            }
        }

        private void SetDefaultLocation()
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
        }

        private void RemoveLocation()
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
        }

        private bool IsChanged(LibraryItem lib, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned)
        {
            bool isChanged = false;

            newDefaultSaveFolder = null;
            newFolders = null;
            newIsPinned = null;

            var currentDefaultSaveFolder = Folders.FirstOrDefault(f => f.IsDefault);
            if (!string.Equals(currentDefaultSaveFolder.Path, lib.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase))
            {
                newDefaultSaveFolder = currentDefaultSaveFolder.Path;
                isChanged = true;
            }

            if ((lib.Folders?.Count ?? 0) != Folders.Count || !lib.Folders.SequenceEqual(Folders.Select(f => f.Path), StringComparer.OrdinalIgnoreCase))
            {
                newFolders = Folders.Select(f => f.Path).ToArray();
                isChanged = true;
            }

            if (isPinned != lib.IsPinned)
            {
                newIsPinned = isPinned;
                isChanged = true;
            }

            return isChanged;
        }

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <returns>Returns true if properties have been saved successfully.</returns>
        public async Task<bool> SaveChangesAsync()
        {
            if (BaseProperties is LibraryProperties props)
            {
                if (!IsChanged(props.Library, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned))
                {
                    return true;
                }
                while (true)
                {
                    using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
                    try
                    {
                        var newLib = await LibraryHelper.UpdateLibrary(props.Library.ItemPath, newDefaultSaveFolder, newFolders, newIsPinned);
                        if (newLib != null)
                        {
                            props.UpdateLibrary(new LibraryItem(newLib));
                            return true;
                        }
                        return false;
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
            return false;
        }

        public class LibraryFolder
        {
            public string Path { get; set; }

            public bool IsDefault { get; set; }
        }
    }
}