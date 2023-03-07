using System;

namespace ProjectHorizon.ApplicationCore.Constants
{
    public class UserRole : BaseEnum<UserRole>
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Administrator = "Administrator";
        public const string Contributor = "Contributor";
        public const string Reader = "Reader";

        public static void ValidateUserRole(string userRole)
        {
            if (string.IsNullOrEmpty(userRole))
            {
                throw new ArgumentException("User role is missing");
            }

            if (!Values.Contains(userRole) || userRole == SuperAdmin)
            {
                throw new ArgumentException($"Invalid user role");
            }
        }
    }
}