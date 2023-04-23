// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Backend.SecureStore
{
	public abstract class FreeableStore<TImplementation> : IDisposable, IEquatable<TImplementation>
		where TImplementation : class
	{
		protected bool disposed;

		public override bool Equals(object? obj)
		{
			return obj is TImplementation objImpl ? Equals(objImpl) : base.Equals(obj);
		}

		public abstract TImplementation CreateCopy();

		public abstract bool Equals(TImplementation? other);

		public abstract override int GetHashCode();

		protected abstract void SecureFree();

		public void Dispose()
		{
			SecureFree();
			disposed = true;
		}
	}
}
