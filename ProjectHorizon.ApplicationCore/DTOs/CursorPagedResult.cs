namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class CursorPagedResult<T> : PagedResult<T>
    {
        public string? NextPageLink { get; set; }
    }
}
