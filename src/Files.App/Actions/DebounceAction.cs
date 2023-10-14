using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	public abstract class DebouncedAction : IAction
	{
		private DateTime lastExecuted = DateTime.MinValue;

		private readonly TimeSpan debounceTime;
		public abstract string Label { get; }
		public abstract string Description { get; }
		public abstract RichGlyph Glyph { get; }
		public abstract HotKey HotKey { get; }
		public abstract HotKey SecondHotKey { get; }
		public abstract HotKey ThirdHotKey { get; }
		public abstract HotKey MediaHotKey { get; }
		public abstract bool IsExecutable { get; }

		protected DebouncedAction(TimeSpan debounceDuration)
		{
			debounceTime = debounceDuration;
		}

		public virtual bool CanExecuteNow()
		{
			return DateTime.Now - lastExecuted > debounceTime;
		}

		public virtual void MarkLastExecutionTime()
		{
			lastExecuted = DateTime.Now;
		}

		public abstract Task ExecuteAsync();
	}
}