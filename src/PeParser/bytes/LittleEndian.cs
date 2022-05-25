using System;
using System.IO;
using System.Numerics;

/*
 * Copyright 2014 dorkbox, llc
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
namespace dorkbox.bytes
{

    /// <summary>
    /// This is intel/amd/arm arch!
    /// <p/>
    /// arm is technically bi-endian
    /// <p/>
    /// Network byte order IS big endian, as is Java.
    /// </summary>
    public class LittleEndian
    {
        // the following are ALL in Little-Endian (byte[0] is LEAST significant)

        /// <summary>
        /// CHAR to and from bytes
        /// </summary>
        public sealed class Char_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static char from(final byte[] bytes, final int offset, final int byteNum)
            public static char from(in sbyte[] bytes, in int offset, in int byteNum)
            {
                char number = (char)0;

                switch (byteNum)
                {
                    case 2:
                        number |= (char)((bytes[offset + 1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (char)((bytes[offset + 0] & 0xFF) << 0);
                        break;
                }

                return number;
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static char from(final byte[] bytes)
            public static char from(in sbyte[] bytes)
            {
                char number = (char)0;

                switch (bytes.Length)
                {
                    default:
                        goto case 2;
                    case 2:
                        number |= (char)((bytes[1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (char)((bytes[0] & 0xFF) << 0);
                        break;
                }

                return number;
            }

            public static char from(in sbyte b0, in sbyte b1)
            {
                return (char)((b1 & 0xFF) << 8 | (b0 & 0xFF) << 0);
            }

            public static char from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static char from(final java.io.InputStream inputStream) throws java.io.IOException
            public static char from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(in char x)
            {
                return new sbyte[] { (sbyte)(x >> 0), (sbyte)(x >> 8) };
            }

            public static void toBytes(in char x, in sbyte[] bytes, in int offset)
            {
                bytes[offset + 1] = (sbyte)(x >> 8);
                bytes[offset + 0] = (sbyte)(x >> 0);
            }

            public static void toBytes(in char x, in sbyte[] bytes)
            {
                bytes[1] = (sbyte)(x >> 8);
                bytes[0] = (sbyte)(x >> 0);
            }


            internal Char_()
            {
            }
        }


        /// <summary>
        /// UNSIGNED CHAR to and from bytes
        /// </summary>
        public sealed class UChar_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UShort from(final byte[] bytes, final int offset, final int bytenum)
            public static UShort from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                char number = (char)0;

                switch (bytenum)
                {
                    case 2:
                        number |= (char)((bytes[offset + 1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (char)((bytes[offset + 0] & 0xFF) << 0);
                        break;
                }

                return UShort.valueOf(number);
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UShort from(final byte[] bytes)
            public static UShort from(in sbyte[] bytes)
            {
                short number = 0;

                switch (bytes.Length)
                {
                    default:
                        goto case 2;
                    case 2:
                        number |= (short)((bytes[1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (short)((bytes[0] & 0xFF) << 0);
                        break;
                }

                return UShort.valueOf(number);
            }

            public static UShort from(in sbyte b0, in sbyte b1)
            {
                return UShort.valueOf((short)((b1 & 0xFF) << 8) | (b0 & 0xFF) << 0);
            }

            public static UShort from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static UShort from(final java.io.InputStream inputStream) throws java.io.IOException
            public static UShort from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(UShort x)
            {
                int num = x.intValue();

                return new sbyte[] { (sbyte)(num & 0x00FF >> 0), (sbyte)((num & 0xFF00) >> 8) };
            }

            public static void toBytes(in UShort x, in sbyte[] bytes, in int offset)
            {
                int num = x.intValue();

                bytes[offset + 1] = (sbyte)((num & 0xFF00) >> 8);
                bytes[offset + 0] = (sbyte)(num & 0x00FF >> 0);
            }

            public static void toBytes(in UShort x, in sbyte[] bytes)
            {
                int num = x.intValue();

                bytes[1] = (sbyte)((num & 0xFF00) >> 8);
                bytes[0] = (sbyte)(num & 0x00FF >> 0);
            }

            internal UChar_()
            {
            }
        }

        /// <summary>
        /// SHORT to and from bytes
        /// </summary>
        public sealed class Short_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static short from(final byte[] bytes, final int offset, final int bytenum)
            public static short from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                short number = 0;

                switch (bytenum)
                {
                    case 2:
                        number |= (short)((bytes[offset + 1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (short)((bytes[offset + 0] & 0xFF) << 0);
                        break;
                }

                return number;
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static short from(final byte[] bytes)
            public static short from(in sbyte[] bytes)
            {
                short number = 0;

                switch (bytes.Length)
                {
                    default:
                        goto case 2;
                    case 2:
                        number |= (short)((bytes[1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (short)((bytes[0] & 0xFF) << 0);
                        break;
                }

                return number;
            }

            public static short from(in sbyte b0, in sbyte b1)
            {
                return (short)((b1 & 0xFF) << 8 | (b0 & 0xFF) << 0);
            }

            public static short from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static short from(final java.io.InputStream inputStream) throws java.io.IOException
            public static short from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(in short x)
            {
                return new sbyte[] { (sbyte)(x >> 0), (sbyte)(x >> 8) };
            }

            public static void toBytes(in short x, in sbyte[] bytes, in int offset)
            {
                bytes[offset + 1] = (sbyte)(x >> 8);
                bytes[offset + 0] = (sbyte)(x >> 0);
            }

            public static void toBytes(in short x, in sbyte[] bytes)
            {
                bytes[1] = (sbyte)(x >> 8);
                bytes[0] = (sbyte)(x >> 0);
            }

            internal Short_()
            {
            }
        }


        /// <summary>
        /// UNSIGNED SHORT to and from bytes
        /// </summary>
        public sealed class UShort_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UShort from(final byte[] bytes, final int offset, final int bytenum)
            public static UShort from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                short number = 0;

                switch (bytenum)
                {
                    case 2:
                        number |= (short)((bytes[offset + 1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (short)((bytes[offset + 0] & 0xFF) << 0);
                        break;
                }

                return UShort.valueOf(number);
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UShort from(final byte[] bytes)
            public static UShort from(in sbyte[] bytes)
            {
                short number = 0;

                switch (bytes.Length)
                {
                    default:
                        goto case 2;
                    case 2:
                        number |= (short)((bytes[1] & 0xFF) << 8);
                        goto case 1;
                    case 1:
                        number |= (short)((bytes[0] & 0xFF) << 0);
                        break;
                }

                return UShort.valueOf(number);
            }

            public static UShort from(in sbyte b0, in sbyte b1)
            {
                return UShort.valueOf((short)((b1 & 0xFF) << 8 | (b0 & 0xFF) << 0));
            }

            public static UShort from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static UShort from(final java.io.InputStream inputStream) throws java.io.IOException
            public static UShort from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }
            public static sbyte[] toBytes(in UShort x)
            {
                int num = x.intValue();

                return new sbyte[] { (sbyte)(num & 0x00FF >> 0), (sbyte)((num & 0xFF00) >> 8) };
            }

            public static void toBytes(in UShort x, in sbyte[] bytes, in int offset)
            {
                int num = x.intValue();

                bytes[offset + 1] = (sbyte)((num & 0xFF00) >> 8);
                bytes[offset + 0] = (sbyte)(num & 0x00FF >> 0);
            }

            public static void toBytes(in UShort x, in sbyte[] bytes)
            {
                int num = x.intValue();

                bytes[1] = (sbyte)((num & 0xFF00) >> 8);
                bytes[0] = (sbyte)(num & 0x00FF >> 0);
            }

            internal UShort_()
            {
            }
        }

        /// <summary>
        /// INT to and from bytes
        /// </summary>
        public sealed class Int_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static int from(final byte[] bytes, final int offset, final int bytenum)
            public static int from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                int number = 0;

                switch (bytenum)
                {
                    case 4:
                        number |= (bytes[offset + 3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (bytes[offset + 2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (bytes[offset + 1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (bytes[offset + 0] & 0xFF) << 0;
                        break;
                }

                return number;
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static int from(final byte[] bytes)
            public static int from(in sbyte[] bytes)
            {
                int number = 0;

                switch (bytes.Length)
                {
                    default:
                        goto case 4;
                    case 4:
                        number |= (bytes[3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (bytes[2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (bytes[1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (bytes[0] & 0xFF) << 0;
                        break;
                }

                return number;
            }

            public static int from(in sbyte b0, in sbyte b1, in sbyte b2, in sbyte b3)
            {
                return (b3 & 0xFF) << 24 | (b2 & 0xFF) << 16 | (b1 & 0xFF) << 8 | (b0 & 0xFF) << 0;
            }

            public static int from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get(), buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static int from(final java.io.InputStream inputStream) throws java.io.IOException
            public static int from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(in int x)
            {
                return new sbyte[] { (sbyte)(x >> 0), (sbyte)(x >> 8), (sbyte)(x >> 16), (sbyte)(x >> 24) };
            }

            public static void toBytes(in int x, in sbyte[] bytes, in int offset)
            {
                bytes[offset + 3] = (sbyte)(x >> 24);
                bytes[offset + 2] = (sbyte)(x >> 16);
                bytes[offset + 1] = (sbyte)(x >> 8);
                bytes[offset + 0] = (sbyte)(x >> 0);
            }

            public static void toBytes(in int x, in sbyte[] bytes)
            {
                bytes[3] = (sbyte)(x >> 24);
                bytes[2] = (sbyte)(x >> 16);
                bytes[1] = (sbyte)(x >> 8);
                bytes[0] = (sbyte)(x >> 0);
            }

            internal Int_()
            {
            }
        }

        /// <summary>
        /// UNSIGNED INT to and from bytes
        /// </summary>
        public sealed class UInt_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UInteger from(final byte[] bytes, final int offset, final int bytenum)
            public static UInteger from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                int number = 0;

                switch (bytenum)
                {
                    case 4:
                        number |= (bytes[offset + 3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (bytes[offset + 2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (bytes[offset + 1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (bytes[offset + 0] & 0xFF) << 0;
                        break;
                }

                return UInteger.valueOf(number);
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static UInteger from(final byte[] bytes)
            public static UInteger from(in sbyte[] bytes)
            {
                int number = 0;

                switch (bytes.Length)
                {
                    default:
                        goto case 4;
                    case 4:
                        number |= (bytes[3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (bytes[2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (bytes[1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (bytes[0] & 0xFF) << 0;
                        break;
                }

                return UInteger.valueOf(number);
            }

            public static UInteger from(in sbyte b0, in sbyte b1, in sbyte b2, in sbyte b3)
            {
                int number = (b3 & 0xFF) << 24 | (b2 & 0xFF) << 16 | (b1 & 0xFF) << 8 | (b0 & 0xFF) << 0;

                return UInteger.valueOf(number);
            }

            public static UInteger from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get(), buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static UInteger from(final java.io.InputStream inputStream) throws java.io.IOException
            public static UInteger from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(in UInteger x)
            {
                long num = x.longValue();

                return new sbyte[] { (sbyte)(num & 0x000000FFL >> 0), (sbyte)((num & 0x0000FF00L) >> 8), (sbyte)((num & 0x00FF0000L) >> 16), (sbyte)((num & 0xFF000000L) >> 24) };
            }

            public static void toBytes(in UInteger x, in sbyte[] bytes, in int offset)
            {
                long num = x.longValue();

                bytes[offset + 3] = (sbyte)((num & 0xFF000000L) >> 24);
                bytes[offset + 2] = (sbyte)((num & 0x00FF0000L) >> 16);
                bytes[offset + 1] = (sbyte)((num & 0x0000FF00L) >> 8);
                bytes[offset + 0] = (sbyte)(num & 0x000000FFL >> 0);
            }


            public static void toBytes(in UInteger x, in sbyte[] bytes)
            {
                long num = x.longValue();

                bytes[3] = (sbyte)((num & 0xFF000000L) >> 24);
                bytes[2] = (sbyte)((num & 0x00FF0000L) >> 16);
                bytes[1] = (sbyte)((num & 0x0000FF00L) >> 8);
                bytes[0] = (sbyte)(num & 0x000000FFL >> 0);
            }

            internal UInt_()
            {
            }
        }

        public sealed class Long_
        {
            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static long from(final byte[] bytes, final int offset, final int bytenum)
            public static long from(in sbyte[] bytes, in int offset, in int bytenum)
            {
                long number = 0;

                switch (bytenum)
                {
                    case 8:
                        number |= (long)(bytes[offset + 7] & 0xFF) << 56;
                        goto case 7;
                    case 7:
                        number |= (long)(bytes[offset + 6] & 0xFF) << 48;
                        goto case 6;
                    case 6:
                        number |= (long)(bytes[offset + 5] & 0xFF) << 40;
                        goto case 5;
                    case 5:
                        number |= (long)(bytes[offset + 4] & 0xFF) << 32;
                        goto case 4;
                    case 4:
                        number |= (long)(bytes[offset + 3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (long)(bytes[offset + 2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (long)(bytes[offset + 1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (long)(bytes[offset + 0] & 0xFF) << 0;
                        break;
                }

                return number;
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @SuppressWarnings("fallthrough") public static long from(final byte[] bytes)
            public static long from(in sbyte[] bytes)
            {
                long number = 0L;

                switch (bytes.Length)
                {
                    default:
                        goto case 8;
                    case 8:
                        number |= (long)(bytes[7] & 0xFF) << 56;
                        goto case 7;
                    case 7:
                        number |= (long)(bytes[6] & 0xFF) << 48;
                        goto case 6;
                    case 6:
                        number |= (long)(bytes[5] & 0xFF) << 40;
                        goto case 5;
                    case 5:
                        number |= (long)(bytes[4] & 0xFF) << 32;
                        goto case 4;
                    case 4:
                        number |= (long)(bytes[3] & 0xFF) << 24;
                        goto case 3;
                    case 3:
                        number |= (long)(bytes[2] & 0xFF) << 16;
                        goto case 2;
                    case 2:
                        number |= (long)(bytes[1] & 0xFF) << 8;
                        goto case 1;
                    case 1:
                        number |= (long)(bytes[0] & 0xFF) << 0;
                        break;
                }

                return number;
            }

            public static long from(in sbyte b0, in sbyte b1, in sbyte b2, in sbyte b3, in sbyte b4, in sbyte b5, in sbyte b6, in sbyte b7)
            {
                return (long)(b7 & 0xFF) << 56 | (long)(b6 & 0xFF) << 48 | (long)(b5 & 0xFF) << 40 | (long)(b4 & 0xFF) << 32 | (long)(b3 & 0xFF) << 24 | (long)(b2 & 0xFF) << 16 | (long)(b1 & 0xFF) << 8 | (long)(b0 & 0xFF) << 0;
            }

            public static long from(in ByteBuffer buff)
            {
                return from(buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get());
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
            //ORIGINAL LINE: public static long from(final java.io.InputStream inputStream) throws java.io.IOException
            public static long from(in Stream inputStream)
            {
                return from((sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte(), (sbyte)inputStream.ReadByte());
            }

            public static sbyte[] toBytes(in long x)
            {
                return new sbyte[] { (sbyte)(x >> 0), (sbyte)(x >> 8), (sbyte)(x >> 16), (sbyte)(x >> 24), (sbyte)(x >> 32), (sbyte)(x >> 40), (sbyte)(x >> 48), (sbyte)(x >> 56) };
            }

            public static void toBytes(in long x, in sbyte[] bytes, in int offset)
            {
                bytes[offset + 7] = (sbyte)(x >> 56);
                bytes[offset + 6] = (sbyte)(x >> 48);
                bytes[offset + 5] = (sbyte)(x >> 40);
                bytes[offset + 4] = (sbyte)(x >> 32);
                bytes[offset + 3] = (sbyte)(x >> 24);
                bytes[offset + 2] = (sbyte)(x >> 16);
                bytes[offset + 1] = (sbyte)(x >> 8);
                bytes[offset + 0] = (sbyte)(x >> 0);
            }

            public static void toBytes(in long x, in sbyte[] bytes)
            {
                bytes[7] = (sbyte)(x >> 56);
                bytes[6] = (sbyte)(x >> 48);
                bytes[5] = (sbyte)(x >> 40);
                bytes[4] = (sbyte)(x >> 32);
                bytes[3] = (sbyte)(x >> 24);
                bytes[2] = (sbyte)(x >> 16);
                bytes[1] = (sbyte)(x >> 8);
                bytes[0] = (sbyte)(x >> 0);
            }

            internal Long_()
            {
            }
        }

        /// <summary>
        /// UNSIGNED LONG to and from bytes
        /// </summary>
        public sealed class ULong_
		{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("fallthrough") public static ULong from(final byte[] bytes, final int offset, final int bytenum)
			public static ULong from(in sbyte[] bytes, in int offset, in int bytenum)
			{
				long number = 0;

				switch (bytenum)
				{
					case 8:
						number |= (long)(bytes[offset + 7] & 0xFF) << 56;
						goto case 7;
					case 7:
						number |= (long)(bytes[offset + 6] & 0xFF) << 48;
						goto case 6;
					case 6:
						number |= (long)(bytes[offset + 5] & 0xFF) << 40;
						goto case 5;
					case 5:
						number |= (long)(bytes[offset + 4] & 0xFF) << 32;
						goto case 4;
					case 4:
						number |= (long)(bytes[offset + 3] & 0xFF) << 24;
						goto case 3;
					case 3:
						number |= (long)(bytes[offset + 2] & 0xFF) << 16;
						goto case 2;
					case 2:
						number |= (long)(bytes[offset + 1] & 0xFF) << 8;
						goto case 1;
					case 1:
						number |= (long)(bytes[offset + 0] & 0xFF) << 0;
					break;
				}

				return ULong.valueOf(number);
			}

			public static ULong from(in sbyte[] bytes)
			{
                BigInteger @ulong = new BigInteger(Array.ConvertAll(bytes, x => unchecked((byte)(x))));
				return ULong.valueOf(@ulong);
			}

			public static ULong from(in sbyte b0, in sbyte b1, in sbyte b2, in sbyte b3, in sbyte b4, in sbyte b5, in sbyte b6, in sbyte b7)
			{
				sbyte[] bytes = new sbyte[] {b7, b6, b5, b4, b3, b2, b1, b0};
                BigInteger @ulong = new BigInteger(Array.ConvertAll(bytes, x => unchecked((byte)(x))));
                return ULong.valueOf(@ulong);
			}

			public static ULong from(in ByteBuffer buff)
			{
				return from(buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get(), buff.get());
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static ULong from(final java.io.InputStream inputStream) throws java.io.IOException
			public static ULong from(in Stream inputStream)
			{
				return from((sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte(), (sbyte) inputStream.ReadByte());
			}

			public static sbyte[] toBytes(in ULong x)
			{
				sbyte[] bytes = new sbyte[8];
				int offset = 0;

				sbyte[] temp_byte = Array.ConvertAll(x.toBigInteger().ToByteArray(), x => unchecked((sbyte)(x)));
				int array_count = temp_byte.Length - 1;

				for (int i = 7; i >= 0; i--)
				{
					if (array_count >= 0)
					{
						bytes[offset] = temp_byte[array_count];
					}
					else
					{
						bytes[offset] = (sbyte) 0x0;
					}

					offset++;
					array_count--;
				}

				return bytes;
			}

			public static void toBytes(in ULong x, in sbyte[] bytes, in int offset)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes1 = toBytes(x);
				sbyte[] bytes1 = toBytes(x);
				int length = bytes.Length;
				int pos = 8;

				while (length > 0)
				{
					bytes[pos--] = bytes1[offset + length--];
				}
			}

			public static void toBytes(in ULong x, in sbyte[] bytes)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes1 = toBytes(x);
				sbyte[] bytes1 = toBytes(x);
				int length = bytes.Length;
				int pos = 8;

				while (length > 0)
				{
					bytes[pos--] = bytes1[length--];
				}
			}

			internal ULong_()
			{
			}
		}
	}


}