namespace ProjectHorizon.ApplicationCore.Constants
{
    public class Patterns
    {
        public const string PersonName = @"^[\p{L},.' -]+$"; // \p{L} is any letter from any language
        public const string CompanyName = @".*\S.*";
    }
}
