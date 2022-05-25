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
	using Characteristics = dorkbox.peParser.headers.flags.Characteristics;

	public class CoffCharacteristics : ByteDefinition<Characteristics[]>
	{

		private readonly UShort value;

		public CoffCharacteristics(UShort value, string descriptiveName) : base(descriptiveName)
		{
			this.value = value;
		}

		public override sealed Characteristics[] get()
		{
			return Characteristics.get(this.value);
		}

		public override void format(StringBuilder b)
		{
			Characteristics[] characteristics = get();


			b.Append(DescriptiveName).Append(":").Append(System.Environment.NewLine);

			if (characteristics.Length > 0)
			{
				foreach (Characteristics c in characteristics)
				{
					b.Append("\t * ").Append(c.Description).Append(System.Environment.NewLine);
				}
			}
			else
			{
				b.Append("\t * none").Append(System.Environment.NewLine);
			}
			b.Append(System.Environment.NewLine);
		}
	}

}