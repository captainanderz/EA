using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentScheduleRemoveDto
    {
        public long[] Ids { get; set; }

        public bool ShouldRemovePatchApp { get; set; }
    }
}
