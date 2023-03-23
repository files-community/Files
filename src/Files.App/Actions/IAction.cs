﻿using Files.App.Commands;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public interface IAction
	{
		/// <summary>
		/// A label for display in context menus and toolbars
		/// </summary>
		string Label { get; }

		/// <summary>
		/// A brief description of what the action does
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Glyph information to display icon
		/// </summary>
		RichGlyph Glyph => RichGlyph.None;

		/// <summary>
		/// A hotkey to execute actions
		/// </summary>
		HotKey HotKey => HotKey.None;
		/// <summary>
		/// Another hotkey to execute actions
		/// </summary>
		HotKey SecondHotKey => HotKey.None;
		/// <summary>
		/// Yet another hotkey to execute actions
		/// </summary>
		HotKey ThirdHotKey => HotKey.None;
		/// <summary>
		/// A hotkey with media keys
		/// </summary>
		HotKey MediaHotKey => HotKey.None;

		/// <summary>
		/// Returns whether the action is executable in the current context.
		/// </summary>
		bool IsExecutable => true;

		/// <summary>
		/// Executes the action asynchronously.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task ExecuteAsync();
	}
}
