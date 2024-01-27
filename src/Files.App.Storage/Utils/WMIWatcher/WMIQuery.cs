// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage
{
	public class WMIQuery
	{
		public string QueryExpression { get; }

		public WMIQuery(string queryExpression)
		{
			QueryExpression = queryExpression;
		}
	}
}
