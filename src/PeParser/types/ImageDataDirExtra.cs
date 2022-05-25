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

	public class ImageDataDirExtra : ByteDefinition<UInteger>
	{

		private TInteger virtualAddress;
		private TInteger size;

		/// <summary>
		/// 8 bytes each </summary>
		public ImageDataDirExtra(ByteArray bytes, string description) : base(description)
		{

			this.virtualAddress = new TInteger(bytes.readUInt(4), "Virtual Address");
			this.size = new TInteger(bytes.readUInt(4), "Size");
		}

		public override UInteger get()
		{
			return this.virtualAddress.get();
		}

		public virtual UInteger Size
		{
			get
			{
				return this.size.get();
			}
		}

		public override void format(StringBuilder b)
		{
			b.Append(DescriptiveName).Append(": ").Append(System.Environment.NewLine).Append("\t").Append("address: ").Append(this.virtualAddress).Append(" (0x").Append(this.virtualAddress.get().toHexString()).Append(")").Append(System.Environment.NewLine).Append("\t").Append("size: ").Append(this.size.get()).Append(" (0x").Append(this.size.get().toHexString()).Append(")").Append(System.Environment.NewLine);
		}
	}

}