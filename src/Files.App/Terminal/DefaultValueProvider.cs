using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.Foundation.Metadata;

namespace Files.App.UserControls
{
	/// <summary>
	/// Disclaimer: code from https://github.com/felixse/FluentTerminal
	/// </summary>
	public class DefaultValueProvider
	{
		public IDictionary<string, ICollection<KeyBinding>> GetCommandKeyBindings()
		{
			var keyBindings = new Dictionary<string, ICollection<KeyBinding>>();

			foreach (Command command in Enum.GetValues(typeof(Command)))
			{
				keyBindings.Add(command.ToString(), GetDefaultKeyBindings(command));
			}
			return keyBindings;
		}

		public ICollection<KeyBinding> GetDefaultKeyBindings(Command command)
		{
			switch (command)
			{
				case Command.ToggleWindow:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.ToggleWindow),
						Key = (int)ExtendedVirtualKey.Scroll
					}
				};

				case Command.NextTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.NextTab),
						Ctrl = true,
						Key = (int)ExtendedVirtualKey.Tab
					}
				};

				case Command.PreviousTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.PreviousTab),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.Tab
					}
				};

				case Command.SwitchToTerm1:
				case Command.SwitchToTerm2:
				case Command.SwitchToTerm3:
				case Command.SwitchToTerm4:
				case Command.SwitchToTerm5:
				case Command.SwitchToTerm6:
				case Command.SwitchToTerm7:
				case Command.SwitchToTerm8:
				case Command.SwitchToTerm9:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = command.ToString(),
						Alt = true,
						Key = (int)ExtendedVirtualKey.Number1 + (command - Command.SwitchToTerm1)
					}
				};

				case Command.DuplicateTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.DuplicateTab),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.D
					}
				};

				case Command.ReconnectTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.ReconnectTab),
						Ctrl = true,
						Alt = true,
						Key = (int)ExtendedVirtualKey.R
					}
				};

				case Command.NewTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.NewTab),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.T
					}
				};

				case Command.NewSshTab:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.NewSshTab),
							Ctrl = true,
							Shift = true,
							Key = (int)ExtendedVirtualKey.Y
						}
					};

				case Command.NewSshWindow:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.NewSshWindow),
							Ctrl = true,
							Alt = true,
							Key = (int)ExtendedVirtualKey.Y
						}
					};

				case Command.NewCustomCommandTab:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.NewCustomCommandTab),
							Ctrl = true,
							Alt = true,
							Key = (int)ExtendedVirtualKey.T
						}
					};

				case Command.NewCustomCommandWindow:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.NewCustomCommandWindow),
							Ctrl = true,
							Alt = true,
							Key = (int)ExtendedVirtualKey.N
						}
					};

				case Command.ChangeTabTitle:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.ChangeTabTitle),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.R
					}
				};

				case Command.CloseTab:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.CloseTab),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.W
					}
				};

				case Command.NewWindow:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.NewWindow),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.N
					}
				};

				case Command.ShowSettings:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.ShowSettings),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.Comma
					}
				};

				case Command.Copy:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Copy),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.C
					}
				};

				case Command.Paste:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Paste),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.V
					}
				};


				case Command.PasteWithoutNewlines:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.PasteWithoutNewlines),
						Ctrl = true,
						Alt = true,
						Key = (int)ExtendedVirtualKey.V
					}
				};

				case Command.Search:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Search),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.F
					}
				};

				case Command.CloseSearch:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.CloseSearch),
						Key = (int)ExtendedVirtualKey.Escape
					}
				};

				case Command.ToggleFullScreen:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.ToggleFullScreen),
						Alt = true,
						Key = (int)ExtendedVirtualKey.Enter
					},
					new KeyBinding
					{
						Command = nameof(Command.ToggleFullScreen),
						Key = (int)ExtendedVirtualKey.F11
					}
				};

				case Command.SelectAll:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.SelectAll),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.A
					}
				};

				case Command.Clear:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Clear),
						Ctrl = true,
						Alt = true,
						Key = (int)ExtendedVirtualKey.L
					}
				};
				case Command.IncreaseFontSize:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.IncreaseFontSize),
							Ctrl = true,
							Key = (int)ExtendedVirtualKey.Plus
						}
					};
				case Command.DecreaseFontSize:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.DecreaseFontSize),
							Ctrl = true,
							Key = (int)ExtendedVirtualKey.Minus
						}
					};
				case Command.ResetFontSize:
					return new List<KeyBinding>
					{
						new KeyBinding
						{
							Command = nameof(Command.ResetFontSize),
							Ctrl = true,
							Key = (int)ExtendedVirtualKey.Number0
						}
					};
				default:
					throw new InvalidOperationException($"Default keybindings for Command '{command}' are missing");
			}
		}

		public Guid GetDefaultShellProfileId()
		{
			return Guid.Parse("813f2298-210a-481a-bdbf-c17bc637a3e2");
		}

		public TerminalOptions GetDefaultTerminalOptions()
		{
			return new TerminalOptions
			{
				BellStyle = BellStyle.None,
				CursorBlink = true,
				CursorStyle = CursorStyle.Block,
				ScrollBarStyle = ScrollBarStyle.Hidden,
				FontFamily = "Consolas",
				FontSize = 13,
				FontWeight = 400,
				BackgroundOpacity = 0.8,
				Padding = 12,
				ScrollBackLimit = 1000,
				WordSeparator = " ()[]{}:;|│!&*<>@&quot;&squo;",
				UseAcrylicBackground = true
			};
		}

		public Guid GetDefaultThemeId()
		{
			return Guid.Parse("281e4352-bb50-47b7-a691-2b13830df95e");
		}

		public IEnumerable<ShellProfile> GetPreinstalledShellProfiles()
		{
			return new[]
			{
				new ShellProfile
				{
					Id = GetDefaultShellProfileId(),
					Name = "Powershell",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\windows\system32\WindowsPowerShell\v1.0\powershell.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8), // Windows 10 1903+
                    UseBuffer = false,
					EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = GetDefaultShellProfileId().ToString(),
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number1
						}
					}
				},
				new ShellProfile
				{
					Id = Guid.Parse("ab942a61-7673-4755-9bd8-765aff91d9a3"),
					Name = "CMD",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\Windows\System32\cmd.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7), // Windows 10 1809+
                    UseBuffer = true,
					EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = "ab942a61-7673-4755-9bd8-765aff91d9a3",
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number2
						}
					}
				},
				new ShellProfile
				{
					Id= Guid.Parse("e5785ad6-584f-40cb-bdcd-d5b3b3953e7f"),
					Name = "WSL",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\windows\system32\wsl.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7), // Windows 10 1809+
                    UseBuffer = true, //TODO: Set to false if the buffer causes issues with WSL.
                    EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = "e5785ad6-584f-40cb-bdcd-d5b3b3953e7f",
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number3
						}
					}
				}
			};
		}

		public IEnumerable<TerminalTheme> GetPreInstalledThemes()
		{
			var defaultXterm = new TerminalTheme
			{
				Id = GetDefaultThemeId(),
				Author = "xterm.js",
				Name = "Xterm.js Default",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#2e3436",
					Red = "#cc0000",
					Green = "#4e9a06",
					Yellow = "#c4a000",
					Blue = "#3465a4",
					Magenta = "#75507b",
					Cyan = "#06989a",
					White = "#d3d7cf",
					BrightBlack = "#555753",
					BrightRed = "#ef2929",
					BrightGreen = "#8ae234",
					BrightYellow = "#fce94f",
					BrightBlue = "#729fcf",
					BrightMagenta = "#ad7fa8",
					BrightCyan = "#34e2e2",
					BrightWhite = "#eeeeec",
					Foreground = "#ffffff",
					Background = "#000000",
					Cursor = "#ffffff",
					CursorAccent = "#000000",
					Selection = "rgba(255, 255, 255, 0.3)"
				}
			};

			var powerShell = new TerminalTheme
			{
				Id = Guid.Parse("3571ce1b-31ce-4cf7-ae15-e0bff70c3eea"),
				Author = "Microsoft",
				Name = "PowerShell",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#000000",
					Red = "#800000",
					Green = "#008000",
					Yellow = "#EEEDF0",
					Blue = "#000080",
					Magenta = "#012456",
					Cyan = "#008080",
					White = "#c0c0c0",
					BrightBlack = "#808080",
					BrightRed = "#ff0000",
					BrightGreen = "#00ff00",
					BrightYellow = "#ffff00",
					BrightBlue = "#0000ff",
					BrightMagenta = "#ff00ff",
					BrightCyan = "#00ffff",
					BrightWhite = "#ffffff",
					Foreground = "#ffffff",
					Background = "#012456",
					Cursor = "#fedba9",
					CursorAccent = "#000000",
					Selection = "rgba(254, 219, 169, 0.3)"
				}
			};

			var homebrew = new TerminalTheme
			{
				Id = Guid.Parse("5f034210-067b-40e2-b9c9-6a25eb6fb8e6"),
				Author = "Hans Kokx",
				Name = "Homebrew",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#000000",
					Red = "#A0160B",
					Green = "#00AF21",
					Yellow = "#A1A222",
					Blue = "#192AB7",
					Magenta = "#AA2FAE",
					Cyan = "#12B1BC",
					White = "#BBB5AF",
					BrightBlack = "#747876",
					BrightRed = "#E52213",
					BrightGreen = "#00D92B",
					BrightYellow = "#E6E435",
					BrightBlue = "#283EF9",
					BrightMagenta = "#EB43E6",
					BrightCyan = "#15E7E8",
					BrightWhite = "#E9E9E9",
					Foreground = "#00D92B",
					Background = "#000000",
					Cursor = "#00D92B",
					CursorAccent = "#000000",
					Selection = "#4CFFFFFF"
				}
			};

			var tomorrow = new TerminalTheme
			{
				Id = Guid.Parse("0cbbf805-fa86-4be5-ac51-75f3054dcc3a"),
				Author = "Chris Kempson",
				Name = "Tomorrow",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Foreground = "#4D4D4C",
					Background = "#FFFFFF",
					Cursor = "#4D4D4C",
					CursorAccent = "#FFFFFF",
					Selection = "rgba(214, 214, 214, 0.5)",
					Black = "#000000",
					Red = "#C82829",
					Green = "#718C00",
					Yellow = "#EAB700",
					Blue = "#4271AE",
					Magenta = "#8959A8",
					Cyan = "#3E999F",
					White = "#FFFFFF",
					BrightBlack = "#000000",
					BrightRed = "#C82829",
					BrightGreen = "#718C00",
					BrightYellow = "#EAB700",
					BrightBlue = "#4271AE",
					BrightMagenta = "#8959A8",
					BrightCyan = "#3E999F",
					BrightWhite = "#FFFFFF"
				}
			};

			var ubuntu = new TerminalTheme
			{
				Id = Guid.Parse("32519543-bf94-4db6-92a1-7bcec3966c82"),
				Author = "Canonical",
				Name = "Ubuntu Bash",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Foreground = "#EEEEEE",
					Background = "#300A24",
					Cursor = "#CFF5DB",
					CursorAccent = "#000000",
					Selection = "#4DABEDC1",
					Black = "#300A24",
					Red = "#CC0000",
					Green = "#4E9A06",
					Yellow = "#C4A000",
					Blue = "#3465A4",
					Magenta = "#75507B",
					Cyan = "#06989A",
					White = "#D3D7CF",
					BrightBlack = "#554E53",
					BrightRed = "#EF2929",
					BrightGreen = "#8AE234",
					BrightYellow = "#FCE94F",
					BrightBlue = "#729FCF",
					BrightMagenta = "#AD7FA8",
					BrightCyan = "#34E2E2",
					BrightWhite = "#EEEEEE"
				}
			};

			return new[] { defaultXterm, powerShell, homebrew, tomorrow, ubuntu };
		}
	}

	public class KeyBinding
	{
		public KeyBinding()
		{

		}

		public KeyBinding(KeyBinding other)
		{
			Command = other.Command;
			Key = other.Key;
			Ctrl = other.Ctrl;
			Alt = other.Alt;
			Shift = other.Shift;
			Meta = other.Meta;
		}

		public string Command { get; set; }
		public int Key { get; set; }
		public bool Ctrl { get; set; }
		public bool Alt { get; set; }
		public bool Shift { get; set; }
		public bool Meta { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is KeyBinding other)
			{
				return other.Command.Equals(Command)
					&& other.Key == Key
					&& other.Ctrl == Ctrl
					&& other.Alt == Alt
					&& other.Shift == Shift
					&& other.Meta == Meta;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Command, Key, Ctrl, Alt, Shift, Meta);
		}
	}

	public class TerminalTheme
	{
		public TerminalTheme()
		{
		}

		public TerminalTheme(TerminalTheme other)
		{
			Id = other.Id;
			Name = other.Name;
			Author = other.Author;
			PreInstalled = other.PreInstalled;
			Colors = new TerminalColors(other.Colors);;
		}

		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public bool PreInstalled { get; set; }
		public TerminalColors Colors { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is TerminalTheme other)
			{
				return Equals(other.Name, Name)
					&& Equals(other.Author, Author)
					&& Equals(other.Colors, Colors);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Author, Colors);
		}
	}

	public class TerminalColors
	{
		public string Foreground { get; set; }
		public string Background { get; set; }
		public string Cursor { get; set; }
		public string CursorAccent { get; set; }
		public string Selection { get; set; }
		public string SelectionForeground { get; set; }
		public string SelectionBackground { get; set; }

		public string Black { get; set; }
		public string Red { get; set; }
		public string Green { get; set; }
		public string Yellow { get; set; }
		public string Blue { get; set; }
		public string Magenta { get; set; }
		public string Cyan { get; set; }
		public string White { get; set; }

		public string BrightBlack { get; set; }
		public string BrightRed { get; set; }
		public string BrightGreen { get; set; }
		public string BrightYellow { get; set; }
		public string BrightBlue { get; set; }
		public string BrightMagenta { get; set; }
		public string BrightCyan { get; set; }
		public string BrightWhite { get; set; }

		public TerminalColors()
		{
		}

		public TerminalColors(TerminalColors other)
		{
			Foreground = other.Foreground;
			Background = other.Background;
			Cursor = other.Cursor;
			CursorAccent = other.CursorAccent;
			Selection = other.Selection;

			Black = other.Black;
			Red = other.Red;
			Green = other.Green;
			Yellow = other.Yellow;
			Blue = other.Blue;
			Magenta = other.Magenta;
			Cyan = other.Cyan;
			White = other.White;

			BrightBlack = other.BrightBlack;
			BrightRed = other.BrightRed;
			BrightGreen = other.BrightGreen;
			BrightYellow = other.BrightYellow;
			BrightBlue = other.BrightBlue;
			BrightMagenta = other.BrightMagenta;
			BrightCyan = other.BrightCyan;
			BrightWhite = other.BrightWhite;
		}

		public override bool Equals(object obj)
		{
			if (obj is TerminalColors other)
			{
				return other.Foreground.Equals(Foreground)
					&& other.Background.Equals(Background)
					&& other.Cursor.Equals(Cursor)
					&& other.CursorAccent.Equals(CursorAccent)
					&& other.Selection.Equals(Selection)
					&& other.Black.Equals(Black)
					&& other.Red.Equals(Red)
					&& other.Green.Equals(Green)
					&& other.Yellow.Equals(Yellow)
					&& other.Blue.Equals(Blue)
					&& other.Magenta.Equals(Magenta)
					&& other.Cyan.Equals(Cyan)
					&& other.White.Equals(White)
					&& other.BrightBlack.Equals(BrightBlack)
					&& other.BrightRed.Equals(BrightRed)
					&& other.BrightGreen.Equals(BrightGreen)
					&& other.BrightYellow.Equals(BrightYellow)
					&& other.BrightBlue.Equals(BrightBlue)
					&& other.BrightMagenta.Equals(BrightMagenta)
					&& other.BrightCyan.Equals(BrightCyan)
					&& other.BrightWhite.Equals(BrightWhite);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				HashCode.Combine(Foreground, Background, Cursor, CursorAccent, Selection),
				HashCode.Combine(Black, Red, Green, Yellow, Blue, Magenta, Cyan, White),
				HashCode.Combine(BrightBlack, BrightRed, BrightGreen, BrightYellow, BrightBlue, BrightMagenta, BrightCyan, BrightWhite)
			);
		}
	}

	public enum ExtendedVirtualKey
	{
		None = 0,
		LeftButton = 1,
		RightButton = 2,
		Cancel = 3,
		MiddleButton = 4,
		XButton1 = 5,
		XButton2 = 6,
		Back = 8,
		Tab = 9,
		Clear = 12,
		Enter = 13,
		Shift = 16,
		Control = 17,
		Menu = 18,
		Pause = 19,
		CapitalLock = 20,
		Kana = 21,
		Hangul = 21,
		Junja = 23,
		Final = 24,
		Hanja = 25,
		Kanji = 25,
		Escape = 27,
		Convert = 28,
		NonConvert = 29,
		Accept = 30,
		ModeChange = 31,
		Space = 32,
		PageUp = 33,
		PageDown = 34,
		End = 35,
		Home = 36,
		Left = 37,
		Up = 38,
		Right = 39,
		Down = 40,
		Select = 41,
		Print = 42,
		Execute = 43,
		Snapshot = 44,
		Insert = 45,
		Delete = 46,
		Help = 47,

		[Description("0")]
		Number0 = 48,

		[Description("1")]
		Number1 = 49,

		[Description("2")]
		Number2 = 50,

		[Description("3")]
		Number3 = 51,

		[Description("4")]
		Number4 = 52,

		[Description("5")]
		Number5 = 53,

		[Description("6")]
		Number6 = 54,

		[Description("7")]
		Number7 = 55,

		[Description("8")]
		Number8 = 56,

		[Description("9")]
		Number9 = 57,

		A = 65,
		B = 66,
		C = 67,
		D = 68,
		E = 69,
		F = 70,
		G = 71,
		H = 72,
		I = 73,
		J = 74,
		K = 75,
		L = 76,
		M = 77,
		N = 78,
		O = 79,
		P = 80,
		Q = 81,
		R = 82,
		S = 83,
		T = 84,
		U = 85,
		V = 86,
		W = 87,
		X = 88,
		Y = 89,
		Z = 90,
		LeftWindows = 91,
		RightWindows = 92,
		Application = 93,
		Sleep = 95,
		NumberPad0 = 96,
		NumberPad1 = 97,
		NumberPad2 = 98,
		NumberPad3 = 99,
		NumberPad4 = 100,
		NumberPad5 = 101,
		NumberPad6 = 102,
		NumberPad7 = 103,
		NumberPad8 = 104,
		NumberPad9 = 105,
		Multiply = 106,
		Add = 107,
		Separator = 108,
		Subtract = 109,
		Decimal = 110,
		Divide = 111,
		F1 = 112,
		F2 = 113,
		F3 = 114,
		F4 = 115,
		F5 = 116,
		F6 = 117,
		F7 = 118,
		F8 = 119,
		F9 = 120,
		F10 = 121,
		F11 = 122,
		F12 = 123,
		F13 = 124,
		F14 = 125,
		F15 = 126,
		F16 = 127,
		F17 = 128,
		F18 = 129,
		F19 = 130,
		F20 = 131,
		F21 = 132,
		F22 = 133,
		F23 = 134,
		F24 = 135,
		NavigationView = 136,
		NavigationMenu = 137,
		NavigationUp = 138,
		NavigationDown = 139,
		NavigationLeft = 140,
		NavigationRight = 141,
		NavigationAccept = 142,
		NavigationCancel = 143,
		NumberKeyLock = 144,
		Scroll = 145,
		LeftShift = 160,
		RightShift = 161,
		LeftControl = 162,
		RightControl = 163,
		LeftMenu = 164,
		RightMenu = 165,
		GoBack = 166,
		GoForward = 167,
		Refresh = 168,
		Stop = 169,
		Search = 170,
		Favorites = 171,
		GoHome = 172,
		VolumeMute = 173,
		VolumeDown = 174,
		VolumeUp = 175,
		MediaNext = 176,
		MediaPrevious = 177,
		MediaStop = 178,
		MediaPlayPause = 179,

		[Description(";")]
		OEM_1 = 186,

		[Description("+")]
		Plus = 187,

		[Description(",")]
		Comma = 188,

		[Description("-")]
		Minus = 189,

		[Description(".")]
		Period = 190,

		[Description("/")]
		OEM_2 = 191,

		[Description("`")]
		OEM_3 = 192,

		GamepadA = 195,
		GamepadB = 196,
		GamepadX = 197,
		GamepadY = 198,
		GamepadRightShoulder = 199,
		GamepadLeftShoulder = 200,
		GamepadLeftTrigger = 201,
		GamepadRightTrigger = 202,
		GamepadDPadUp = 203,
		GamepadDPadDown = 204,
		GamepadDPadLeft = 205,
		GamepadDPadRight = 206,
		GamepadMenu = 207,
		GamepadView = 208,
		GamepadLeftThumbstickButton = 209,
		GamepadRightThumbstickButton = 210,
		GamepadLeftThumbstickUp = 211,
		GamepadLeftThumbstickDown = 212,
		GamepadLeftThumbstickRight = 213,
		GamepadLeftThumbstickLeft = 214,
		GamepadRightThumbstickUp = 215,
		GamepadRightThumbstickDown = 216,
		GamepadRightThumbstickRight = 217,
		GamepadRightThumbstickLeft = 218,

		[Description("[")]
		OEM_4 = 219,

		[Description(@"\")]
		OEM_5 = 220,

		[Description("]")]
		OEM_6 = 221,

		[Description("'")]
		OEM_7 = 222,

		[Description("<")]
		OEM_102 = 226
	}

	public class ShellProfile
	{
		/// <summary>
		/// Replace all instances of anything resembling a newline, treating pairs of \r\n in either order as a single linebreak.
		/// </summary>
		public static readonly Regex NewlinePattern = new Regex(@"\n\r|\r\n|\r|\n", RegexOptions.Compiled);
		public const int CurrentMigrationVersion = 1;

		public ShellProfile()
		{

		}

		protected ShellProfile(ShellProfile other)
		{
			Id = other.Id;
			PreInstalled = other.PreInstalled;
			Name = other.Name;
			Arguments = other.Arguments;
			Location = other.Location;
			WorkingDirectory = other.WorkingDirectory;
			TabThemeId = other.TabThemeId;
			TerminalThemeId = other.TerminalThemeId;
			UseConPty = other.UseConPty;
			UseBuffer = other.UseBuffer;
			KeyBindings = other.KeyBindings.Select(x => new KeyBinding(x)).ToList();
		}

		public Guid Id { get; set; }
		public bool PreInstalled { get; set; }
		public string Name { get; set; }
		public string Arguments { get; set; }
		public string Location { get; set; }
		public string WorkingDirectory { get; set; }
		public int TabThemeId { get; set; }
		public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
		public bool UseConPty { get; set; }
		public bool UseBuffer { get; set; } = true;

		public int MigrationVersion { get; set; } = CurrentMigrationVersion;

		/// <summary>
		/// For attaching a data to the profile. This property doesn't get serialized nor cloned.
		/// </summary>
		[JsonIgnore]
		public object Tag { get; set; }

		public Guid TerminalThemeId { get; set; }
		public ICollection<KeyBinding> KeyBindings { get; set; } = new List<KeyBinding>();

		public virtual bool EqualTo(ShellProfile other)
		{
			if (other == null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.Id.Equals(Id)
				   && other.PreInstalled.Equals(PreInstalled)
				   && other.Name.NullableEqualTo(Name)
				   && other.Arguments.NullableEqualTo(Arguments)
				   && other.Location.NullableEqualTo(Location)
				   && other.WorkingDirectory.NullableEqualTo(WorkingDirectory)
				   && other.TabThemeId.Equals(TabThemeId)
				   && other.TerminalThemeId.Equals(TerminalThemeId)
				   && other.UseConPty == UseConPty
				   && other.UseBuffer == UseBuffer
				   && other.KeyBindings.SequenceEqual(KeyBindings);
		}

		public virtual ShellProfile Clone() => new ShellProfile(this);
	}

	public static class StringExtensions
	{
		/// <summary>
		/// Compares two strings for equality, but assumes that null string is equal to an empty string.
		/// </summary>
		public static bool NullableEqualTo(this string original, string other,
			StringComparison stringComparison = StringComparison.Ordinal) => string.IsNullOrEmpty(original)
			? string.IsNullOrEmpty(other)
			: original.Equals(other, stringComparison);
	}

	public class TerminalOptions
	{
		public string FontFamily { get; set; }
		public int FontSize { get; set; }

		public int FontWeight { get; set; }

		public CursorStyle CursorStyle { get; set; }

		public bool CursorBlink { get; set; }

		public BellStyle BellStyle { get; set; }

		public ScrollBarStyle ScrollBarStyle { get; set; }

		public double BackgroundOpacity { get; set; }

		public bool UseAcrylicBackground { get; set; }

		public int Padding { get; set; }

		public uint ScrollBackLimit { get; set; }

		public string WordSeparator { get; set; }
	}

	public enum CursorStyle
	{
		Block,
		Underline,
		Bar
	}

	public enum BellStyle
	{
		None,
		Sound
	}

	public enum ScrollBarStyle
	{
		Hidden,
		AutoHiding,
		Visible
	}

	public enum Command
	{
		ToggleWindow,
		NextTab,
		PreviousTab,
		NewTab,
		NewWindow,
		NewSshTab,
		NewSshWindow,
		NewCustomCommandTab,
		NewCustomCommandWindow,
		ChangeTabTitle,
		CloseTab,
		ShowSettings,
		Copy,
		Paste,
		PasteWithoutNewlines,
		Search,
		CloseSearch,
		ToggleFullScreen,
		SelectAll,
		Clear,
		SwitchToTerm1,
		SwitchToTerm2,
		SwitchToTerm3,
		SwitchToTerm4,
		SwitchToTerm5,
		SwitchToTerm6,
		SwitchToTerm7,
		SwitchToTerm8,
		SwitchToTerm9,
		DuplicateTab,
		ReconnectTab,
		IncreaseFontSize,
		DecreaseFontSize,
		ResetFontSize
	}

	public class TerminalSize
	{
		public int Columns { get; set; }

		public int Rows { get; set; }

		public bool EquivalentTo(TerminalSize other) => ReferenceEquals(this, other) ||
														other != null && Columns.Equals(other.Columns) &&
														Rows.Equals(other.Rows);
	}

	public enum SessionType
	{
		Unknown,
		WinPty,
		ConPty
	}

	public enum MouseAction
	{
		None,
		ContextMenu,
		Paste,
		CopySelectionOrPaste
	}

	public enum MouseButton
	{
		Left,
		Middle,
		Right
	}
}
