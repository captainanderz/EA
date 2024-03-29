﻿using Microsoft.Graph;

namespace ProjectHorizon.IntuneAppBuilder.Util
{
    internal static class RetryUtil
    {
        /// <summary>
        ///     Sets the retry delay time for request retries.
        /// </summary>
        public static T WithRetryDelay<T>(this T baseRequest, int delaySeconds) where T : IBaseRequest
        {
            string? key = typeof(RetryHandlerOption).ToString();
            if (baseRequest.MiddlewareOptions.TryGetValue(key, out IMiddlewareOption? option) && option is RetryHandlerOption rho)
            {
                rho.Delay = delaySeconds;
            }
            else
            {
                baseRequest.MiddlewareOptions.Add(key, new RetryHandlerOption
                {
                    Delay = delaySeconds
                });
            }

            return baseRequest;
        }
    }
}
