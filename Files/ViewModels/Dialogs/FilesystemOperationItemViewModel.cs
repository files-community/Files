using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
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

        private bool isConflict;
        public bool IsConflict
        {
            get => isConflict;
            set => SetProperty(ref isConflict, value);
        }

        public string SourceDirectoryDisplayName
        {
            // If the destination path is empty, then show full source path instead
            get => !string.IsNullOrEmpty(DestinationPath) ? System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(SourcePath)) : System.IO.Path.GetDirectoryName(SourcePath);
        }

        public string DestinationDirectoryDisplayName
        {
            get => System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(DestinationPath));
        }

        public string DisplayFileName
        {
            get => string.IsNullOrEmpty(DestinationPath) ? System.IO.Path.GetFileName(SourcePath) : System.IO.Path.GetFileName(DestinationPath);
        }

        public string TakenActionText
        {
            get
            {
                switch (ConflictResolveOption)
                {
                    case FileNameConflictResolveOptionType.GenerateNewName:
                        {
                            return "ConflictingItemsDialogTakenActionGenerateNewName".GetLocalized();
                        }

                    case FileNameConflictResolveOptionType.ReplaceExisting:
                        {
                            return "ConflictingItemsDialogTakenActionReplaceExisting".GetLocalized();
                        }

                    case FileNameConflictResolveOptionType.Skip:
                        {
                            return "ConflictingItemsDialogTakenActionSkip".GetLocalized();
                        }

                    case FileNameConflictResolveOptionType.NotAConflict:
                    default:
                        {
                            return "Not a conflict";
                        }
                }
            }
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

        public Visibility ShowSubFolders
        {
            get => string.IsNullOrEmpty(DestinationPath) || !IsConflict || (!ActionTaken && ConflictResolveOption != FileNameConflictResolveOptionType.NotAConflict) ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ShowResolveOption
        {
            get => ActionTaken && ConflictResolveOption != FileNameConflictResolveOptionType.NotAConflict ? Visibility.Visible : Visibility.Collapsed;
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

        public ICommand OptionGenerateNewNameCommand { get; private set; }

        public ICommand OptionReplaceExistingCommand { get; private set; }

        public ICommand OptionSkipCommand { get; private set; }

        public FilesystemOperationItemViewModel(Action updatePrimaryButtonEnabled, Action optionGenerateNewName, Action optionReplaceExisting, Action optionSkip)
        {
            this.updatePrimaryButtonEnabled = updatePrimaryButtonEnabled;

            GenerateNewNameCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.GenerateNewName));
            ReplaceExistingCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.ReplaceExisting));
            SkipCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.Skip));
            SplitButtonDefaultActionCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.GenerateNewName)); // GenerateNewName is the default action
            UndoTakenActionCommand = new RelayCommand<RoutedEventArgs>((e) => TakeAction(FileNameConflictResolveOptionType.GenerateNewName, false));

            OptionGenerateNewNameCommand = new RelayCommand(optionGenerateNewName);
            OptionReplaceExistingCommand = new RelayCommand(optionReplaceExisting);
            OptionSkipCommand = new RelayCommand(optionSkip);
        }

        public void TakeAction(FileNameConflictResolveOptionType action, bool actionTaken = true)
        {
            if (IsConflict)
            {
                ConflictResolveOption = action;
                ActionTaken = actionTaken;
                ExclamationMarkVisibility = actionTaken ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged(nameof(ShowSubFolders));
                OnPropertyChanged(nameof(ShowResolveOption));
                OnPropertyChanged(nameof(ShowUndoButton));
                OnPropertyChanged(nameof(ShowSplitButton));
                OnPropertyChanged(nameof(TakenActionText));
                this.updatePrimaryButtonEnabled?.Invoke();
            }
        }
    }
}