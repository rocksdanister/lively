using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Models.Gallery.API
{
    public class ProfileDto
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
    }
}
