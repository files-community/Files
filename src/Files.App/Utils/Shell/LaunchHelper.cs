// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides static helper for launching external executable files.
	/// </summary>
	public static class LaunchHelper
	{
		public unsafe static void LaunchSettings(string page)
		{
			PInvoke.CoCreateInstance(typeof(ApplicationActivationManager).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IApplicationActivationManager? pApplicationActivationManager);

			pApplicationActivationManager!.ActivateApplication(
				"windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel",
				page,
				ACTIVATEOPTIONS.AO_NONE,
				out _);
		}

		public static Task<bool> LaunchAppAsync(string application, string? arguments, string? workingDirectory)
		{
			return HandleApplicationLaunch(application, arguments, workingDirectory);
		}

		public static Task<bool> RunCompatibilityTroubleshooterAsync(string filePath)
		{
			var compatibilityTroubleshooterAnswerFile = Path.Combine(Path.GetTempPath(), "CompatibilityTroubleshooterAnswerFile.xml");

			try
			{
				File.WriteAllText(compatibilityTroubleshooterAnswerFile, $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Answers Version=\"1.0\"><Interaction ID=\"IT_LaunchMethod\"><Value>CompatTab</Value></Interaction><Interaction ID=\"IT_BrowseForFile\"><Value>{filePath}</Value></Interaction></Answers>");
			}
			catch (IOException)
			{
				// Try with a different file name
				SafetyExtensions.IgnoreExceptions(() =>
				{
					compatibilityTroubleshooterAnswerFile = Path.Combine(Path.GetTempPath(), "CompatibilityTroubleshooterAnswerFile1.xml");
					File.WriteAllText(compatibilityTroubleshooterAnswerFile, $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Answers Version=\"1.0\"><Interaction ID=\"IT_LaunchMethod\"><Value>CompatTab</Value></Interaction><Interaction ID=\"IT_BrowseForFile\"><Value>{filePath}</Value></Interaction></Answers>");
				});
			}

			return HandleApplicationLaunch("MSDT.exe", $"/id PCWDiagnostic /af \"{compatibilityTroubleshooterAnswerFile}\"", "");
		}

		private static async Task<bool> HandleApplicationLaunch(string application, string? arguments, string? workingDirectory)
		{
			var currentWindows = Win32Helper.GetDesktopWindows();

			if (FileExtensionHelpers.IsVhdFile(application))
			{
				// Use PowerShell to mount Vhd Disk as this requires admin rights
				return await Win32Helper.MountVhdDisk(application);
			}

			var resolvedWorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? PathNormalization.GetParentDir(application) : workingDirectory;

			try
			{
				using Process process = new Process();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.FileName = application;

				// Show window if workingDirectory (opening terminal)
				process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(resolvedWorkingDirectory);

				if (arguments == "RunAs")
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.Verb = "RunAs";

					if (FileExtensionHelpers.IsMsiFile(application))
					{
						process.StartInfo.FileName = "MSIEXEC.exe";
						process.StartInfo.Arguments = $"/a \"{application}\"";
					}
				}
				else if (arguments == "RunAsUser")
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.Verb = "RunAsUser";

					if (FileExtensionHelpers.IsMsiFile(application))
					{
						process.StartInfo.FileName = "MSIEXEC.exe";
						process.StartInfo.Arguments = $"/i \"{application}\"";
					}
				}
				else
				{
					process.StartInfo.Arguments = arguments;

					// Refresh env variables for the child process
					foreach (DictionaryEntry ent in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
					{
						string key = (string)ent.Key;

						// Skip USERNAME to avoid issues where files were executed as SYSTEM user (#12139)
						if (string.Equals(key, "USERNAME", StringComparison.OrdinalIgnoreCase))
							continue;

						process.StartInfo.EnvironmentVariables[key] = (string)ent.Value!;
					}

					foreach (DictionaryEntry ent in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
						process.StartInfo.EnvironmentVariables[(string)ent.Key] = (string)ent.Value!;

					process.StartInfo.EnvironmentVariables["PATH"] = string.Join(';',
						Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
						Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
				}

				process.StartInfo.WorkingDirectory = resolvedWorkingDirectory;
				process.Start();

				Win32Helper.BringToForeground(currentWindows);

				return true;
			}
			catch (Win32Exception ex) when (ex.NativeErrorCode is 193 or 216 && FileExtensionHelpers.IsExecutableFile(application, exeOnly: true))
			{
				// ERROR_EXE_MACHINE_TYPE_MISMATCH (216)
				// "This app can't run on your PC"
				await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_CannotRunFileDialog());
				return false;
			}
			catch (Win32Exception)
			{
				using Process process = new Process();
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = application;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = resolvedWorkingDirectory;

				try
				{
					process.Start();

					Win32Helper.BringToForeground(currentWindows);

					return true;
				}
				catch (Win32Exception ex) when (ex.NativeErrorCode == 50)
				{
					// ShellExecute return code 50 (ERROR_NOT_SUPPORTED) for some exes (#15179)
					return Win32Helper.RunPowershellCommand($"& {Win32Helper.ToPowerShellStringLiteral(application)}", PowerShellExecutionOptions.Hidden);
				}
				catch (Win32Exception)
				{
					try
					{
						var opened = await STATask.Run(async () =>
						{
							var split = application.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).Select(GetMtpPath).ToArray();
							if (split.Length == 1)
							{
								Process.Start(split[0]);

								Win32Helper.BringToForeground(currentWindows);
							}
							else
							{
								var pathsWithAssociations = new List<(string Path, string? Directory, string Association)>(split.Length);
								foreach (var path in split)
								{
									var association = await Win32Helper.GetDefaultFileAssociationAsync(path) ?? Path.GetExtension(path);
									pathsWithAssociations.Add((path, Path.GetDirectoryName(path), association));
								}

								foreach (var group in pathsWithAssociations.GroupBy(x => new { x.Directory, x.Association }))
								{
									if (!group.Any())
										continue;

									using var cMenu = await ContextMenu.GetContextMenuForFiles(group.Select(x => x.Path).ToArray(), PInvoke.CMF_DEFAULTONLY);

									if (cMenu is not null)
										await cMenu.InvokeVerb("open");
								}
							}

							return true;
						}, App.Logger);

						if (!opened)
						{
							if (application.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
							{
								opened = await STATask.Run(async () =>
								{
									using var cMenu = await ContextMenu.GetContextMenuForFiles(new[] { application }, PInvoke.CMF_DEFAULTONLY);

									if (cMenu is not null)
									{
										var menuItems = cMenu.Items
											?? throw new InvalidOperationException("The shell context menu has no item collection.");
										await cMenu.InvokeItem(menuItems.FirstOrDefault()?.ID ?? -1);
									}

									return true;
								}, App.Logger);
							}
						}

						if (!opened)
						{
							var isAlternateStream = RegexHelpers.AlternateStream().IsMatch(application);
							if (isAlternateStream)
							{
								var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
								Directory.CreateDirectory(basePath);

								var tempPath = Path.Combine(basePath, new string(Path.GetFileName(application).SkipWhile(x => x != ':').Skip(1).ToArray()));
								try
								{
									using var hFileSrc = PInvoke.CreateFile(application, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL, null);
									using var hFileDst = PInvoke.CreateFile(tempPath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, null, FILE_CREATION_DISPOSITION.CREATE_ALWAYS, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_READONLY, null);

									if (!hFileSrc.IsInvalid && !hFileDst.IsInvalid)
									{
										// Copy ADS to temp folder and open
										await using (var inStream = new FileStream(hFileSrc, FileAccess.Read))
										await using (var outStream = new FileStream(hFileDst, FileAccess.Write))
										{
											await inStream.CopyToAsync(outStream);
											await outStream.FlushAsync();
										}

										opened = await HandleApplicationLaunch(tempPath, arguments, workingDirectory);
									}
								}
								finally
								{
									void DeleteTemporaryCopy()
									{
										if (File.Exists(tempPath))
											File.SetAttributes(tempPath, FileAttributes.Normal);
										Directory.Delete(basePath, true);
									}

									if (!opened)
									{
										SafetyExtensions.IgnoreExceptions(DeleteTemporaryCopy, App.Logger);
									}
									else
									{
										_ = Task.Run(async () =>
										{
											for (var attempt = 0; attempt < 3; attempt++)
											{
												var delay = attempt switch
												{
													0 => TimeSpan.FromMinutes(1),
													1 => TimeSpan.FromMinutes(5),
													_ => TimeSpan.FromMinutes(30),
												};
												await Task.Delay(delay);
												if (SafetyExtensions.IgnoreExceptions(DeleteTemporaryCopy, App.Logger))
													return;
											}
										});
									}
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
				App.Logger.LogWarning(ex, $"Error launching: {LogPathHelper.RedactPath(application)}");
				return false;
			}
		}

		private static string GetMtpPath(string executable)
		{
			if (executable.StartsWith("\\\\?\\", StringComparison.Ordinal))
			{
				using var computer = new ShellFolder(FOLDERID.FOLDERID_ComputerFolder);
				using var device = computer.FirstOrDefault(i =>
				{
					return i.Name is { } name &&
						executable.Replace("\\\\?\\", "", StringComparison.Ordinal).StartsWith(name, StringComparison.Ordinal);
				});
				var deviceId = device?.ParsingName;
				var itemPath = RegexHelpers.WindowsPath().Replace(executable, "");
				return deviceId is not null ? Path.Combine(deviceId, itemPath) : executable;
			}

			return executable;
		}
	}
}
