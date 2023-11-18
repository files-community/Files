// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents an interface for the <see cref="IAction"/> that have parameter.
	/// </summary>
	public interface IExtendedAction : IAction
	{
		/// <summary>
		/// Gets the parameter that can be used when execution is about to run.
		/// </summary>
		object? Parameter { get; set; }
	}
}
