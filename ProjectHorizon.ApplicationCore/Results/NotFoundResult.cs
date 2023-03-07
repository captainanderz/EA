using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    public class NotFoundResult : ErrorResult
    {
        public NotFoundResult(string message) : base(message)
        {
        }
    }

    public class NotFoundResult<T> : ErrorResult<T>
    {
        public NotFoundResult(string message) : base(message)
        {
        }
    }
}
