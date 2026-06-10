// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ServiceProcess;

namespace Files.SearchService;

internal sealed class SearchWindowsService : ServiceBase
{
	private CancellationTokenSource? _cts;
	private Task? _run;

	public SearchWindowsService()
	{
		ServiceName = "FilesSearchService";
		CanStop = true;
		CanPauseAndContinue = false;
		AutoLog = false;
	}

	protected override void OnStart(string[] args)
	{
		_cts = new CancellationTokenSource();
		_run = Task.Run(() => Program.RunAsync(_cts.Token));
	}

	protected override void OnStop()
	{
		_cts?.Cancel();
		try { _run?.Wait(TimeSpan.FromSeconds(10)); } catch { }
	}
}
