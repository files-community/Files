using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public class DebouncedActionDecorator : DebouncedAction
	{
		public override string Label => _innerAction.Label;
		public override string Description => _innerAction.Description;
		public override RichGlyph Glyph => _innerAction.Glyph;
		public override HotKey HotKey => _innerAction.HotKey;
		public override HotKey SecondHotKey => _innerAction.SecondHotKey;
		public override HotKey ThirdHotKey => _innerAction.ThirdHotKey;
		public override HotKey MediaHotKey => _innerAction.MediaHotKey;
		public override bool IsExecutable => _innerAction.IsExecutable;

		private readonly IAction _innerAction;

		/// <summary>
		/// Initializes a new instance of the DebouncedActionDecorator class.
		/// </summary>
		/// <param name="innerAction">The IAction instance to be wrapped.</param>
		/// <param name="debounceDuration">The debounce duration. Default is 800 milliseconds.</param>
		public DebouncedActionDecorator(IAction innerAction, TimeSpan? debounceDuration = null)
			: base(debounceDuration ?? TimeSpan.FromMilliseconds(800))
		{
			_innerAction = innerAction;
		}

		public override async Task ExecuteAsync()
		{
			if (!CanExecuteNow())
				return;
			
			MarkLastExecutionTime();

			await _innerAction.ExecuteAsync();
		}
	}
}
