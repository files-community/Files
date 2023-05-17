// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

// System
global using global::System;
global using global::System.Collections;
global using global::System.Collections.Generic;
global using global::System.Collections.ObjectModel;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::System.ComponentModel;
global using global::System.Diagnostics;
global using SystemIO = global::System.IO;

// Windows Community Toolkit
global using global::CommunityToolkit.Mvvm.ComponentModel;
global using global::CommunityToolkit.Mvvm.DependencyInjection;
global using global::CommunityToolkit.Mvvm.Input;
global using global::CommunityToolkit.Mvvm.Messaging;

// Files Front-end
global using global::Files.App.Helpers;
global using global::Files.App.Extensions;
global using global::Files.App.Filesystem;
global using global::Files.App.Filesystem.Cloud;
global using global::Files.App.Filesystem.Search;
global using global::Files.App.Filesystem.StorageEnumerators;
global using global::Files.App.Helpers;
global using global::Files.App.Helpers.FileListCache;
global using global::Files.App.Data.EventArguments;
global using global::Files.App.Data.Factories;
global using global::Files.App.Data.Items;
global using global::Files.App.Data.Models;
global using global::Files.App.Data.Parameters;
global using global::Files.App.Interacts;
global using global::Files.App.Shell;
global using global::Files.App.Storage.FtpStorage;
global using global::Files.App.UserControls;
global using global::Files.App.ViewModels;
global using global::Files.App.ViewModels.Previews;
global using global::Files.App.ViewModels.UserControls;
global using global::Files.App.Views;
global using global::Files.App.Views.LayoutModes;
global using global::Files.App.Views.Shells;

// Files Back-end
global using global::Files.Backend.CommandLine;
global using global::Files.Backend.Enums;
global using global::Files.Backend.Helpers;
global using global::Files.Backend.Services.Settings;
global using global::Files.Backend.Services;
global using global::Files.Backend.ViewModels.Dialogs;
global using global::Files.Shared;
global using global::Files.Shared.Cloud;
global using global::Files.Shared.Enums;
global using global::Files.Shared.EventArguments;
global using global::Files.Shared.Extensions;
global using global::Files.Shared.Services;
