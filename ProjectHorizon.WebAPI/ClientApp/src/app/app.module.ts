import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { HeaderComponent } from './components/header/header.component';
import { PublicRepositoryComponent } from './pages/repositories/public-repository/public-repository.component';
import { LoaderInterceptor } from './http-interceptors/loader-interceptor';
import { SignUpComponent } from './pages/auth-pages/sign-up/sign-up.component';
import { ConfirmEmailComponent } from './pages/auth-pages/confirm-email/confirm-email.component';
import { CreateSubscriptionComponent } from './pages/create-subscription/create-subscription.component';
import { IntegrationsComponent } from './pages/integrations/integrations.component';
import { LoginComponent } from './pages/auth-pages/login/login.component';
import { JwtModule } from '@auth0/angular-jwt';
import { AuthGuard } from './guards/auth-guard.service';
import { ApprovalsComponent } from './pages/approvals/approvals.component';
import { UserStoreKeys } from './constants/user-store-keys';
import { NotificationsComponent } from './pages/notifications/notifications.component';
import { NotificationSettingsComponent } from './pages/notification-settings/notification-settings.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { InfoModalComponent } from './components/modals/info-modal/info-modal.component';
import { ConfirmationModalComponent } from './components/modals/confirmation-modal/confirmation-modal.component';
import { ChangeSubscriptionModalComponent } from './components/modals/change-subscription-modal/change-subscription-modal.component';
import { ApplicationUploadModalComponent } from './components/modals/application-upload-modal/application-upload-modal.component';
import { UserManagementComponent } from './pages/user-management/user-management.component';
import { ChangeUserRoleModalComponent } from './components/modals/change-user-role-modal/change-user-role-modal.component';
import { UserSettingsComponent } from './pages/user-settings/user-settings.component';
import { ForgotPasswordComponent } from './pages/auth-pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './pages/auth-pages/reset-password/reset-password.component';
import { ConfirmChangeEmailComponent } from './pages/auth-pages/confirm-change-email/confirm-change-email.component';
import { InviteUserModalComponent } from './components/modals/invite-user-modal/invite-user-modal.component';
import { RegisterInvitationComponent } from './pages/register-invitation/register-invitation.component';
import { PrivateRepositoryComponent } from './pages/repositories/private-repository/private-repository.component';
import { PublicApplicationUploadModalComponent } from './components/modals/public-application-upload-modal/public-application-upload-modal.component';
import { PrivateApplicationUploadModalComponent } from './components/modals/private-application-upload-modal/private-application-upload-modal.component';
import { SetupAuthenticatorComponent } from './pages/mfa-pages/components/setup-authenticator/setup-authenticator.component';
import { QRCodeModule } from 'angularx-qrcode';
import { RecoveryCodesComponent } from './pages/mfa-pages/components/recovery-codes/recovery-codes.component';
import { SettingsRecoveryCodesComponent } from './pages/mfa-pages/settings-recovery-codes/settings-recovery-codes.component';
import { SettingsSetupAuthenticatorComponent } from './pages/mfa-pages/settings-setup-authenticator/settings-setup-authenticator.component';
import { SignUpSetupAuthenticatorComponent } from './pages/mfa-pages/sign-up-setup-authenticator/sign-up-setup-authenticator.component';
import { LoginMfaComponent } from './pages/auth-pages/login-mfa/login-mfa.component';
import { MultipleSelectDirective } from './pages/multiple-select.directive';
import { LoginRecoveryCodeComponent } from './pages/auth-pages/login-recovery-code/login-recovery-code.component';
import { RepositoryDirective } from './pages/repositories/repository/repository.directive';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';
import { loginRequest, msalConfig } from './auth-azure-config';
import {
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalModule,
  MsalRedirectComponent,
  MsalService,
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
} from '@azure/msal-angular';
import {
  InteractionType,
  IPublicClientApplication,
  PublicClientApplication,
} from '@azure/msal-browser';
import { SignUpAzureComponent } from './pages/auth-pages/sign-up-azure/sign-up-azure.component';
import { HttpParametersInterceptor } from './http-interceptors/http-parameters-interceptor';
import { SingleSignOnComponent } from './pages/sso/single-sign-on/single-sign-on.component';
import { ErrorInterceptor } from './http-interceptors/error-interceptor';
import { TrialInfoComponent } from './pages/auth-pages/trial-info/trial-info.component';
import { PaymentSetupComponent } from './pages/auth-pages/payment-setup/payment-setup/payment-setup.component';
import { SubscriptionsComponent } from './pages/subscriptions/subscriptions.component';
import { SubscriptionComponent } from './pages/subscription/subscription.component';
import { ChangeBillingInfoModalComponent } from './components/modals/change-billing-info-modal/change-billing-info-modal.component';
import { CommonModule } from '@angular/common';
import { SuperAdminUsersComponent } from './pages/superadmin-users/superadmin-users.component';
import { AssignmentProfilesComponent } from './pages/assignment-profiles/assignment-profiles.component';
import { AssignmentProfileDetailsComponent } from './pages/assignment-profiles-details/assignment-profiles-details.component';
import { SelectGroupsModalComponent } from './components/modals/select-groups-modal/select-groups-modal.component';
import { AssignProfileModalComponent } from './components/modals/assign-profile-modal/assign-profile-modal.component';
import { TermsAndConditionsModalComponent } from './components/modals/terms-and-conditions-modal/terms-and-conditions-modal.component';
import { TermsAndConditionsComponent } from './pages/terms-and-conditions/terms-and-conditions.component';
import { ShopRequestsComponent } from './pages/shop-requests/shop-requests.component';
import { ShopAddConfirmationModalComponent } from './components/modals/shop-add-confirmation-modal/shop-add-confirmation-modal.component';
import { ShopRemoveConfirmationModalComponent } from './components/modals/shop-remove-confirmation-modal/shop-remove-confirmation-modal.component';
import { NotLoggedInGuard } from './guards/not-logged-in.guard';
import { UnicodePatternValidator } from './validators/unicode-pattern-validator.directive';
import { BulkActionsComponent } from './components/bulk-actions/bulk-actions.component';
import { SetDeploymentScheduleComponent } from './components/modals/set-deployment-schedule/set-deployment-schedule.component';
import { DeploymentSchedulesComponent } from './pages/deployment-schedules/deployment-schedules.component';
import { DeploymentScheduleDetailsComponent } from './pages/deployment-schedule-details/deployment-schedule-details.component';
import { CronSelectorModalComponent } from './components/modals/cron-selector-modal/cron-selector-modal.component';
import { ClearDeploymentScheduleModalComponent } from './components/modals/clear-deployment-schedule-modal/clear-deployment-schedule-modal.component';
import { InfiniteScrollModule } from 'ngx-infinite-scroll';
import { MaxValueValidator } from './validators/max-value-validator.directive';
import { MinValueValidator } from './validators/min-value-validator.directive';
import { FocusInvalidInputDirective } from './directives/focus-invalid-input.directive';
import { AutoFocusDirective } from './directives/auto-focus.directive';
import { HangfireComponent } from './pages/hangfire/hangfire.component';
import { SnowComponent } from './components/snow/snow.component';
import { ApplicationInformationService } from './services/application-information.service';

