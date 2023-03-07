import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { NavbarComponent } from './components/navbar/navbar.component';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApplicationViewComponent } from './pages/applications/application-view/application-view.component';
import { FormsModule } from '@angular/forms';
import { LoginComponent } from './pages/login/login.component';
import { loginRequest, msalConfig } from './auth-azure-config';
import {
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalModule,
  MsalRedirectComponent,
  MsalService,
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
} from '@azure/msal-angular';
import {
  InteractionType,
  IPublicClientApplication,
  PublicClientApplication,
} from '@azure/msal-browser';
import { ApplicationDetailsComponent } from './pages/application-details/application-details.component';
import { RequestsComponent } from './pages/requests/requests.component';
import { AuthGuard } from './guards/auth.guard';
import { SubscriptionCheckComponent } from './pages/subscription-check/subscription-check.component';
import { JwtModule } from '@auth0/angular-jwt';
import { UserStoreKeys } from './constants/user-store-keys';
import { ApiRoutes } from './constants/api-routes';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { ApplicationDetailsModalComponent } from './modals/application-details-modal/application-details-modal.component';
import { LoaderInterceptor } from './interceptors/loader.interceptor';

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication(msalConfig);
}

/**
 * Set your default interaction type for MSALGuard here. If you have any
 * additional scopes you want the user to consent upon login, add them here as well.
 */
export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: loginRequest,
  };
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap: new Map([
      [
        ApiRoutes.login,
        ['api://4bb8e2f4-188b-4d1d-a0d1-78ceac01f763/access_as_user'],
      ],
    ]),
  };
}

@NgModule({
  declarations: [
    AppComponent,
    ApplicationsComponent,
    NavbarComponent,
    ApplicationViewComponent,
    LoginComponent,
    ApplicationDetailsComponent,
    RequestsComponent,
    SubscriptionCheckComponent,
    ApplicationDetailsModalComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    NgbModule,
    HttpClientModule,
    FormsModule,
    MsalModule,
    JwtModule.forRoot({
      config: {
        skipWhenExpired: true,
        tokenGetter: (request) => {
          if (!request) {
            return null;
          }

          if (request.url.includes(ApiRoutes.login)) {
            return null;
          }

          return localStorage.getItem(UserStoreKeys.accessToken);
        },
      },
    }),
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoaderInterceptor,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true,
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory,
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true,
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    AuthGuard,
  ],
  bootstrap: [AppComponent, MsalRedirectComponent],
})
export class AppModule {}
