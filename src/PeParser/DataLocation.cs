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
	public class DataLocation
	{

		private readonly int location;
		private readonly int size;

		public DataLocation(int location, int size)
		{
			this.location = location;
			this.size = size;
		}

		public virtual int Location
		{
			get
			{
				return this.location;
			}
		}

		public virtual int Size
		{
			get
			{
				return this.size;
			}
		}
	}

}