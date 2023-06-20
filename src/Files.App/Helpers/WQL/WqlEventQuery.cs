// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.MMI
{
	public class WqlEventQuery
	{
		public string QueryExpression { get; }

		public WqlEventQuery(string queryExpression)
		{
			QueryExpression = queryExpression;
		}
	}
}
