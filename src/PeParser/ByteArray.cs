using System;
using System.IO;

/*
 * Copyright 2012 dorkbox, llc
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace dorkbox.peParser
{

	using LittleEndian = dorkbox.bytes.LittleEndian;
	using UByte = dorkbox.bytes.UByte;
	using UInteger = dorkbox.bytes.UInteger;
	using ULong = dorkbox.bytes.ULong;
	using UShort = dorkbox.bytes.UShort;


	public class ByteArray : MemoryStream
	{
		public ByteArray(byte[] bytes) : base(bytes, 0, bytes.Length, false, true)
		{
			this.buf = Array.ConvertAll(this.GetBuffer(), x => unchecked((sbyte)(x)));
		}

		public ByteArray(sbyte[] bytes) : base(Array.ConvertAll(bytes, x => unchecked((byte)(x))), 0, bytes.Length, false, true)
		{
			this.buf = bytes;
		}

		public virtual string readAsciiString(int length)
		{
			// pos is incremented by the copybytes method
			return (StringHelper.NewString(copyBytes(length), System.Text.Encoding.ASCII)).Trim();
		}

		private int pos
		{
			get => (int)this.Position;
			set => this.Position = value;
		}

		private sbyte[] buf;

		public virtual ULong readULong(int length)
		{
			ULong result = LittleEndian.ULong_.from(this.buf, this.pos, length);
			this.pos += length;
			return result;
		}

		public virtual UInteger readUInt(int length)
		{
			UInteger result = LittleEndian.UInt_.from(this.buf, this.pos, length);
			this.pos += length;
			return result;
		}

		public virtual UShort readUShort(int length)
		{
			UShort result = LittleEndian.UShort_.from(this.buf, this.pos, length);
			this.pos += length;
			return result;
		}

		public virtual UByte readUByte()
		{
			UByte b = UByte.valueOf(this.buf[this.pos]);
			this.pos++;
			return b;
		}

		public virtual sbyte readRaw(int offset)
		{
			return this.buf[this.pos + offset];
		}

		public virtual sbyte[] copyBytes(int length)
		{
			byte[] data = new byte[length];
			base.Read(data, 0, length);
			return Array.ConvertAll(data, x => unchecked((sbyte)(x)));
		}

		private int marker = 0;

		public virtual void mark()
		{
			marker = this.pos;
		}

		public virtual void seek(int position)
		{
			this.pos = position;
		}

		public virtual int position()
		{
			return this.pos;
		}

		public virtual int marked()
		{
			return this.marker;
		}

        public void reset()
        {
			this.pos = marker;
		}

        public void skip(int v)
        {
			this.pos += v;
        }
    }

}