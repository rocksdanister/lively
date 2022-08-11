using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Gallery.Client
{
    public class ApiException : Exception
    {
        public ApiException()
        {
        }
        public ApiException(List<string> errors)
        {
            Errors = errors;
        }

        public ApiException(string? message) : base(message)
        {
        }

        public ApiException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public List<string> Errors { get; }

        public override string ToString()
        {
            return string.Join("\n", Errors);
        }
    }
}
