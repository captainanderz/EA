using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentScheduleAssignmentDto
    {
        public int[] ApplicationIds { get; set; }

        public bool AutoUpdate { get; set; }
    }
}
