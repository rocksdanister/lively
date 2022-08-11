using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Models.Gallery.API
{
    public class ReportModel
    {     
        public ReportType Report { get; set; }
        public string Message { get; set; }

        public enum ReportType
        {
            Other,
            WrongGenre,
            NudityViolence,
            CopyrightViolation,
            Spam
        }
    }
}
