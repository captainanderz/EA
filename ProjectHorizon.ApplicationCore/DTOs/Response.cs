namespace ProjectHorizon.ApplicationCore.DTOs
{
    public class Response<T>
    {
        public bool IsSuccessful { get; set; }

        public string ErrorMessage { get; set; }

        public T Dto { get; set; }
    }
}
