using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Data.EventArguments
{
	public class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabItemContent CurrentInstance { get; set; }

		public List<ITabItemContent> PageInstances { get; set; }
	}
}
