using ProjectHorizon.ApplicationCore.DTOs;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IGraphAssignmentService
    {
        Task SetMobileAppAssignmentAsync(AssignmentProfileApplicationDto application, AssignmentProfileGroupDto[] assignmentProfileGroupDtos);
    }
}
