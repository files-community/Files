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
	/// The <code>unsigned short</code> type
	/// 
	/// @author Lukas Eder
	/// </summary>
	public sealed class UShort : UNumber, IComparable<UShort>
	{

		/// <summary>
		/// Generated UID
		/// </summary>
		private const long serialVersionUID = -6821055240959745390L;

		/// <summary>
		/// A constant holding the minimum value an <code>unsigned short</code> can
		/// have, 0.
		/// </summary>
		public const int MIN_VALUE = 0x0000;

		/// <summary>
		/// A constant holding the maximum value an <code>unsigned short</code> can
		/// have, 2<sup>16</sup>-1.
		/// </summary>
		public const int MAX_VALUE = 0xffff;

		/// <summary>
		/// The value modelling the content of this <code>unsigned short</code>
		/// </summary>
		private readonly int value;

		/// <summary>
		/// Create an <code>unsigned short</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> does not contain a
		///             parsable <code>unsigned short</code>. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static UShort valueOf(String value) throws NumberFormatException
		public static UShort valueOf(string value)
		{
			return new UShort(value);
		}

		/// <summary>
		/// Create an <code>unsigned short</code> by masking it with
		/// <code>0xFFFF</code> i.e. <code>(short) -1</code> becomes
		/// <code>(ushort) 65535</code>
		/// </summary>
		public static UShort valueOf(short value)
		{
			return new UShort(value);
		}

		/// <summary>
		/// Create an <code>unsigned short</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> is not in the range
		///             of an <code>unsigned short</code> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static UShort valueOf(int value) throws NumberFormatException
		public static UShort valueOf(int value)
		{
			return new UShort(value);
		}

		/// <summary>
		/// Create an <code>unsigned short</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> is not in the range
		///             of an <code>unsigned short</code> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private UShort(int value) throws NumberFormatException
		private UShort(int value)
		{
			this.value = value;
			rangeCheck();
		}

		/// <summary>
		/// Create an <code>unsigned short</code> by masking it with
		/// <code>0xFFFF</code> i.e. <code>(short) -1</code> becomes
		/// <code>(ushort) 65535</code>
		/// </summary>
		private UShort(short value)
		{
			this.value = value & MAX_VALUE;
		}

		/// <summary>
		/// Create an <code>unsigned short</code>
		/// </summary>
		/// <exception cref="NumberFormatException"> If <code>value</code> does not contain a
		///             parsable <code>unsigned short</code>. </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private UShort(String value) throws NumberFormatException
		private UShort(string value)
		{
			this.value = int.Parse(value);
			rangeCheck();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private void rangeCheck() throws NumberFormatException
		private void rangeCheck()
		{
			if (this.value < MIN_VALUE || this.value > MAX_VALUE)
			{
				throw new System.FormatException("Value is out of range : " + this.value);
			}
		}

		public override int intValue()
		{
			return this.value;
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
			return Convert.ToInt32(this.value).GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is UShort)
			{
				return this.value == ((UShort) obj).value;
			}

			return false;
		}

		public override string ToString()
		{
			return Convert.ToInt32(this.value).ToString();
		}

		public override string toHexString()
		{
			return this.value.ToString("x");
		}

		public int CompareTo(UShort o)
		{
			return this.value < o.value ? -1 : this.value == o.value ? 0 : 1;
		}
	}
}