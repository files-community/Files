// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	public interface IToggleAction : IAction
	{
		/// <summary>
		/// Returns whether the toggle is on or not.
		/// </summary>
		bool IsOn { get; }
	}
}
