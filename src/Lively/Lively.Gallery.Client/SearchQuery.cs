using Lively.Models.Gallery.API;

namespace Lively.Gallery.Client
{
    public class SearchQuery
    {
        internal SearchQuery(List<string> tags, string name, int page, int limit, SortingType sortingType)
        {
            Tags = tags;
            Name = name;
            Page = page;
            Limit = limit;
            SortingType = sortingType;
        }

        public int Limit { get; }
        public SortingType SortingType { get; }
        public List<string> Tags { get; }
        public string Name { get; }
        public int Page { get; }
    }
}