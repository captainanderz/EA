using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Utility
{
    public class LazilyResolved<T> : Lazy<T> 
        where T : notnull
    {
        public LazilyResolved(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<T>)
        {
        }
    }
}
