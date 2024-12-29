// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

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