/**
 * Here we pass the configuration parameters to create an MSAL instance.
 * For more info, visit: https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-angular/docs/v2-docs/configuration.md
 */

export function MSALInstanceFactory(): IPublicClientApplication {
  const request = new XMLHttpRequest();
  request.open('GET', '/api/application-information', false); // request application settings synchronous
  request.send(null);
  const response = JSON.parse(request.responseText);
  msalConfig.auth.clientId = response.clientId;
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
@NgModule({
  declarations: [
    AppComponent,
    SidebarComponent,
    HeaderComponent,
    PublicRepositoryComponent,
    SignUpComponent,
    ConfirmEmailComponent,
    CreateSubscriptionComponent,
    IntegrationsComponent,
    LoginComponent,
    ApprovalsComponent,
    NotificationsComponent,
    NotificationSettingsComponent,
    InfoModalComponent,
    ConfirmationModalComponent,
    ChangeSubscriptionModalComponent,
    ApplicationUploadModalComponent,
    UserManagementComponent,
    ChangeUserRoleModalComponent,
    InviteUserModalComponent,
    RegisterInvitationComponent,
    UserSettingsComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    ConfirmChangeEmailComponent,
    PrivateRepositoryComponent,
    PublicApplicationUploadModalComponent,
    PrivateApplicationUploadModalComponent,
    SetupAuthenticatorComponent,
    RecoveryCodesComponent,
    SettingsRecoveryCodesComponent,
    SettingsSetupAuthenticatorComponent,
    SignUpSetupAuthenticatorComponent,
    LoginMfaComponent,
    LoginRecoveryCodeComponent,
    MultipleSelectDirective,
    RepositoryDirective,
    AuditLogComponent,
    SignUpAzureComponent,
    SingleSignOnComponent,
    TrialInfoComponent,
    PaymentSetupComponent,
    SubscriptionsComponent,
    SubscriptionComponent,
    ChangeBillingInfoModalComponent,
    SuperAdminUsersComponent,
    AssignmentProfilesComponent,
    AssignmentProfileDetailsComponent,
    SelectGroupsModalComponent,
    AssignProfileModalComponent,
    TermsAndConditionsModalComponent,
    TermsAndConditionsComponent,
    ShopRequestsComponent,
    ShopAddConfirmationModalComponent,
    ShopRemoveConfirmationModalComponent,
    UnicodePatternValidator,
    BulkActionsComponent,
    DeploymentSchedulesComponent,
    DeploymentScheduleDetailsComponent,
    CronSelectorModalComponent,
    SetDeploymentScheduleComponent,
    ClearDeploymentScheduleModalComponent,
    MaxValueValidator,
    MinValueValidator,
    FocusInvalidInputDirective,
    AutoFocusDirective,
    HangfireComponent,
    SnowComponent,
  ],
  imports: [
    CommonModule,
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    JwtModule.forRoot({
      config: {
        skipWhenExpired: true,
        tokenGetter: () => localStorage.getItem(UserStoreKeys.accessToken),
      },
    }),
    NgbModule,
    QRCodeModule,
    MsalModule,
    InfiniteScrollModule,
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoaderInterceptor,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpParametersInterceptor,
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
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    AuthGuard,
    NotLoggedInGuard,
  ],
  bootstrap: [AppComponent, MsalRedirectComponent],
})
export class AppModule {}
