namespace Files.App.Views
{
    public class LayoutModeArguments
    {
        public bool FocusOnNavigation { get; set; }
        public bool IsSearchResultPage { get; set; }
        public string? SearchPathParam { get; set; }
        public string? SearchQuery { get; set; }
        public bool SearchUnindexedItems { get; set; }

        public LayoutModeArguments(bool isSearchResult = false, bool focusOnNavigation = true)
        {
            this.IsSearchResultPage = isSearchResult;
            this.FocusOnNavigation = focusOnNavigation;
        }
    }
}
