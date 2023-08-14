using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Data.Parameters
{
	public class TabItemArguments
	{
		private static readonly KnownTypesConverter TypesConverter = new KnownTypesConverter();

		public Type InitialPageType { get; set; }
		public object NavigationArg { get; set; }

		public string Serialize() => JsonSerializer.Serialize(this, TypesConverter.Options);

		public static TabItemArguments Deserialize(string obj)
		{
			var tabArgs = new TabItemArguments();

			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
			tabArgs.InitialPageType = Type.GetType(tempArgs["InitialPageType"].GetString());

			try
			{
				tabArgs.NavigationArg = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs["NavigationArg"].GetRawText());
			}
			catch (JsonException)
			{
				tabArgs.NavigationArg = tempArgs["NavigationArg"].GetString();
			}

			return tabArgs;
		}
	}
}
