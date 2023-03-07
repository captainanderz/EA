using System;

namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ShopAddDto
    {
        public int[] ApplicationIds { get; set; } = Array.Empty<int>();
        public bool ShouldCreateAssignmentProfile { get; set; }
        public string GroupName { get; set; }
    }
}
