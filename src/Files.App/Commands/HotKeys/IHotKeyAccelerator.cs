using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;

namespace Files.App.Commands
{
	public interface IHotKeyAccelerator
	{
		void Initialize(IList<KeyboardAccelerator> accelerators);
	}
}
