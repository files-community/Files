//-------------------------------------------------------------------------------------------
//	Copyright © 2007 - 2022 Tangible Software Solutions, Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class is used to replicate the java.nio.ByteBuffer class in C#.
//
//	Instances are only obtainable via the static 'allocate' method.
//
//	Some methods are not available:
//		All methods which create shared views of the buffer such as: array,
//		asCharBuffer, asDoubleBuffer, asFloatBuffer, asIntBuffer, asLongBuffer,
//		asReadOnlyBuffer, asShortBuffer, duplicate, slice, & wrap.
//
//		Other methods such as: mark, reset, isReadOnly, order, compareTo,
//		arrayOffset, & the limit setter method.
//-------------------------------------------------------------------------------------------
using System.IO;

namespace dorkbox.bytes
{
	public class ByteBuffer
	{
		//'Mode' is only used to determine whether to return data length or capacity from the 'limit' method:
		private enum Mode
		{
			Read,
			Write
		}
		private Mode mode;

		private MemoryStream stream;
		private BinaryReader reader;
		private BinaryWriter writer;

		private ByteBuffer()
		{
			stream = new MemoryStream();
			reader = new BinaryReader(stream);
			writer = new BinaryWriter(stream);
		}

		~ByteBuffer()
		{
			reader.Close();
			writer.Close();
			stream.Close();
			stream.Dispose();
		}

		public static ByteBuffer allocate(int capacity)
		{
			ByteBuffer buffer = new ByteBuffer();
			buffer.stream.Capacity = capacity;
			buffer.mode = Mode.Write;
			return buffer;
		}

		public static ByteBuffer allocateDirect(int capacity)
		{
			//this wrapper class makes no distinction between 'allocate' & 'allocateDirect'
			return allocate(capacity);
		}

		public int capacity()
		{
			return stream.Capacity;
		}

		public ByteBuffer flip()
		{
			mode = Mode.Read;
			stream.SetLength(stream.Position);
			stream.Position = 0;
			return this;
		}

		public ByteBuffer clear()
		{
			mode = Mode.Write;
			stream.Position = 0;
			return this;
		}

		public ByteBuffer compact()
		{
			mode = Mode.Write;
			MemoryStream newStream = new MemoryStream(stream.Capacity);
			stream.CopyTo(newStream);
			stream = newStream;
			return this;
		}

		public ByteBuffer rewind()
		{
			stream.Position = 0;
			return this;
		}

		public long limit()
		{
			if (mode == Mode.Write)
				return stream.Capacity;
			else
				return stream.Length;
		}

		public long position()
		{
			return stream.Position;
		}

		public ByteBuffer position(long newPosition)
		{
			stream.Position = newPosition;
			return this;
		}

		public long remaining()
		{
			return this.limit() - this.position();
		}

		public bool hasRemaining()
		{
			return this.remaining() > 0;
		}

		public sbyte get()
		{
			return (sbyte)stream.ReadByte();
		}

		public ByteBuffer get(byte[] dst, int offset, int length)
		{
			stream.Read(dst, offset, length);
			return this;
		}

		public ByteBuffer put(byte b)
		{
			stream.WriteByte(b);
			return this;
		}

		public ByteBuffer put(byte[] src, int offset, int length)
		{
			stream.Write(src, offset, length);
			return this;
		}

		public bool Equals(ByteBuffer other)
		{
			if (other != null && this.remaining() == other.remaining())
			{
				long thisOriginalPosition = this.position();
				long otherOriginalPosition = other.position();

				bool differenceFound = false;
				while (stream.Position < stream.Length)
				{
					if (this.get() != other.get())
					{
						differenceFound = true;
						break;
					}
				}

				this.position(thisOriginalPosition);
				other.position(otherOriginalPosition);

				return !differenceFound;
			}
			else
				return false;
		}

		//methods using the internal BinaryReader:
		public char getChar()
		{
			return reader.ReadChar();
		}
		public char getChar(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			char value = reader.ReadChar();
			stream.Position = originalPosition;
			return value;
		}
		public double getDouble()
		{
			return reader.ReadDouble();
		}
		public double getDouble(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			double value = reader.ReadDouble();
			stream.Position = originalPosition;
			return value;
		}
		public float getFloat()
		{
			return reader.ReadSingle();
		}
		public float getFloat(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			float value = reader.ReadSingle();
			stream.Position = originalPosition;
			return value;
		}
		public int getInt()
		{
			return reader.ReadInt32();
		}
		public int getInt(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			int value = reader.ReadInt32();
			stream.Position = originalPosition;
			return value;
		}
		public long getLong()
		{
			return reader.ReadInt64();
		}
		public long getLong(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			long value = reader.ReadInt64();
			stream.Position = originalPosition;
			return value;
		}
		public short getShort()
		{
			return reader.ReadInt16();
		}
		public short getShort(int index)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			short value = reader.ReadInt16();
			stream.Position = originalPosition;
			return value;
		}

		//methods using the internal BinaryWriter:
		public ByteBuffer putChar(char value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putChar(int index, char value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
		public ByteBuffer putDouble(double value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putDouble(int index, double value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
		public ByteBuffer putFloat(float value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putFloat(int index, float value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
		public ByteBuffer putInt(int value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putInt(int index, int value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
		public ByteBuffer putLong(long value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putLong(int index, long value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
		public ByteBuffer putShort(short value)
		{
			writer.Write(value);
			return this;
		}
		public ByteBuffer putShort(int index, short value)
		{
			long originalPosition = stream.Position;
			stream.Position = index;
			writer.Write(value);
			stream.Position = originalPosition;
			return this;
		}
	}
}