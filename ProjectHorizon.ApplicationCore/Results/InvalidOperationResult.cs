using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    public class InvalidOperationResult : ErrorResult
    {
        public InvalidOperationResult(string message) : base(message)
        {
        }
    }

    public class InvalidOperationResult<T> : ErrorResult<T>
    {
        public InvalidOperationResult(string message) : base(message)
        {
        }
    }
}
