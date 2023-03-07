import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from './guards/auth-guard.service';
import { ConfirmEmailComponent } from './pages/auth-pages/confirm-email/confirm-email.component';
import { CreateSubscriptionComponent } from './pages/create-subscription/create-subscription.component';
import { IntegrationsComponent } from './pages/integrations/integrations.component';
import { LoginComponent } from './pages/auth-pages/login/login.component';
import { PublicRepositoryComponent } from './pages/repositories/public-repository/public-repository.component';
import { SignUpComponent } from './pages/auth-pages/sign-up/sign-up.component';
import { ApprovalsComponent } from './pages/approvals/approvals.component';
import { AppRoutes } from './constants/app-routes';
import { NotificationsComponent } from './pages/notifications/notifications.component';
import { NotificationSettingsComponent } from './pages/notification-settings/notification-settings.component';
import { UserManagementComponent } from './pages/user-management/user-management.component';
import { UserSettingsComponent } from './pages/user-settings/user-settings.component';
import { ForgotPasswordComponent } from './pages/auth-pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './pages/auth-pages/reset-password/reset-password.component';
import { ConfirmChangeEmailComponent } from './pages/auth-pages/confirm-change-email/confirm-change-email.component';
import { RegisterInvitationComponent } from './pages/register-invitation/register-invitation.component';
import { PrivateRepositoryComponent } from './pages/repositories/private-repository/private-repository.component';
import { SettingsRecoveryCodesComponent } from './pages/mfa-pages/settings-recovery-codes/settings-recovery-codes.component';
import { SettingsSetupAuthenticatorComponent } from './pages/mfa-pages/settings-setup-authenticator/settings-setup-authenticator.component';
import { SignUpSetupAuthenticatorComponent } from './pages/mfa-pages/sign-up-setup-authenticator/sign-up-setup-authenticator.component';
import { LoginMfaComponent } from './pages/auth-pages/login-mfa/login-mfa.component';
import { LoginRecoveryCodeComponent } from './pages/auth-pages/login-recovery-code/login-recovery-code.component';
import { UserRole } from './constants/user-role';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';
import { SignUpAzureComponent } from './pages/auth-pages/sign-up-azure/sign-up-azure.component';
import { SingleSignOnComponent } from './pages/sso/single-sign-on/single-sign-on.component';
import { PaymentSetupComponent } from './pages/auth-pages/payment-setup/payment-setup/payment-setup.component';
import { SubscriptionsComponent } from './pages/subscriptions/subscriptions.component';
import { SuperAdminUsersComponent } from './pages/superadmin-users/superadmin-users.component';
import { SubscriptionComponent } from './pages/subscription/subscription.component';
import { AssignmentProfilesComponent } from './pages/assignment-profiles/assignment-profiles.component';
import { AssignmentProfileDetailsComponent } from './pages/assignment-profiles-details/assignment-profiles-details.component';
import { TermsAndConditionsComponent } from './pages/terms-and-conditions/terms-and-conditions.component';
import { TermsGuard } from './guards/terms.guard';
import { ShopRequestsComponent } from './pages/shop-requests/shop-requests.component';
import { NotLoggedInGuard } from './guards/not-logged-in.guard';
import { DeploymentSchedulesComponent } from './pages/deployment-schedules/deployment-schedules.component';
import { DeploymentScheduleDetailsComponent } from './pages/deployment-schedule-details/deployment-schedule-details.component';
import { HangfireComponent } from './pages/hangfire/hangfire.component';

const routes: Routes = [
  {
    path: AppRoutes.root,
    component: PublicRepositoryComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.assignmentProfiles + '/' + AppRoutes.new,
    component: AssignmentProfileDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.assignmentProfiles + '/' + AppRoutes.edit + '/:id',
    component: AssignmentProfileDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.assignmentProfiles + '/' + AppRoutes.details + '/:id',
    component: AssignmentProfileDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.terms,
    component: TermsAndConditionsComponent,
    canActivate: [AuthGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.privateRepository,
    component: PrivateRepositoryComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.assignmentProfiles,
    component: AssignmentProfilesComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.deploymentSchedules,
    component: DeploymentSchedulesComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.deploymentSchedules + '/' + AppRoutes.details + '/:id',
    component: DeploymentScheduleDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.deploymentSchedules + '/' + AppRoutes.edit + '/:id',
    component: DeploymentScheduleDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.deploymentSchedules + '/' + AppRoutes.new,
    component: DeploymentScheduleDetailsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.hangfireDashboard,
    component: HangfireComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.login,
    component: LoginComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.loginMfa,
    component: LoginMfaComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.loginRecoveryCode,
    component: LoginRecoveryCodeComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.signUp,
    component: SignUpComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.signUpAzure,
    component: SignUpAzureComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.registerInvitation,
    component: RegisterInvitationComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.paymentSetup,
    component: PaymentSetupComponent,
  },
  {
    path: AppRoutes.confirmEmail,
    component: ConfirmEmailComponent,
  },
  {
    path: AppRoutes.confirmChangeEmail,
    component: ConfirmChangeEmailComponent,
  },
  {
    path: AppRoutes.forgotPassword,
    component: ForgotPasswordComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.resetPassword,
    component: ResetPasswordComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.createSubscription,
    component: CreateSubscriptionComponent,
    canActivate: [NotLoggedInGuard],
  },
  {
    path: AppRoutes.approvals,
    component: ApprovalsComponent,
    canActivate: [AuthGuard, TermsGuard],
    runGuardsAndResolvers: 'always',
  },
  {
    path: AppRoutes.notifications,
    component: NotificationsComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.notificationSettings,
    component: NotificationSettingsComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.integrations,
    component: IntegrationsComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin, UserRole.Administrator],
    },
  },
  {
    path: AppRoutes.singleSignOn,
    component: SingleSignOnComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin, UserRole.Administrator],
    },
  },
  {
    path: AppRoutes.userManagement,
    component: UserManagementComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin, UserRole.Administrator],
    },
  },
  {
    path: AppRoutes.settingsSetupAuthenticator,
    component: SettingsSetupAuthenticatorComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.settingsRecoveryCodes,
    component: SettingsRecoveryCodesComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.signUpSetupAuthenticator,
    component: SignUpSetupAuthenticatorComponent,
  },
  {
    path: AppRoutes.userSettings,
    component: UserSettingsComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.auditLog,
    component: AuditLogComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.shopRequests,
    component: ShopRequestsComponent,
    canActivate: [AuthGuard, TermsGuard],
  },
  {
    path: AppRoutes.subscriptions,
    component: SubscriptionsComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin],
    },
  },
  {
    path: AppRoutes.subscription,
    component: SubscriptionComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin, UserRole.Administrator],
    },
  },
  {
    path: AppRoutes.superAdminUsers,
    component: SuperAdminUsersComponent,
    canActivate: [AuthGuard, TermsGuard],
    data: {
      allowedRoles: [UserRole.SuperAdmin],
    },
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
