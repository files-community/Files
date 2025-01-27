using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	/// <summary>
	/// The blade mode.
	/// </summary>
	public enum BladeMode
	{
		/// <summary>
		/// Default mode : each blade will take the specified Width and Height
		/// </summary>
		Normal,

		/// <summary>
		/// Fullscreen mode : each blade will take the entire Width and Height of the UI control container (cf <see cref="BladeView"/>)
		/// </summary>
		Fullscreen
	}
}
