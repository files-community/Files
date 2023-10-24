using CommunityToolkit.WinUI;
using Files.App.Data.Items;
using Files.App.Terminal;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System.IO;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class TerminalView : UserControl, IxtermEventListener
	{
		// Members related to initialization
		private readonly TaskCompletionSource<object> _tcsConnected = new TaskCompletionSource<object>();
		private readonly TaskCompletionSource<object> _tcsNavigationCompleted = new TaskCompletionSource<object>();

		public event EventHandler<object> OnOutput;
		public event EventHandler<string> OnPaste;
		public event EventHandler<string> OnSessionRestart;

		private Terminal.Terminal _terminal { get; set; }

		public TerminalView()
		{
			InitializeComponent();
		}

		private async void WebViewControl_LoadedAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await WebViewControl.EnsureCoreWebView2Async();
			WebViewControl.NavigationCompleted += WebViewControl_NavigationCompleted;
			WebViewControl.NavigationStarting += WebViewControl_NavigationStarting;
			WebViewControl.Source = new Uri("ms-appx-web:///Terminal/UI/index.html");

			//ViewModel.Terminal.OutputReceived += Terminal_OutputReceived;
			//ViewModel.Terminal.RegisterSelectedTextCallback(() => ExecuteScriptAsync("term.getSelection()"));

			// Waiting for WebView.NavigationCompleted event to happen
			await _tcsNavigationCompleted.Task.ConfigureAwait(false);

			var provider = new DefaultValueProvider();
			var options = provider.GetDefaultTerminalOptions();
			var keyBindings = provider.GetCommandKeyBindings();
			var theme = provider.GetPreInstalledThemes().First();
			var profile = provider.GetPreinstalledShellProfiles().First();

			var size = await CreateXtermViewAsync(options, theme.Colors,
				keyBindings.Values.SelectMany(k => k)).ConfigureAwait(false);

			// Waiting for IxtermEventListener.OnInitialized() call to happen
			await _tcsConnected.Task;

			StartShellProcess(size, profile);
		}

		private Task<TerminalSize> CreateXtermViewAsync(TerminalOptions options, TerminalColors theme, IEnumerable<KeyBinding> keyBindings)
		{
			var serializedOptions = JsonConvert.SerializeObject(options);
			var serializedTheme = JsonConvert.SerializeObject(theme);
			var serializedKeyBindings = JsonConvert.SerializeObject(keyBindings);
			return ExecuteScriptAsync(
					$"createTerminal('{serializedOptions}', '{serializedTheme}', '{serializedKeyBindings}')")
				.ContinueWith(t => JsonConvert.DeserializeObject<TerminalSize>(t.Result));
		}

		private void WebViewControl_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
		{
			var _terminalBridge = new TerminalBridge(this);
			_ = WebViewControl.AddWebAllowedObject("terminalBridge", _terminalBridge);
		}

		private void WebViewControl_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			_tcsNavigationCompleted.TrySetResult(null);
		}

		void IxtermEventListener.OnKeyboardCommand(string command)
		{
			/*if (Enum.TryParse(command, true, out Command commandValue))
			{
				ViewModel.Terminal.ProcessKeyboardCommand(commandValue.ToString());
			}
			else if (Guid.TryParse(command, out Guid shellProfileId))
			{
				ViewModel.Terminal.ProcessKeyboardCommand(shellProfileId.ToString());
			}*/
		}

		void IxtermEventListener.OnMouseClick(MouseButton mouseButton, int x, int y, bool hasSelection, string hoveredUri)
		{
			/*var action = MouseAction.None;

			switch (mouseButton)
			{
				case MouseButton.Middle:
					action = ViewModel.ApplicationSettings.MouseMiddleClickAction;
					break;
				case MouseButton.Right:
					action = ViewModel.ApplicationSettings.MouseRightClickAction;
					break;
			}

			if (action == MouseAction.Paste)
			{
				((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Paste));
			}
			else if (action == MouseAction.CopySelectionOrPaste)
			{
				if (hasSelection)
				{
					((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Copy));
				}
				else
				{
					((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Paste));
				}
			}*/
		}

		async void IxtermEventListener.OnSelectionChanged(string selection)
		{
			if (!string.IsNullOrEmpty(selection))
			{
				await ExecuteScriptAsync("term.clearSelection()").ConfigureAwait(false);
			}
		}

		void IxtermEventListener.OnTerminalResized(int columns, int rows)
		{
			//TODO
		}

		void IxtermEventListener.OnInitialized()
		{
			_tcsConnected.TrySetResult(null);
		}

		void IxtermEventListener.OnTitleChanged(string title)
		{
			//ViewModel.Terminal.SetTitle(title);
		}

		private void Terminal_OutputReceived(object sender, byte[] e)
		{
			OnOutput?.Invoke(this, e);
		}

		void IxtermEventListener.OnError(string error)
		{
		}

		public void OnInput(byte[] data)
		{
			_terminal.WriteToPseudoConsole(data);
		}

		private async Task<string> ExecuteScriptAsync(string script)
		{
			try
			{
				var scriptTask = DispatcherQueue.EnqueueAsync(() => WebViewControl.InvokeScriptAsync("eval", new[] { script }))
					.ConfigureAwait(false);

				return await scriptTask;
			}
			catch (Exception e)
			{
			}

			return string.Empty;
		}

		private void StartShellProcess(TerminalSize size, ShellProfile profile)
		{
			var sessionType = SessionType.ConPty;

			var ShellExecutableName = Path.GetFileNameWithoutExtension(profile.Location);
			var cwd = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			var args = !string.IsNullOrWhiteSpace(profile.Location)
				? $"\"{profile.Location}\" {profile.Arguments}"
				: profile.Arguments;

			BufferedReader _reader;

			_terminal = new Terminal.Terminal();
			_terminal.OutputReady += (s, e) =>
			{
				_reader = new BufferedReader(_terminal.ConsoleOutStream, b => Terminal_OutputReceived(this, b), true);
			};

			Task.Factory.StartNew(() => _terminal.Start(args, cwd,
				80, 30));
		}
	}
}