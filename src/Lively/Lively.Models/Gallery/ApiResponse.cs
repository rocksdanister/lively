using System.Collections.Generic;

namespace Lively.Models.Gallery
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }
        public int StatusCode { get; set; }
    }
}
