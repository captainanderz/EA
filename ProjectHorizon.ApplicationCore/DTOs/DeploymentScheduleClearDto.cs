namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class DeploymentScheduleClearDto
    {
        public int[] ApplicationIds { get; set; }

        public bool ShouldRemovePatchApp { get; set; }
    }
}
