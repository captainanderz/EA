using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Entities
{
    public class DeploymentSchedulePrivateApplication : DeploymentScheduleApplication
    {
        public virtual PrivateApplication Application { get; set; }
    }
}
