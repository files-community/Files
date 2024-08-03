// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Windows.System;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsAppLauncherService"/>
	public class WindowsAppLauncherService : IWindowsAppLauncherService
	{
		/// <inheritdoc/>
		public async Task<bool> LaunchStorageSensePolicySettingsAsync()
		{
			bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:storagepolicies"));
			return result;
		}

		/// <inheritdoc/>
		public async Task<bool> LaunchProgramCompatibilityTroubleshooterAsync(string path)
		{
			var compatibilityTroubleshooterAnswerFile = SystemIO.Path.Combine(SystemIO.Path.GetTempPath(), "CompatibilityTroubleshooterAnswerFile.xml");
			string troubleshooterAnswerFileText = $@"<?xml version=""1.0"" encoding=""UTF-8""?><Answers Version=""1.0""><Interaction ID=""IT_LaunchMethod""><Value>CompatTab</Value></Interaction><Interaction ID=""IT_BrowseForFile""><Value>{path}</Value></Interaction></Answers>";

			try
			{
				SystemIO.File.WriteAllText(compatibilityTroubleshooterAnswerFile, troubleshooterAnswerFileText);
			}
			catch (SystemIO.IOException)
			{
				// Try with a different file name
				SafetyExtensions.IgnoreExceptions(() =>
				{
					compatibilityTroubleshooterAnswerFile = SystemIO.Path.Combine(SystemIO.Path.GetTempPath(), "CompatibilityTroubleshooterAnswerFile1.xml");
					SystemIO.File.WriteAllText(compatibilityTroubleshooterAnswerFile, troubleshooterAnswerFileText);
				});
			}

			return await LaunchApplicationAsync("MSDT.exe", @$"/id PCWDiagnostic /af ""{compatibilityTroubleshooterAnswerFile}""", "");
		}

		/// <inheritdoc/>
		public async Task<bool> LaunchApplicationAsync(string path, string arguments, string workingDirectory)
		{
			var currentWindows = Win32Helper.GetDesktopWindows();

			// Use PowerShell to mount Vhd Disk as this requires admin rights
			if (FileExtensionHelpers.IsVhdFile(path))
				return await Win32Helper.MountVhdDisk(path);

			try
			{
				using Process process = new Process();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.FileName = path;

				// Show window if workingDirectory (opening terminal)
				process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(workingDirectory);

				if (arguments.Equals("RunAs", StringComparison.OrdinalIgnoreCase))
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.Verb = "RunAs";

					if (FileExtensionHelpers.IsMsiFile(path))
					{
						process.StartInfo.FileName = "MSIEXEC.exe";
						process.StartInfo.Arguments = $"/a \"{path}\"";
					}
				}
				else if (arguments.Equals("RunAsUser", StringComparison.OrdinalIgnoreCase))
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.Verb = "RunAsUser";

					if (FileExtensionHelpers.IsMsiFile(path))
					{
						process.StartInfo.FileName = "MSIEXEC.exe";
						process.StartInfo.Arguments = $"/i \"{path}\"";
					}
				}
				else
				{
					process.StartInfo.Arguments = arguments;

					// Refresh env variables for the child process
					foreach (DictionaryEntry ent in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
						process.StartInfo.EnvironmentVariables[(string)ent.Key] = (string)ent.Value;

					process.StartInfo.EnvironmentVariables["PATH"] = string.Join(';',
						Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
						Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
				}

				process.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? PathNormalization.GetParentDir(path) : workingDirectory;
				process.Start();

				Win32Helper.BringToForeground(currentWindows);

				return true;
			}
			catch (Win32Exception)
			{
				using Process process = new Process();
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = path;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? PathNormalization.GetParentDir(path) : workingDirectory;

				try
				{
					process.Start();

					Win32Helper.BringToForeground(currentWindows);

					return true;
				}
				catch (Win32Exception ex) when (ex.NativeErrorCode == 50)
				{
					// ShellExecute return code 50 (ERROR_NOT_SUPPORTED) for some exes (#15179)
					return Win32Helper.RunPowershellCommand($"\"{path}\"", PowerShellExecutionOptions.Hidden);
				}
				catch (Win32Exception)
				{
					try
					{
						var opened = await Win32Helper.StartSTATask(async () =>
						{
							var split = path.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => GetMtpPath(x));
							if (split.Count() == 1)
							{
								Process.Start(split.First());

								Win32Helper.BringToForeground(currentWindows);
							}
							else
							{
								var groups = split.GroupBy(x => new
								{
									Dir = SystemIO.Path.GetDirectoryName(x),
									Prog = Win32Helper.GetFileAssociationAsync(x).Result ?? SystemIO.Path.GetExtension(x)
								});

								foreach (var group in groups)
								{
									if (!group.Any())
										continue;

									using var cMenu = await ContextMenu.GetContextMenuForFiles(group.ToArray(), Vanara.PInvoke.Shell32.CMF.CMF_DEFAULTONLY);

									if (cMenu is not null)
										await cMenu.InvokeVerb(Vanara.PInvoke.Shell32.CMDSTR_OPEN);
								}
							}

							return true;
						});

						if (!opened)
						{
							if (path.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
							{
								opened = await Win32Helper.StartSTATask(async () =>
								{
									using var cMenu = await ContextMenu.GetContextMenuForFiles(new[] { path }, Vanara.PInvoke.Shell32.CMF.CMF_DEFAULTONLY);

									if (cMenu is not null)
										await cMenu.InvokeItem(cMenu.Items.FirstOrDefault()?.ID ?? -1);

									return true;
								});
							}
						}

						if (!opened)
						{
							var isAlternateStream = RegexHelpers.AlternateStream().IsMatch(path);
							if (isAlternateStream)
							{
								var basePath = SystemIO.Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString("n"));
								Vanara.PInvoke.Kernel32.CreateDirectory(basePath);

								var tempPath = SystemIO.Path.Combine(basePath, new string(SystemIO.Path.GetFileName(path).SkipWhile(x => x != ':').Skip(1).ToArray()));
								using var hFileSrc = Vanara.PInvoke.Kernel32.CreateFile(path, Vanara.PInvoke.Kernel32.FileAccess.GENERIC_READ, SystemIO.FileShare.ReadWrite, null, SystemIO.FileMode.Open, Vanara.PInvoke.FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
								using var hFileDst = Vanara.PInvoke.Kernel32.CreateFile(tempPath, Vanara.PInvoke.Kernel32.FileAccess.GENERIC_WRITE, 0, null, SystemIO.FileMode.Create, Vanara.PInvoke.FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL | Vanara.PInvoke.FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY);

								if (!hFileSrc.IsInvalid && !hFileDst.IsInvalid)
								{
									// Copy ADS to temp folder and open
									using (var inStream = new SystemIO.FileStream(hFileSrc.DangerousGetHandle(), SystemIO.FileAccess.Read))
									using (var outStream = new SystemIO.FileStream(hFileDst.DangerousGetHandle(), SystemIO.FileAccess.Write))
									{
										await inStream.CopyToAsync(outStream);
										await outStream.FlushAsync();
									}

									opened = await LaunchApplicationAsync(tempPath, arguments, workingDirectory);
								}
							}
						}

						return opened;
					}
					catch (Win32Exception)
					{
						// Cannot open file (e.g DLL)
						return false;
					}
					catch (ArgumentException)
					{
						// Cannot open file (e.g DLL)
						return false;
					}
				}
			}
			catch (InvalidOperationException)
			{
				// Invalid file path
				return false;
			}
			catch (Exception ex)
			{
				// Generic error, log
				App.Logger.LogWarning(ex, $"Error launching: {path}");
				return false;
			}

			string GetMtpPath(string executable)
			{
				if (executable.StartsWith("\\\\?\\", StringComparison.Ordinal))
				{
					using var computer = new Vanara.Windows.Shell.ShellFolder(Vanara.PInvoke.Shell32.KNOWNFOLDERID.FOLDERID_ComputerFolder);
					using var device = computer.FirstOrDefault(i => executable.Replace("\\\\?\\", "", StringComparison.Ordinal).StartsWith(i.Name, StringComparison.Ordinal));
					var deviceId = device?.ParsingName;
					var itemPath = RegexHelpers.WindowsPath().Replace(executable, "");
					return deviceId is not null ? SystemIO.Path.Combine(deviceId, itemPath) : executable;
				}

				return executable;
			}
		}
	}
}
