using CommunityToolkit.WinUI;
using Files.App.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls
{
	/// <summary>
	/// Disclaimer: code from https://github.com/felixse/FluentTerminal
	/// </summary>
	public sealed partial class TerminalView : UserControl, IxtermEventListener, IDisposable
	{
		// Members related to initialization
		private readonly TaskCompletionSource<object> _tcsConnected = new TaskCompletionSource<object>();
		private readonly TaskCompletionSource<object> _tcsNavigationCompleted = new TaskCompletionSource<object>();

		IContentPageContext _context = Ioc.Default.GetRequiredService<IContentPageContext>();
		MainPageViewModel _mainPageModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		#region Resize handling

		// Members related to resize handling
		private static readonly TimeSpan ResizeDelay = TimeSpan.FromMilliseconds(60);
		private readonly object _resizeLock = new object();
		private TerminalSize _requestedSize;
		private TerminalSize _setSize;
		private DateTime _resizeScheduleTime;
		private Task _resizeTask;
		private MemoryStream _outputBlockedBuffer;

		// Must be called from a code locked with _resizeLock
		private void ScheduleResize(TerminalSize size, bool scheduleIfEqual)
		{
			if (!scheduleIfEqual && size.EquivalentTo(_requestedSize))
			{
				return;
			}

			_requestedSize = size;
			_resizeScheduleTime = DateTime.UtcNow.Add(ResizeDelay);

			if (_resizeTask == null)
			{
				_resizeTask = ResizeTask();
			}
		}

		private async Task ResizeTask()
		{
			while (true)
			{
				TimeSpan delay;
				TerminalSize size = null;

				lock (_resizeLock)
				{
					if (_requestedSize?.EquivalentTo(_setSize) ?? true)
					{
						// Resize finished. Unblock output and exit.

						if (_outputBlockedBuffer != null)
						{
							OnOutput?.Invoke(this, _outputBlockedBuffer.ToArray());

							_outputBlockedBuffer.Dispose();
							_outputBlockedBuffer = null;
						}

						_resizeTask = null;

						break;
					}

					delay = _resizeScheduleTime.Subtract(DateTime.UtcNow);

					// To avoid sleeping for only few milliseconds we're introducing a threshold of 10 milliseconds
					if (delay.TotalMilliseconds < 10)
					{
						_setSize = size = _requestedSize;

						if (_outputBlockedBuffer == null)
						{
							_outputBlockedBuffer = new MemoryStream();
						}
					}
				}

				if (size == null)
				{
					await Task.Delay(delay).ConfigureAwait(false);
				}
				else
				{
					_terminal.Resize(_requestedSize.Columns, _requestedSize.Rows);
				}
			}
		}

		#endregion Resize handling

		public event EventHandler<object> OnOutput;
		public event EventHandler<string> OnPaste;
		public event EventHandler<string> OnSessionRestart;

		public WebView2 WebView => WebViewControl;

		private Terminal.Terminal _terminal;
		private BufferedReader _reader;

		public TerminalView()
		{
			InitializeComponent();
		}

		private async void WebViewControl_LoadedAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await WebViewControl.EnsureCoreWebView2Async();
			//WebViewControl.CoreWebView2.OpenDevToolsWindow();
			WebViewControl.NavigationCompleted += WebViewControl_NavigationCompleted;
			WebViewControl.NavigationStarting += WebViewControl_NavigationStarting;
			WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping(
				"terminal.files",
				Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Terminal", "UI"),
				CoreWebView2HostResourceAccessKind.DenyCors);
			WebViewControl.Source = new Uri("http://terminal.files/index.html");

			// Waiting for WebView.NavigationCompleted event to happen
			await _tcsNavigationCompleted.Task;

			var provider = new DefaultValueProvider();
			var options = provider.GetDefaultTerminalOptions();
			var keyBindings = provider.GetCommandKeyBindings();
			var profile = _mainPageModel.TerminalSelectedProfile;
			var theme = provider.GetPreInstalledThemes().First(x => x.Id == profile.TerminalThemeId);

			WebViewControl.DefaultBackgroundColor = ColorHelpers.FromHex(theme.Colors.Background);

			var size = await CreateXtermViewAsync(options, theme.Colors,
				keyBindings.Values.SelectMany(k => k)).ConfigureAwait(false);

			// Waiting for IxtermEventListener.OnInitialized() call to happen
			await _tcsConnected.Task;

			lock (_resizeLock)
			{
				// Check to see if some resizing has happened meanwhile
				if (_requestedSize != null)
				{
					size = _requestedSize;
				}
				else
				{
					_requestedSize = size;
				}
			}

			StartShellProcess(size, profile);

			lock (_resizeLock)
			{
				// Check to see if some resizing has happened meanwhile
				if (!size.EquivalentTo(_requestedSize))
				{
					ScheduleResize(_requestedSize, true);
				}
				else
				{
					_setSize = size;
				}
			}
		}

		private Task<TerminalSize> CreateXtermViewAsync(TerminalOptions options, TerminalColors theme, IEnumerable<KeyBinding> keyBindings)
		{
			var serializerSettings = new JsonSerializerSettings();
			serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			var serializedOptions = JsonConvert.SerializeObject(options, serializerSettings);
			var serializedTheme = JsonConvert.SerializeObject(theme, serializerSettings);
			var serializedKeyBindings = JsonConvert.SerializeObject(keyBindings, serializerSettings);
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
			if (Enum.TryParse(command, true, out Command commandValue))
			{
				ProcessKeyboardCommand(commandValue.ToString());
			}
			else if (Guid.TryParse(command, out Guid shellProfileId))
			{
				ProcessKeyboardCommand(shellProfileId.ToString());
			}
		}

		void IxtermEventListener.OnMouseClick(MouseButton mouseButton, int x, int y, bool hasSelection, string hoveredUri)
		{
			var action = MouseAction.None;

			switch (mouseButton)
			{
				case MouseButton.Middle:
					action = MouseAction.None;
					break;
				case MouseButton.Right:
					action = MouseAction.CopySelectionOrPaste;
					break;
			}

			if (action == MouseAction.ContextMenu)
			{
			}
			else if (action == MouseAction.Paste)
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
			}
		}

		async void IxtermEventListener.OnSelectionChanged(string selection)
		{
			if (!string.IsNullOrEmpty(selection) && false)
			{
				CopyTextToClipbpard(selection);
				await ExecuteScriptAsync("term.clearSelection()").ConfigureAwait(false);
			}
		}

		void IxtermEventListener.OnTerminalResized(int columns, int rows)
		{
			var size = new TerminalSize { Columns = columns, Rows = rows };

			lock (_resizeLock)
			{
				if (_setSize == null)
				{
					// Initialization not finished yet
					_requestedSize = size;
				}
				else
				{
					ScheduleResize(size, false);
				}
			}
		}

		void IxtermEventListener.OnInitialized()
		{
			_tcsConnected.TrySetResult(null);
		}

		void IxtermEventListener.OnTitleChanged(string title)
		{
		}

		void IxtermEventListener.OnError(string error)
		{
			App.Logger.LogWarning(error);
		}

		private void OutputReceivedCallback(byte[] e)
		{
			OnOutput?.Invoke(this, e);
		}

		public void OnInput(byte[] data)
		{
			_terminal.WriteToPseudoConsole(data);
		}

		private void CopyTextToClipbpard(string content)
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage data = new();
				data.SetText(content);

				Clipboard.SetContent(data);
				Clipboard.Flush();
			});
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

		public void Dispose()
		{
			_mainPageModel.GetTerminalFolder = null;
			_mainPageModel.SetTerminalFolder = null;
			WebViewControl.Close();
			_outputBlockedBuffer?.Dispose();
			_reader?.Dispose();
			_terminal?.Dispose();
		}

		public void Paste(string text) => OnPaste?.Invoke(this, text);

		private void StartShellProcess(TerminalSize size, ShellProfile profile)
		{
			var ShellExecutableName = Path.GetFileNameWithoutExtension(profile.Location);
			var cwd = _context.Folder?.ItemPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			var args = !string.IsNullOrWhiteSpace(profile.Location)
				? $"\"{profile.Location}\" {profile.Arguments}"
				: profile.Arguments;

			_terminal = new Terminal.Terminal();
			_terminal.OutputReady += (s, e) =>
			{
				_reader = new BufferedReader(_terminal.ConsoleOutStream, OutputReceivedCallback, true);
				_mainPageModel.GetTerminalFolder = async () =>
				{
					var tcs = new TaskCompletionSource<string>();
					EventHandler<object> getResponse = (s, e) =>
					{
						var pwd = Encoding.UTF8.GetString((byte[])e);
						var match = Regex.Match(pwd, @"[a-zA-Z]:\\(((?![<>:""/\\|?*]).)+((?<![ .])\\)?)*");
						if (match.Success)
							tcs.TrySetResult(match.Value);
					};
					OnOutput += getResponse;
					_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"cd .\r"));
					var pwd = await tcs.Task.WithTimeoutAsync(TimeSpan.FromSeconds(1));
					OnOutput -= getResponse;
					return pwd;
				};
				_mainPageModel.SetTerminalFolder = (folder) =>
				{
					_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"cd \"{folder}\"\r"));
				};
			};
			_terminal.Exited += (s, e) =>
			{
				DispatcherQueue.EnqueueAsync(() =>
				{
					_mainPageModel.IsTerminalViewOpen = false;
				});
			};

			Task.Factory.StartNew(() => _terminal.Start(args, cwd, size.Columns, size.Rows));
		}

		private async void ProcessKeyboardCommand(string e)
		{
			switch (e)
			{
				case nameof(Command.Copy):
					{
						var selection = await ExecuteScriptAsync("term.getSelection()").ConfigureAwait(false);
						CopyTextToClipbpard(selection);
						return;
					}
				case nameof(Command.Paste):
					{
						Task<string> GetTextAsync()
						{
							var content = Clipboard.GetContent();
							if (content.Contains(StandardDataFormats.Text))
								return content.GetTextAsync().AsTask();
							// Otherwise return a new task that just sends an empty string.
							return Task.FromResult(string.Empty);
						}
						var content = await GetTextAsync().ConfigureAwait(false);
						if (content != null)
							Paste(content);
						return;
					}
				case nameof(Command.PasteWithoutNewlines):
					{
						return;
					}
				case nameof(Command.Search):
					{
						return;
					}
				case nameof(Command.CloseSearch):
					{
						return;
					}
				case nameof(Command.IncreaseFontSize):
					{
						FontSize++;
						await ExecuteScriptAsync($"setFontSize({FontSize})");
						return;
					}
				case nameof(Command.DecreaseFontSize):
					{
						if (FontSize > 2)
						{
							FontSize--;
							await ExecuteScriptAsync($"setFontSize({FontSize})");
						}

						return;
					}
				case nameof(Command.ResetFontSize):
					{
						FontSize = new DefaultValueProvider().GetDefaultTerminalOptions().FontSize;
						await ExecuteScriptAsync($"setFontSize({FontSize})");
						return;
					}
				default:
					{
						return;
					}
			}
		}

		private void TerminalView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			Dispose();
		}
	}
}