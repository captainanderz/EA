using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class BulkChangeUsersRoleDto
    {
        [Required]
        public IEnumerable<string> UserIds { get; set; }

        [Required]
        public string NewUserRole { get; set; }
    }
}
