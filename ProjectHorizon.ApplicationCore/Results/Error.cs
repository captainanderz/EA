using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    public class Error
    {
        public Error(string details) : this(null, details)
        {

        }

        public Error(string code, string details)
        {
            Code = code;
            Details = details;
        }

        public string? Code { get; }
        public string Details { get; }
    }
}
