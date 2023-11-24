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

// CommunityToolkit.Mvvm
global using global::CommunityToolkit.Mvvm.ComponentModel;
global using global::CommunityToolkit.Mvvm.DependencyInjection;
global using global::CommunityToolkit.Mvvm.Input;
global using global::CommunityToolkit.Mvvm.Messaging;

// Files.App
global using global::Files.App.Helpers;
global using global::Files.App.Extensions;
global using global::Files.App.Utils;
global using global::Files.App.Utils.Archives;
global using global::Files.App.Utils.Cloud;
global using global::Files.App.Utils.FileTags;
global using global::Files.App.Utils.Git;
global using global::Files.App.Utils.Library;
global using global::Files.App.Utils.RecentItem;
global using global::Files.App.Utils.RecycleBin;
global using global::Files.App.Utils.Serialization;
global using global::Files.App.Utils.Shell;
global using global::Files.App.Utils.StatusCenter;
global using global::Files.App.Utils.Storage;
global using global::Files.App.Data.Attributes;
global using global::Files.App.Data.Behaviors;
global using global::Files.App.Data.Commands;
global using global::Files.App.Data.Contexts;
global using global::Files.App.Data.EventArguments;
global using global::Files.App.Data.Exceptions;
global using global::Files.App.Data.Factories;
global using global::Files.App.Data.Items;
global using global::Files.App.Data.Models;
global using global::Files.App.Data.Parameters;
global using global::Files.App.Data.TemplateSelectors;
global using global::Files.App.Services;
global using global::Files.App.UserControls;
global using global::Files.App.UserControls.TabBar;
global using global::Files.App.ViewModels;
global using global::Files.App.ViewModels.UserControls;
global using global::Files.App.Views;
global using global::Files.App.Views.Layouts;
global using global::Files.App.Views.Shells;

// Files.Core
global using global::Files.Core.Data.Enums;
global using global::Files.Core.Data.EventArguments;
global using global::Files.Core.Data.Items;
global using global::Files.Core.Data.Messages;
global using global::Files.Core.Data.Models;
global using global::Files.Core.Extensions;
global using global::Files.Core.Helpers;
global using global::Files.Core.Services;
global using global::Files.Core.Services.DateTimeFormatter;
global using global::Files.Core.Services.Settings;
global using global::Files.Core.ViewModels;
global using global::Files.Core.ViewModels.Dialogs;
global using global::Files.Core.ViewModels.Dialogs.AddItemDialog;
global using global::Files.Core.ViewModels.Dialogs.FileSystemDialog;
global using global::Files.Core.ViewModels.FileTags;
global using global::Files.Core.ViewModels.Widgets;
global using global::Files.Core.Utils;
global using global::Files.Core.Utils.Cloud;
global using global::Files.Core.Utils.CommandLine;

// Files.Shared
global using global::Files.Shared;
global using global::Files.Shared.Extensions;
