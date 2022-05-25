using System;

/// <summary>
/// Copyright (c) 2011-2013, Lukas Eder, lukas.eder@gmail.com
/// All rights reserved.
/// 
/// This software is licensed to you under the Apache License, Version 2.0
/// (the "License"); You may obtain a copy of the License at
/// 
///   http://www.apache.org/licenses/LICENSE-2.0
/// 
/// Redistribution and use in source and binary forms, with or without
/// modification, are permitted provided that the following conditions are met:
/// 
/// . Redistributions of source code must retain the above copyright notice, this
///   list of conditions and the following disclaimer.
/// 
/// . Redistributions in binary form must reproduce the above copyright notice,
///   this list of conditions and the following disclaimer in the documentation
///   and/or other materials provided with the distribution.
/// 
/// . Neither the name "jOOU" nor the names of its contributors may be
///   used to endorse or promote products derived from this software without
///   specific prior written permission.
/// 
/// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
/// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
/// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
/// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
/// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
/// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
/// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
/// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
/// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
/// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
/// POSSIBILITY OF SUCH DAMAGE.
/// </summary>
namespace dorkbox.bytes
{

	/// <summary>
	/// The <code>unsigned int</code> type
	/// 
	/// @author Lukas Eder
	/// @author Ed Schaller
	/// </summary>
	public sealed class UInteger : UNumber, IComparable<UInteger>
	{
		private static readonly Type CLASS = typeof(UInteger);
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
		private static readonly string CLASS_NAME = CLASS.FullName;

		/// <summary>
		/// System property name for the property to set the size of the pre-cache.
		/// </summary>
		private static readonly string PRECACHE_PROPERTY = CLASS_NAME + ".precacheSize";

		/// <summary>
		/// Default size for the value cache.
		/// </summary>
		private const int DEFAULT_PRECACHE_SIZE = 256;

		/// <summary>
		/// Generated UID
		/// </summary>
		private const long serialVersionUID = -6821055240959745390L;

		/// <summary>
		/// Cached values
		/// </summary>
		private static readonly UInteger[] VALUES = mkValues();

		/// <summary>
		/// A constant holding the minimum value an <code>unsigned int</code> can
		/// have, 0.
		/// </summary>
		public const long MIN_VALUE = 0x00000000;

		/// <summary>
		/// A constant holding the maximum value an <code>unsigned int</code> can
		/// have, 2<sup>32</sup>-1.
		/// </summary>
		public const long MAX_VALUE = 0xFFFFFFFFL;

		/// <summary>
		/// The value modelling the content of this <code>unsigned int</code>
		/// </summary>
		private readonly long value;

		/// <summary>
		/// Figure out the size of the precache.
		/// </summary>
		/// <returns> The parsed value of the system property
		///         <seealso cref="PRECACHE_PROPERTY"/> or <seealso cref="DEFAULT_PRECACHE_SIZE"/> if
		///         the property is not set, not a number or retrieving results in a
		///         <seealso cref="SecurityException"/>. If the parsed value is zero or
		///         negative no cache will be created. If the value is larger than
		///         <seealso cref="Integer.MAX_VALUE"/> then Integer#MAX_VALUE will be used. </returns>
		private static int PrecacheSize
		{
			get
			{
				return DEFAULT_PRECACHE_SIZE;
			}
		}

		/// <summary>
		/// Generate a cached value for initial unsigned integer values.
		/// </summary>
		/// <returns> Array of cached values for UInteger </returns>
		private static UInteger[] mkValues()
		{
			int precacheSize = PrecacheSize;
			UInteger[] ret;

			if (precacheSize <= 0)
			{
				return null;
			}
			ret = new UInteger[precacheSize];
			for (int i = 0; i < precacheSize; i++)
			{
				ret[i] = new UInteger(i);
			}
			return ret;
		}

		/// <summary>
		/// Unchecked internal constructor. This serves two purposes: first it allows
		/// <seealso cref="UInteger(long)"/> to stay deprecated without warnings and second
		/// constructor without unnecessary value checks.
		/// </summary>
		/// <param name="value"> The value to wrap </param>
		/// <param name="unused"> Unused paramater to distinguish between this and the
		///            deprecated public constructor. </param>
		private UInteger(long value, bool unused)
		{
			this.value = value;
		}

