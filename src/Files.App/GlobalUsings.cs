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
global using global::Files.App.Commands;
global using global::Files.App.Contexts;
global using global::Files.App.Helpers;
global using global::Files.App.Extensions;
global using global::Files.App.Utils;
global using global::Files.App.Data.EventArguments;
global using global::Files.App.Data.Factories;
global using global::Files.App.Data.Items;
global using global::Files.App.Data.Models;
global using global::Files.App.Data.Parameters;
global using global::Files.App.Interacts;
global using global::Files.App.UserControls;
global using global::Files.App.ViewModels;
global using global::Files.App.ViewModels.UserControls;
global using global::Files.App.Views;
global using global::Files.App.Views.LayoutModes;
global using global::Files.App.Views.Shells;

// Files.Core
global using global::Files.Core.CommandLine;
global using global::Files.Core.Data.Enums;
global using global::Files.Core.Data.Messages;
global using global::Files.Core.Data.Models;
global using global::Files.Core.Extensions;
global using global::Files.Core.Helpers;
global using global::Files.Core.Services;
global using global::Files.Core.Services.Settings;
//global using global::Files.Core.Services.SizeProvider;
global using global::Files.Core.ViewModels;
global using global::Files.Core.ViewModels.Dialogs;
global using global::Files.Core.ViewModels.Dialogs.AddItemDialog;
global using global::Files.Core.ViewModels.Dialogs.FileSystemDialog;
global using global::Files.Core.ViewModels.FileTags;
global using global::Files.Core.ViewModels.Widgets;

// Files.Shared
global using global::Files.Shared;
global using global::Files.Shared.Enums;
global using global::Files.Shared.Extensions;

// Files Back-end

// Vanara
//global using global::Vanara.PInvoke;
