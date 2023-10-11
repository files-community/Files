using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public class DebouncedActionDecorator : DebouncedAction
	{
		private readonly IAction _innerAction;

		public DebouncedActionDecorator(IAction innerAction, TimeSpan debounceDuration)
			: base(debounceDuration)
		{
			_innerAction = innerAction;
		}

		public override string Label => _innerAction.Label;
		public override string Description => _innerAction.Description;
		public override RichGlyph Glyph => _innerAction.Glyph;
		public override HotKey HotKey => _innerAction.HotKey;
		public override HotKey SecondHotKey => _innerAction.SecondHotKey;
		public override HotKey ThirdHotKey => _innerAction.ThirdHotKey;
		public override HotKey MediaHotKey => _innerAction.MediaHotKey;
		public override bool IsExecutable => _innerAction.IsExecutable;

		public override async Task ExecuteAsync()
		{
			if (!CanExecuteNow())
			{
				return;
			}

			MarkAsExecuted();

			await _innerAction.ExecuteAsync();
		}
	}
}
