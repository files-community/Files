using System;
using System.Text;

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
namespace dorkbox.peParser.types
{

	using UInteger = dorkbox.bytes.UInteger;
	using ByteArray = dorkbox.peParser.ByteArray;
	using ResourceTypes = dorkbox.peParser.misc.ResourceTypes;

	public class ResourceDirName : ByteDefinition<string>
	{

		private const int NAME_IS_STRING_MASK = unchecked((int)0x80000000);
		private const int NAME_OFFSET_MASK = 0x7FFFFFFF;

		private readonly string value;
		private readonly int level;

		public ResourceDirName(UInteger intValue, string descriptiveName, ByteArray bytes, int level) : base(descriptiveName)
		{

			this.level = level;

			/*
			* This field contains either an integer ID or a pointer to a structure that contains a string name.
			*
			* If the high bit (0x80000000) is zero, this field is interpreted as an integer ID.
			*
			* If the high bit is nonzero, the lower 31 bits are an offset (relative to the start of the resources) to an
			* IMAGE_RESOURCE_DIR_STRING_U structure. This structure contains a WORD character count, followed by a UNICODE
			* string with the resource name.
			*
			* Yes, even PE files intended for non-UNICODE Win32 implementations use UNICODE here. To convert the UNICODE
			* string to an ANSI string, use the WideCharToMultiByte function.
			*/
			long valueInt = intValue.longValue();

			// now process the name
			bool isString = 0 != (valueInt & NAME_IS_STRING_MASK);

			if (isString)
			{
				int savedPosition = bytes.position();
				//
				// High bit is 1
				//
				long offset = valueInt & NAME_OFFSET_MASK;

				if (offset > int.MaxValue)
				{
					throw new Exception("Unable to set offset to more than 2gb!");
				}

				// offset from the start of the resource data to the name string of this particular resource.
				bytes.seek(bytes.marked() + (int) offset);
				int length = bytes.readUShort(2).intValue();

				sbyte[] buff = new sbyte[length * 2]; // UTF-8 chars are 16 bits = 2
				// bytes
				for (int i = 0; i < buff.Length; i++)
				{
					buff[i] = bytes.readUByte().byteValue();
				}

				// go back
				bytes.seek(savedPosition);
				this.value = (StringHelper.NewString(buff, System.Text.Encoding.Unicode)).Trim();
			}
			else
			{
				//
				// High bit is 0
				//

				// if it's NOT a STRING, then we do additional lookups.

				// determine what "name" means
				switch (level)
				{
					case 1: // TYPE
						this.value = ResourceTypes.get(intValue).DetailedInfo;
						break;
					case 2: // NAME
						this.value = intValue.toHexString();
						break;
					case 3: // Language ID
						this.value = intValue.toHexString();
						break;
					default:
						this.value = intValue.toHexString();
						break;
				}
			}
		}

		public override sealed string get()
		{
			return this.value;
		}

		public override void format(StringBuilder b)
		{
			b.Append(DescriptiveName).Append(": ");
			switch (this.level)
			{
				case 1: // TYPE
					break;
				case 2: // NAME
					b.Append("name: ");
					break;
				case 3: // Language ID
					b.Append("Language: ");
					break;
				default:
					b.Append("??: ");
					break;
			}


			b.Append(this.value).Append(System.Environment.NewLine);
		}
	}

}