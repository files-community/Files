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
        public ObservableCollection<LibraryFolder> Folders { get; } = new ObservableCollection<LibraryFolder>();

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
                Folders.Add(new LibraryFolder { Path = folder.Path });
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

        /// <summary>
        /// Tries to save changed properties to file.
        /// </summary>
        /// <returns>Returns true if properties have been saved successfully.</returns>
        public async Task<bool> SaveChangesAsync()
        {
            if (BaseProperties is LibraryProperties props)
            {
                var originalLib = props.Library;

                bool isChanged = false;
                string newDefaultSaveFolder = null;
                string[] newFolders = null;
                bool? newIsPinned = null;

                var defaultSaveFolder = Folders.FirstOrDefault(f => f.IsDefault);
                if (!string.Equals(defaultSaveFolder.Path, originalLib.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase))
                {
                    newDefaultSaveFolder = defaultSaveFolder.Path;
                    isChanged = true;
                }

                if ((originalLib.Folders?.Count ?? 0) != Folders.Count || !originalLib.Folders.SequenceEqual(Folders.Select(f => f.Path), StringComparer.OrdinalIgnoreCase))
                {
                    newFolders = Folders.Select(f => f.Path).ToArray();
                    isChanged = true;
                }

                // TODO: implement isPinned UI and update change here

                if (!isChanged)
                {
                    return true;
                }
                while (true)
                {
                    using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
                    try
                    {
                        var newLib = await LibraryHelper.UpdateLibrary(originalLib.ItemPath, newDefaultSaveFolder, newFolders, newIsPinned);
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