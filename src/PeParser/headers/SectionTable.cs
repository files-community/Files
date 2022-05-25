using System.Collections.Generic;

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
namespace dorkbox.peParser.headers
{

	using ByteArray = dorkbox.peParser.ByteArray;

	public class SectionTable : Header
	{

		// more info here: http://msdn.microsoft.com/en-us/library/ms809762.aspx

		public IList<SectionTableEntry> sections;

		public SectionTable(ByteArray bytes, int numberOfEntries)
		{

			this.sections = new List<SectionTableEntry>(numberOfEntries);

			bytes.mark();
			for (int i = 0;i < numberOfEntries;i++)
			{
				int offset = i * SectionTableEntry.ENTRY_SIZE; // 40 bytes per table entry, no spacing between them
				bytes.skip(offset);
				SectionTableEntry sectionTableEntry = new SectionTableEntry(bytes, i + 1, offset, SectionTableEntry.ENTRY_SIZE);
				this.sections.Add(sectionTableEntry);
				bytes.reset();
			}
		}
	}

}