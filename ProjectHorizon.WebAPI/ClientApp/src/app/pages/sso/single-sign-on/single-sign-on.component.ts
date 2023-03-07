import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  MsalGuardConfiguration,
  MsalService,
  MSAL_GUARD_CONFIG,
} from '@azure/msal-angular';
import { InteractionType, RedirectRequest } from '@azure/msal-browser';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { msalConfig } from 'src/app/auth-azure-config';
import { AppRoutes } from 'src/app/constants/app-routes';
import { AzureScopes } from 'src/app/constants/azure-scopes';
import { UserRole } from 'src/app/constants/user-role';
import { SubscriptionConsentDto } from 'src/app/dtos/subscription-consent-dto.model';
import { AuditLogService } from 'src/app/services/audit-log.service';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-single-sign-on',
  templateUrl: './single-sign-on.component.html',
})
export class SingleSignOnComponent implements OnInit, OnDestroy {
  private readonly unsubscribe$ = new Subject<void>();
  adminConsentMessage = 'Consent granted!';
  adminConsentDetails = '';
  consentAttempted = false;
  consents: ReadonlyArray<SubscriptionConsentDto> = [];

  constructor(
    private readonly userStore: UserStore,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly msalAuthService: MsalService,
    private readonly auditLogService: AuditLogService,
    private readonly subscriptionService: SubscriptionService,
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration
  ) {}

  ngOnInit() {
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        if (
          loggedInUser?.userRole == UserRole.Contributor ||
          loggedInUser?.userRole == UserRole.Reader
        ) {
          this.router.navigate([AppRoutes.root]);
        }
      });

    this.subscriptionService.getConsents().subscribe((consents) => {
      this.consents = consents;
    });

    this.route.queryParams.subscribe((params) => {
      if (params.admin_consent && !params.error) {
        this.consentAttempted = true;
        this.auditLogService.adminConsentAudit().subscribe();

        this.subscriptionService
          .addConsent({ tenantId: params.tenant })
          .subscribe();
      } else if (params.error) {
        this.consentAttempted = true;
        this.adminConsentMessage = 'Consent not granted!';
        this.adminConsentDetails =
          'Only an administrator of the Azure AD tenant can grant permission to this application.';
      }
    });
  }

  adminConsent() {
    // if you want to work with multiple accounts, add your account selection logic below
    let account = this.msalAuthService.instance.getAllAccounts()[0];

    if (account) {
      const state = Math.floor(Math.random() * 90000) + 10000; // state parameter for anti token forgery

      /**
       * Construct URL for admin consent endpoint. For more info,
       * visit: https://docs.microsoft.com/azure/active-directory/develop/v2-admin-consent
       */
      const adminConsentUri =
        'https://login.microsoftonline.com/' +
        `${account.tenantId}` +
        '/v2.0/adminconsent?client_id=' +
        `${msalConfig.auth.clientId}` +
        '&state=' +
        `${state}` +
        '&redirect_uri=' +
        `${window.location.origin}/${AppRoutes.singleSignOn}` +
        '&scope=' +
        `${AzureScopes.ApiScope()}`;

      // redirecting...
      window.location.replace(adminConsentUri);
    } else {
      this.azureLogin();
    }
  }

  azureLogin() {
    const request = {
      scopes: ['user.read'],
      prompt: 'none',
    };

    this.msalAuthService.ssoSilent(request).subscribe(
      (_) => {},
      (_) => {
        this.msalAuthService.loginPopup(request).subscribe(
          (_) => {},
          (_) => {
            window.alert('You must be signed in with your Azure AD account');
          }
        );
      }
    );
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
