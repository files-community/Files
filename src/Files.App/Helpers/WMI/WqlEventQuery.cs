// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public sealed class WqlEventQuery
	{
		public string QueryExpression { get; }

		public WqlEventQuery(string queryExpression)
		{
			QueryExpression = queryExpression;
		}
	}
}
