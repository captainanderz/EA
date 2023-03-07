/**
 * This file contains authentication parameters. Contents of this file
 * is roughly the same across other MSAL.js libraries. These parameters
 * are used to initialize Angular and MSAL Angular configurations in
 * in app.module.ts file.
 */

import {
  LogLevel,
  Configuration,
  BrowserCacheLocation,
} from '@azure/msal-browser';
import { AzureScopes } from './constants/azure-scopes';

const isIE =
  window.navigator.userAgent.indexOf('MSIE ') > -1 ||
  window.navigator.userAgent.indexOf('Trident/') > -1;

/**
 * Configuration object to be passed to MSAL instance on creation.
 * For a full list of MSAL.js configuration parameters, visit:
 * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/configuration.md
 */
export const msalConfig: Configuration = {
  auth: {
    clientId: '9ea1b1a3-2683-4aa2-816e-d2061be79bde', // This is the ONLY mandatory field that you need to supply.
    authority: 'https://login.microsoftonline.com/organizations', // Defaults to "https://login.microsoftonline.com/common"
    redirectUri: '/subscription-check', // Points to window.location.origin. You must register this URI on Azure portal/App Registration.
    postLogoutRedirectUri: '/login', // Points to window.location.origin. You must register this URI on Azure portal/App Registration.
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage, // Configures cache location. "sessionStorage" is more secure, but "localStorage" gives you SSO between tabs.
    storeAuthStateInCookie: isIE, // Set this to "true" if you are having issues on IE11 or Edge
  },
  system: {
    loggerOptions: {
      loggerCallback(logLevel: LogLevel, message: string) {
        console.log(message);
      },
      logLevel: LogLevel.Warning,
      piiLoggingEnabled: false,
    },
  },
};

/**
 * Scopes you add here will be prompted for user consent during sign-in.
 * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
 * For more information about OIDC scopes, visit:
 * https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#openid-connect-scopes
 */
export const loginRequest = {
  scopes: [AzureScopes.ApiScope], // If you would like the admin-user to explicitly consent via "Admin" page, instead of being prompted for admin consent during initial login, remove this scope.
};
