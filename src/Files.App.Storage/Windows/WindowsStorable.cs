// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe abstract class WindowsStorable : IWindowsStorable
	{
		/// <inheritdoc/>
		public IShellItem ThisPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		/// <inheritdoc/>
		public IContextMenu ContextMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		/// <inheritdoc/>
		public string Id => this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		/// <inheritdoc/>
		public string Name => this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);

		public static WindowsStorable? TryParse(string parseablePath)
		{
			HRESULT hr = PInvoke.SHCreateItemFromParsingName(parseablePath, null, typeof(IShellItem).GUID, out var shellItemObj);
			var shellItem = (IShellItem)shellItemObj;
			if (shellItem is null)
				return null;

			return TryParse(shellItem);
		}

		public static WindowsStorable? TryParse(IShellItem shellItem)
		{
			bool isFolder = shellItem.GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded && returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(shellItem) : new WindowsFile(shellItem);
		}

		/// <inheritdoc/>
		public unsafe Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			HRESULT hr = ThisPtr.GetParent(out var parentFolder);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Task.FromResult<IFolder?>(null);

			return Task.FromResult<IFolder?>(new WindowsFolder(parentFolder));
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return Equals(obj as IWindowsStorable);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Name);
		}

		/// <inheritdoc/>
		public virtual void Dispose()
		{
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetDisplayName();
		}

		/// <inheritdoc/>
		public unsafe bool Equals(IWindowsStorable? other)
		{
			if (other is null)
				return false;

			return ThisPtr.Compare(other.ThisPtr, (uint)_SICHINTF.SICHINT_DISPLAY, out int order).Succeeded && order is 0;
		}

		public static bool operator ==(WindowsStorable left, WindowsStorable right)
			=> left.Equals(right);

		public static bool operator !=(WindowsStorable left, WindowsStorable right)
			=> !(left == right);
	}
}
