using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IDeploymentScheduleJobService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggedInUser"></param>
        /// <param name="deploymentScheduleId"></param>
        /// <returns></returns>
        Task TriggerJobAsync(UserDto loggedInUser, Guid subscriptionId, long deploymentScheduleId, (int Id, bool IsPrivate)? application = null);
    }
}
