// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json.Serialization;

namespace Files.App.ViewModels.Actions
{
	[Serializable]
	public sealed partial class ActionsViewModel : ObservableObject
	{
		[JsonPropertyName("Command")]
		public string Command { get; set; }

		[JsonPropertyName("KeyBinding")]
		public string KeyBinding { get; set; }

		[JsonPropertyName("Args")]
		public string Args { get; set; }

		[JsonConstructor]
		public ActionsViewModel(string command, string keyBinding, string args)
		{
			Command = command;
			KeyBinding = keyBinding;
			Args = args;
		}
	}
}
