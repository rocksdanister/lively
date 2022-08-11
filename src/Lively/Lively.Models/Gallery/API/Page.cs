using System.Collections.Generic;

namespace Lively.Models.Gallery.API
{
    public class Page<T>
    {
        public int Number { get; set; }
        public bool NextPageAvailable { get; set; }
        public List<T> Data { get; set; }
    }
}