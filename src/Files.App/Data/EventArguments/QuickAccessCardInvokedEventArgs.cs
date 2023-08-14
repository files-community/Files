using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Data.EventArguments
{
	public class QuickAccessCardInvokedEventArgs : EventArgs
	{
		public string Path { get; set; }
	}
}
