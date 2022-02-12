﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Files.Models.JsonSettings;
using Microsoft.Toolkit.Uwp;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;

namespace Files.Services.Implementation
{
    public class UpdateSettingsService : BaseObservableJsonSettingsModel, IUpdateSettingsService
    {
        private StoreContext _storeContext;
        private IList<StorePackageUpdate> _updatePackages;

        private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;

        private bool _isUpdateAvailable;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                _isUpdateAvailable = value;
                OnPropertyChanged(nameof(IsUpdateAvailable));
            }
        }

        private bool _isUpdating;

        public bool IsUpdating
        {
            get => _isUpdating;
            private set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        public UpdateSettingsService()
        {
            _updatePackages = new List<StorePackageUpdate>();
        }

        public void ReportToAppCenter() {}

        public async Task DownloadUpdates()
        {
            OnUpdateInProgress();

            if (!HasUpdates())
            {
                return;
            }

            // double check for Mandatory
            if (IsMandatory)
            {
                // Show dialog
                var dialog = await ShowDialogAsync();
                if (!dialog)
                {
                    // User rejected mandatory update.
                    OnUpdateCancelled();
                    return;
                }
            }

            await DownloadAndInstall();
            OnUpdateCompleted();
        }

        public async Task DownloadMandatoryUpdates()
        {
            // Prompt the user to download if the package list
            // contains mandatory updates.
            if (IsMandatory && HasUpdates())
            {
                if (await ShowDialogAsync())
                {
                    OnUpdateInProgress();
                    await DownloadAndInstall();
                    OnUpdateCompleted();
                }
            }
        }

        public async Task CheckForUpdates()
        {
            await GetUpdatePackages();

            if (_updatePackages is not null && _updatePackages.Count > 0)
            {
                IsUpdateAvailable = true;
            }
        }

        private async Task DownloadAndInstall()
        {
            App.SaveSessionTabs();
            var downloadOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
            await downloadOperation.AsTask();
        }

        private async Task GetUpdatePackages()
        {
            try
            {
                _storeContext ??= await Task.Run(StoreContext.GetDefault);
                var updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                _updatePackages = updateList?.ToList();
            }
            catch (FileNotFoundException)
            {
                // Suppress the FileNotFoundException.
                // GetAppAndOptionalStorePackageUpdatesAsync throws for unknown reasons.
            }
        }

        private static async Task<bool> ShowDialogAsync()
        {
            //TODO: Use IDialogService in future.
            ContentDialog dialog = new()
            {
                Title             = "ConsentDialogTitle".GetLocalized(),
                Content           = "ConsentDialogContent".GetLocalized(),
                CloseButtonText   = "Close".GetLocalized(),
                PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalized()
            };
            ContentDialogResult result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary;
        }

        private bool HasUpdates()
        {
            return _updatePackages is not null && _updatePackages.Count >= 1;
        }

        protected virtual void OnUpdateInProgress()
        {
            IsUpdating = true;
        }

        protected virtual void OnUpdateCompleted()
        {
            IsUpdating = false;
            IsUpdateAvailable = false;
            _updatePackages.Clear();
        }

        protected virtual void OnUpdateCancelled()
        {
            IsUpdating = false;
        }
    }
}
