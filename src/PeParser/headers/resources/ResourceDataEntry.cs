using System;

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
namespace dorkbox.peParser.headers.resources
{
	using ByteArray = dorkbox.peParser.ByteArray;
	using Header = dorkbox.peParser.headers.Header;
	using SectionTableEntry = dorkbox.peParser.headers.SectionTableEntry;
	using DWORD = dorkbox.peParser.types.DWORD;

	public class ResourceDataEntry : Header
	{

		public readonly DWORD OFFSET_TO_DATA; // The address of a unit of resource data in the Resource Data area.
		public readonly DWORD SIZE;
		public readonly DWORD CODE_PAGE;
		public readonly DWORD RESERVED;

		private readonly SectionTableEntry section;

		/// <param name="section"> - necessary to know this section for when computing the location of the resource data! </param>
		public ResourceDataEntry(ByteArray bytes, SectionTableEntry section)
		{
			this.section = section;

			this.OFFSET_TO_DATA = new DWORD(bytes.readUInt(4), "offsetToData");
			this.SIZE = new DWORD(bytes.readUInt(4), "Size");
			this.CODE_PAGE = new DWORD(bytes.readUInt(4), "CodePage");
			this.RESERVED = new DWORD(bytes.readUInt(4), "Reserved");
		}

		public virtual sbyte[] getData(ByteArray bytes)
		{
			// this is where to get the data from the ABSOLUTE position in the file!
			long dataOffset = this.section.POINTER_TO_RAW_DATA.get().longValue() + this.OFFSET_TO_DATA.get().longValue() - this.section.VIRTUAL_ADDRESS.get().longValue();

			if (dataOffset > int.MaxValue)
			{
				throw new Exception("Unable to set offset to more than 2gb!");
			}

			//String asHex = Integer.toHexString(dataOffset);
			int saved = bytes.position();
			bytes.seek((int) dataOffset);

			long bytesToCopyLong = this.SIZE.get().longValue();
			if (bytesToCopyLong > int.MaxValue)
			{
				throw new Exception("Unable to copy more than 2gb of bytes!");
			}

			sbyte[] copyBytes = bytes.copyBytes((int)bytesToCopyLong);
			bytes.seek(saved);
			return copyBytes;
		}
	}

}