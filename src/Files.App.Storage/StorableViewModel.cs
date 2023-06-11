// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.Sdk.Storage;

namespace Files.App.Storage
{
	public abstract class StorableViewModel : ObservableObject
	{
		public IStorable Storable { get; }

		public StorableViewModel(IStorable storable)
		{
			this.Storable = storable;
		}
	}
}
