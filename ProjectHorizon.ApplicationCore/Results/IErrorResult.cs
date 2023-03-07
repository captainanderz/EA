using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    internal interface IErrorResult
    {
        string Message { get; }
        IReadOnlyCollection<Error> Errors { get; }
    }
}
