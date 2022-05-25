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

	using UShort = dorkbox.bytes.UShort;


	public class WORD : ByteDefinition<UShort>
	{

		private readonly UShort value;

		public WORD(UShort value, string descriptiveName) : base(descriptiveName)
		{

			this.value = value;
		}

		public override sealed UShort get()
		{
			return this.value;
		}

		public override sealed void format(StringBuilder b)
		{
			b.Append(DescriptiveName).Append(": ").Append(this.value).Append(" (0x").Append(this.value.toHexString()).Append(")").Append(System.Environment.NewLine);
		}
	}

}