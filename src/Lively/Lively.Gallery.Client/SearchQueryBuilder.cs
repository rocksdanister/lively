using Lively.Models.Gallery.API;

namespace Lively.Gallery.Client
{
    public class SearchQueryBuilder
    {
        private string? _name;
        private List<string> _tags;
        private SortingType _sortingType;
        private int _page;
        private int _limit;


        /// <summary>
        /// Initializes new SearchQueryBuilder with default values <br/>
        /// SortingType = Trending  <br/>
        /// Tags = null  <br/>
        /// Page = 1  <br/>
        /// Limit = 25  <br/>
        /// Name = null  <br/>
        /// </summary>
        public SearchQueryBuilder()
        {
            _tags = new();
            _name = null;
            _page = 0;
            _limit = 25;
            _sortingType = SortingType.Trending;
        }

        public SearchQueryBuilder SortBy(SortingType sortBy)
        {
            _sortingType = sortBy;
            return this;
        }
        public SearchQueryBuilder WithTags(params string[] tag)
        {
            _tags.AddRange(tag);
            return this;
        }
        public SearchQueryBuilder SetPage(int page)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException("page", "limit should be more than or equals 0");
            _page = page;
            return this;
        }
        public SearchQueryBuilder SetLimit(int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException("limit", "limit should be more than 0");
            _limit = limit;
            return this;
        }
        /// <summary>
        /// Search by name
        /// </summary>
        public SearchQueryBuilder WithSearchQuery(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            _name = name;
            return this;
        }
        public SearchQuery Build()
        {
            var query = new SearchQuery(_tags.Count > 0 ? _tags : null, _name, _page, _limit, _sortingType);
            return query;
        }

    }
}