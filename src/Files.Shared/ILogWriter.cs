﻿using System.Threading.Tasks;

namespace Files.Shared
{
	public interface ILogWriter
	{
		Task InitializeAsync(string name);
		Task WriteLineToLogAsync(string text);
		void WriteLineToLog(string text);
	}
}
