namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class ShopRemoveDto
    {
        public int[] ApplicationIds { get; set; }

        public bool ShouldDeleteGroup { get; set; }

        public bool ShouldDeleteAssignmentProfile { get; set; }
    }
}
