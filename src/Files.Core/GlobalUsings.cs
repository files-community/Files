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

// Files Back-end
global using global::Files.Core.Data.Enums;
global using global::Files.Core.Data.EventArguments;
global using global::Files.Core.Data.Items;
global using global::Files.Core.Data.Messages;
global using global::Files.Core.Data.Models;
global using global::Files.Core.Extensions;
global using global::Files.Core.Helpers;
global using global::Files.Core.Services;
global using global::Files.Core.Services.Settings;
global using global::Files.Core.Services.SizeProvider;
global using global::Files.Core.ViewModels;
global using global::Files.Core.Utils;
global using global::Files.Core.Utils.CommandLine;
