import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { AngularPlugin } from '@microsoft/applicationinsights-angularplugin-js';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { filter } from 'rxjs/operators';
import { AppRoutes } from './constants/app-routes';
import { SignalRConstants } from './constants/signalr-constants';
import { AppSettingsService } from './services/app-settings.service';
import { ConfigurationService } from './services/configuration.service';
import { LoaderService } from './services/loader.service';
import { SignalRService } from './services/signal-r.service';
import { UserService } from './services/user.service';
import { UserStore } from './services/user.store';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit, OnDestroy {
  overlayVisible = false;
  templateRefExp: TemplateRef<any>;

  @ViewChild('loggedIn') loggedInTemplate: TemplateRef<any>;
  @ViewChild('notLoggedIn') notLoggedInTemplate: TemplateRef<any>;
  @ViewChild('notLoggedInCustom') notLoggedInCustomTemplate: TemplateRef<any>;

  appRoutes = AppRoutes;
  private readonly notLoggedInPages = [
    AppRoutes.login,
    AppRoutes.loginMfa,
    AppRoutes.loginRecoveryCode,
    AppRoutes.paymentSetup,
    AppRoutes.confirmEmail,
    AppRoutes.confirmChangeEmail,
    AppRoutes.forgotPassword,
    AppRoutes.resetPassword,
    AppRoutes.registerInvitation,
  ];

  private readonly notLoggedInCustomPages = [
    AppRoutes.signUpAzure,
    AppRoutes.signUpSetupAuthenticator,
    AppRoutes.signUp,
    AppRoutes.createSubscription,
  ];

  constructor(
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly loaderService: LoaderService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly configurationService: ConfigurationService,
    private readonly signalRService: SignalRService,
    private readonly userStore: UserStore,
    private readonly userService: UserService
  ) // public readonly appSettingsService: AppSettingsService -> for customizable options like snow effect on Christmas etc
  {}

  ngOnInit() {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        const firstSegment = this.activatedRoute.firstChild?.snapshot.url ?? '';

        const match = (boundData: string[]) =>
          boundData.some((p) => p == firstSegment.toString());

        this.templateRefExp = match(this.notLoggedInPages)
          ? this.notLoggedInTemplate
          : match(this.notLoggedInCustomPages)
          ? this.notLoggedInCustomTemplate
          : this.loggedInTemplate;
      });

    if (this.signalRService.connection)
      this.signalRService.connection.off(SignalRConstants.UserUpdate);

    this.signalRService.connection.on(SignalRConstants.UserUpdate, (_) =>
      this.userService.get().subscribe((u) => {
        if (u.subscriptions.length <= 0) {
          this.userStore.logout();
        }

        this.userStore.setLoggedInUser(u, true);
      })
    );

    this.loaderService.httpProgress().subscribe((isLoadingInProgress) => {
      this.overlayVisible = isLoadingInProgress;
      this.changeDetectorRef.detectChanges();
    });

    this.configurationService
      .getApplicationInsightsConnectionString()
      .subscribe((appInsightsConnString) => {
        if (appInsightsConnString) {
          var angularPlugin = new AngularPlugin();

          const appInsights = new ApplicationInsights({
            config: {
              connectionString: appInsightsConnString,
              extensions: [angularPlugin],
              extensionConfig: {
                [angularPlugin.identifier]: { router: this.router },
              },
            },
          });

          appInsights.loadAppInsights();
        }
      });

    this.signalRService.connection.start();
  }

  ngOnDestroy() {
    this.signalRService.connection.stop();
  }
}
