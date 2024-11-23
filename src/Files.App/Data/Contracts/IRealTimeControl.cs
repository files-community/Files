// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	internal interface IRealTimeControl
	{
		void InitializeContentLayout();

		void UpdateContentLayout();
	}
}