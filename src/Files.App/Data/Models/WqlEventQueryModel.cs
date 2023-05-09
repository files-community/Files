// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Models
{
	public class WqlEventQueryModel
	{
		public string QueryExpression { get; }

		public WqlEventQueryModel(string queryExpression)
		{
			QueryExpression = queryExpression;
		}
	}
}
