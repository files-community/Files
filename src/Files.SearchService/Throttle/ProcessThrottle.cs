// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Files.SearchService.Throttle;

/// <summary>
/// Keeps the service from being a bad citizen.
/// Sets PROCESS_MODE_BACKGROUND_BEGIN at startup and polls every 2 s
/// to pause index commits when on battery, fullscreen, or CPU &gt; 70%.
/// </summary>
internal static partial class ProcessThrottle
{
	private const uint   PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000;
	private const int    QUNS_RUNNING_D3D_FULL_SCREEN  = 3;
	private const int    QUNS_PRESENTATION_MODE        = 4;
	private const double CpuPauseThreshold             = 0.70;

	private static volatile bool _shouldPause;
	private static Timer?        _pollTimer;

	// Baselines for the next CPU delta — written only by Poll() (timer thread).
	private static long _lastIdle, _lastKernel, _lastUser;

	public static void ApplyBackgroundPriority()
	{
		if (!OperatingSystem.IsWindows()) return;
		SetPriorityClass(Process.GetCurrentProcess().Handle, PROCESS_MODE_BACKGROUND_BEGIN);
	}

	/// <summary>
	/// Starts the 2-second background poll. Call once from RunAsync.
	/// </summary>
	public static void StartPolling()
	{
		if (!OperatingSystem.IsWindows()) return;

		// Seed CPU baseline so the first delta is valid.
		GetSystemTimes(out _lastIdle, out _lastKernel, out _lastUser);

		_pollTimer = new Timer(
			static _ => Poll(),
			null,
			dueTime: TimeSpan.FromSeconds(2),
			period:  TimeSpan.FromSeconds(2));
	}

	/// <summary>
	/// Stops the background poll. Call from OnStop / RunAsync finally.
	/// </summary>
	public static void StopPolling()
	{
		_pollTimer?.Dispose();
		_pollTimer = null;
	}

	/// <summary>
	/// Returns true when index commits should be skipped. Thread-safe read.
	/// </summary>
	public static bool ShouldPause() => _shouldPause;

	// ---- poll --------------------------------------------------------------

	private static void Poll()
	{
		_shouldPause = IsOnBattery() || IsFullscreen() || IsCpuHigh();
	}

	private static bool IsOnBattery()
	{
		if (!GetSystemPowerStatus(out var status)) return false;
		return status.ACLineStatus == 0; // 0 = offline (on battery)
	}

	private static bool IsFullscreen()
	{
		// S_OK == 0; non-zero HRESULT means the call failed (e.g. no shell).
		if (SHQueryUserNotificationState(out int state) != 0) return false;
		return state is QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE;
	}

	private static bool IsCpuHigh()
	{
		if (!GetSystemTimes(out long idle, out long kernel, out long user)) return false;

		long idleDelta   = idle   - _lastIdle;
		long kernelDelta = kernel - _lastKernel;
		long userDelta   = user   - _lastUser;

		_lastIdle   = idle;
		_lastKernel = kernel;
		_lastUser   = user;

		// kernelTime on Windows includes idle time; total = kernel + user.
		long total = kernelDelta + userDelta;
		if (total <= 0) return false;

		double cpuUsage = 1.0 - (double)idleDelta / total;
		return cpuUsage > CpuPauseThreshold;
	}

	// ---- P/Invoke ----------------------------------------------------------

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetPriorityClass(nint handle, uint priorityClass);

	// FILETIME is two consecutive DWORDs (low, high) — maps cleanly to long
	// on little-endian Windows, giving the 100-ns tick count directly.
	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetSystemTimes(
		out long lpIdleTime,
		out long lpKernelTime,
		out long lpUserTime);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

	// Returns HRESULT; pquns receives a QUERY_USER_NOTIFICATION_STATE value.
	[LibraryImport("shell32.dll")]
	private static partial int SHQueryUserNotificationState(out int pquns);

	[StructLayout(LayoutKind.Sequential)]
	private struct SYSTEM_POWER_STATUS
	{
		public byte ACLineStatus;      // 0 = offline (battery), 1 = online (AC)
		public byte BatteryFlag;
		public byte BatteryLifePercent;
		public byte SystemStatusFlag;
		public uint BatteryLifeTime;
		public uint BatteryFullLifeTime;
	}
}
