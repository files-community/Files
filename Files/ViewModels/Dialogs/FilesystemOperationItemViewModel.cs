using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject, IFilesystemOperationItemModel
    {
        private readonly Action updatePrimaryButtonEnabled;

        public string OperationIconGlyph { get; set; }

        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public string SourceDirectoryDisplayName
        {
            get => System.IO.Path.GetDirectoryName(SourcePath);
        }

        public string DestinationDirectoryDisplayName
        {
            get => System.IO.Path.GetDirectoryName(DestinationPath);
        }

        public string DisplayFileName
        {
            get => System.IO.Path.GetFileName(SourcePath);
        }

        public Visibility DestinationLocationVisibility
        {
            get => string.IsNullOrEmpty(DestinationPath) ? Visibility.Collapsed : Visibility.Visible;
        }

        private Visibility exclamationMarkVisibility = Visibility.Collapsed;

        public Visibility ExclamationMarkVisibility
        {
            get => exclamationMarkVisibility;
            set => SetProperty(ref exclamationMarkVisibility, value);
        }

        public FileNameConflictResolveOptionType ConflictResolveOption { get; set; }

        private bool actionTaken = false;
        public bool ActionTaken
        {
            get => actionTaken;
            set => SetProperty(ref actionTaken, value);
        }

        public Visibility ShowUndoButton
        {
            get => ActionTaken && ConflictResolveOption != FileNameConflictResolveOptionType.NotAConflict ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ShowSplitButton
        {
            get => !ActionTaken && ConflictResolveOption != FileNameConflictResolveOptionType.NotAConflict ? Visibility.Visible : Visibility.Collapsed;
        }

        public FilesystemOperationType ItemOperation { get; set; }

        public ICommand GenerateNewNameCommand { get; private set; }

        public ICommand ReplaceExistingCommand { get; private set; }

        public ICommand SkipCommand { get; private set; }

        public ICommand SplitButtonDefaultActionCommand { get; private set; }

        public ICommand UndoTakenActionCommand { get; private set; }

        public FilesystemOperationItemViewModel(Action updatePrimaryButtonEnabled)
        {
            this.updatePrimaryButtonEnabled = updatePrimaryButtonEnabled;

            GenerateNewNameCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.GenerateNewName));
            ReplaceExistingCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.ReplaceExisting));
            SkipCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.Skip));
            SplitButtonDefaultActionCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.GenerateNewName)); // GenerateNewName is the default action
            UndoTakenActionCommand = new RelayCommand<RoutedEventArgs>((e) =>
            {
                ActionTaken = false;
                updatePrimaryButtonEnabled?.Invoke();
                OnPropertyChanged(nameof(ShowUndoButton));
                OnPropertyChanged(nameof(ShowSplitButton));
            });
        }

        public void TakeAction(FileNameConflictResolveOptionType action)
        {
            ConflictResolveOption = action;
            ActionTaken = true;
            OnPropertyChanged(nameof(ShowUndoButton));
            OnPropertyChanged(nameof(ShowSplitButton));
            updatePrimaryButtonEnabled?.Invoke();
        }
    }
}