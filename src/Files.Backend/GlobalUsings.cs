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
global using global::Files.Backend.CommandLine;
global using global::Files.Backend.Data.Enums;
global using global::Files.Backend.Data.Messages;
global using global::Files.Backend.Data.Models;
global using global::Files.Backend.Extensions;
global using global::Files.Backend.Helpers;
global using global::Files.Backend.Services;
global using global::Files.Backend.Services.Settings;
global using global::Files.Backend.Services.SizeProvider;
global using global::Files.Backend.ViewModels;
