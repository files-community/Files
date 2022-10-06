namespace Files.FullTrust.MMI
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
