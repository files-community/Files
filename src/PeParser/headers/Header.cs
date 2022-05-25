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

	using dorkbox.peParser.types;

	public class Header
	{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in C#:
//ORIGINAL LINE: public java.util.List<dorkbox.peParser.types.ByteDefinition<?>> headers = new java.util.ArrayList<dorkbox.peParser.types.ByteDefinition<?>>(0);
		public IList<object> headers = new List<object>(0);

		public Header()
		{
		}

		protected internal virtual T h<T>(T @object)
		{
			this.headers.Add(@object);
			return @object;
		}
	}

}