// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents an interface for the toggle Actions.
	/// </summary>
	public interface IToggleAction : IAction
	{
		/// <summary>
		/// Gets a value whether the toggle is on or not.
		/// </summary>
		bool IsOn { get; }
	}
}
