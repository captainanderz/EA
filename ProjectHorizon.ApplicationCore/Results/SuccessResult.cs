using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    public class SuccessResult : Result
    {
        public SuccessResult()
        {
            Success = true;
        }
    }
}
