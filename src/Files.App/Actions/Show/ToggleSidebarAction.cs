﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private readonly SidebarViewModel viewModel;

		public string Label
			=> "ToggleSidebar".GetLocalizedResource();

		public string Description
			=> "ToggleSidebarDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.B, KeyModifiers.Ctrl);

		public bool IsOn
			=> viewModel.IsSidebarOpen;

		public ToggleSidebarAction()
		{
			viewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();

			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsSidebarOpen = !IsOn;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(SidebarViewModel.IsSidebarOpen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