		/// <summary>
		/// Retrieve a cached value.
		/// </summary>
		/// <param name="value"> Cached value to retrieve </param>
		/// <returns> Cached value if one exists. Null otherwise. </returns>
		private static UInteger getCached(long value)
		{
			if (VALUES != null && value < VALUES.Length)
			{
				return VALUES[(int) value];
			}
			return null;
		}

		/// <summary>
		/// Get the value of a long without checking the value.
		/// </summary>
		private static UInteger valueOfUnchecked(long value)
		{
			UInteger cached;

			if ((cached = getCached(value)) != null)
			{
				return cached;
			}
			return new UInteger(value, true);
		}

		/// <summary>
		/// Create an <code>unsigned int</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> does not contain a
		///             parsable <code>unsigned int</code>. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static UInteger valueOf(String value) throws NumberFormatException
		public static UInteger valueOf(string value)
		{
			return valueOfUnchecked(rangeCheck(long.Parse(value)));
		}

		/// <summary>
		/// Create an <code>unsigned int</code> by masking it with
		/// <code>0xFFFFFFFF</code> i.e. <code>(int) -1</code> becomes
		/// <code>(uint) 4294967295</code>
		/// </summary>
		public static UInteger valueOf(int value)
		{
			return valueOfUnchecked(value & MAX_VALUE);
		}

		/// <summary>
		/// Create an <code>unsigned int</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> is not in the range
		///             of an <code>unsigned byte</code> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static UInteger valueOf(long value) throws NumberFormatException
		public static UInteger valueOf(long value)
		{
			return valueOfUnchecked(rangeCheck(value));
		}

		/// <summary>
		/// Create an <code>unsigned int</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> is not in the range
		///             of an <code>unsigned int</code> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private UInteger(long value) throws NumberFormatException
		private UInteger(long value)
		{
			this.value = rangeCheck(value);
		}

		/// <summary>
		/// Create an <code>unsigned int</code> by masking it with
		/// <code>0xFFFFFFFF</code> i.e. <code>(int) -1</code> becomes
		/// <code>(uint) 4294967295</code>
		/// </summary>
		private UInteger(int value)
		{
			this.value = value & MAX_VALUE;
		}

		/// <summary>
		/// Create an <code>unsigned int</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> does not contain a
		///             parsable <code>unsigned int</code>. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private UInteger(String value) throws NumberFormatException
		private UInteger(string value)
		{
			this.value = rangeCheck(long.Parse(value));
		}

		/// <summary>
		/// Throw exception if value out of range (long version)
		/// </summary>
		/// <param name="value"> Value to check </param>
		/// <returns> value if it is in range </returns>
		/// <exception cref="NumberFormatException"> if value is out of range </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private static long rangeCheck(long value) throws NumberFormatException
		private static long rangeCheck(long value)
		{
			if (value < MIN_VALUE || value > MAX_VALUE)
			{
				throw new System.FormatException("Value is out of range : " + value);
			}
			return value;
		}

		/// <summary>
		/// Replace version read through deserialization with cached version.
		/// </summary>
		/// <returns> cached instance of this object's value if one exists, otherwise
		///         this object </returns>
		/// <exception cref="ObjectStreamException"> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private Object readResolve() throws java.io.ObjectStreamException
		private object readResolve()
		{
			UInteger cached;

			// the value read could be invalid so check it
			rangeCheck(this.value);
			if ((cached = getCached(this.value)) != null)
			{
				return cached;
			}
			return this;
		}

		public override int intValue()
		{
			return (int) this.value;
		}

		public override long longValue()
		{
			return this.value;
		}

		public override float floatValue()
		{
			return this.value;
		}

		public override double doubleValue()
		{
			return this.value;
		}

		public override int GetHashCode()
		{
			return Convert.ToInt64(this.value).GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj is UInteger)
			{
				return this.value == ((UInteger) obj).value;
			}

			return false;
		}

		public override string ToString()
		{
			return Convert.ToInt64(this.value).ToString();
		}

		public override string toHexString()
		{
			return this.value.ToString("x");
		}

		public int CompareTo(UInteger o)
		{
			return this.value < o.value ? -1 : this.value == o.value ? 0 : 1;
		}
	}
}