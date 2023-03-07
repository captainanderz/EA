using System;
using System.Security.Cryptography;

namespace ProjectHorizon.ApplicationCore.Utility
{
    public static class TokenHelper
    {
        public static string GetRandomTokenString()
        {
            using RandomNumberGenerator? rng = RandomNumberGenerator.Create();
            byte[]? randomBytes = new byte[40];
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }
    }
}