// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public abstract class WindowsStorable : IWindowsStorable
	{
		/// <inheritdoc/>
		public IShellItem ThisPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		} = null!;

		/// <inheritdoc/>
		public IContextMenu? ContextMenu
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

		public static WindowsStorable? TryParse(string szPath)
		{
			HRESULT hr = PInvoke.SHCreateItemFromParsingName(szPath, null, out IShellItem pShellItem);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return null;

			return TryParse(pShellItem);
		}

		public static WindowsStorable? TryParse(IShellItem? pShellItem)
		{
			if (pShellItem is null)
				return null;

			bool isFolder = pShellItem.GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded && returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

		/// <inheritdoc/>
		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			HRESULT hr = ThisPtr.GetParent(out var pParentFolder);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Task.FromResult<IFolder?>(null);

			return Task.FromResult<IFolder?>(new WindowsFolder(pParentFolder));
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
			ContextMenu = null;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetDisplayName();
		}

		/// <inheritdoc/>
		public bool Equals(IWindowsStorable? other)
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
