// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides the layout templates and the rules that choose which template a folder uses.
	/// </summary>
	public interface ILayoutTemplatesService : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the templates in display order.
		/// </summary>
		IReadOnlyList<LayoutTemplate> Templates { get; }

		/// <summary>
		/// Gets the conditions that make folders use a template other than <see cref="DefaultTemplate"/>.
		/// </summary>
		IReadOnlyList<LayoutRule> Rules { get; }

		/// <summary>
		/// Gets the template of folders that no rule matches, which can't be deleted.
		/// </summary>
		LayoutTemplate DefaultTemplate { get; }

		/// <summary>
		/// Gets whether any rule depends on the items of a folder.
		/// </summary>
		bool HasContentRules { get; }

		/// <summary>
		/// Occurs when a template is changed, with the id of the template.
		/// </summary>
		event EventHandler<string>? TemplateChanged;

		/// <summary>
		/// Occurs when the rules change.
		/// </summary>
		event EventHandler? RulesChanged;

		LayoutTemplate? GetTemplate(string? id);

		/// <summary>
		/// Saves a copy of the specified template, replacing the template with the same id.
		/// </summary>
		void SaveTemplate(LayoutTemplate template);

		/// <summary>
		/// Adds a copy of the specified template with a new id and the specified name.
		/// </summary>
		LayoutTemplate AddTemplate(LayoutTemplate source, string name);

		/// <summary>
		/// Deletes a template and the rules that use it.
		/// </summary>
		void DeleteTemplate(string id);

		/// <summary>
		/// Restores the built-in settings of a built-in template.
		/// </summary>
		void ResetTemplate(string id);

		/// <summary>
		/// Replaces all templates and their conditions with the built-in ones.
		/// </summary>
		void ResetTemplates();

		void SaveRules(IEnumerable<LayoutRule> rules);

		/// <summary>
		/// Gets the template of the specified folder.
		/// </summary>
		/// <param name="path">The path of the folder.</param>
		/// <param name="getContent">Gets the items of the folder, or null when they aren't known yet. Content rules are skipped when it returns null.</param>
		LayoutTemplate GetTemplateForPath(string? path, Func<LayoutFolderContent?>? getContent = null);
	}
}
