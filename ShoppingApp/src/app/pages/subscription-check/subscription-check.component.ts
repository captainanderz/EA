import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import {
  EventMessage,
  EventType,
  AuthenticationResult,
  InteractionStatus,
} from '@azure/msal-browser';
import { tap, filter, takeUntil, Subject, of, catchError, delay } from 'rxjs';
import { AppRoutes } from 'src/app/constants/app-routes';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-subscription-check',
  templateUrl: './subscription-check.component.html',
  styleUrls: ['./subscription-check.component.scss'],
})
export class SubscriptionCheckComponent implements OnInit {
  // Fields
  private readonly destroying$ = new Subject<void>();
  public hasSubscription: boolean | undefined = undefined;

  // Constructor
  constructor(
    private router: Router,
    private msalAuthService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private readonly authService: AuthService
  ) {}

  // Methods
  ngOnInit(): void {
    this.msalBroadcastService.inProgress$
      .pipe(
        tap((status: InteractionStatus) => console.log(status)),
        filter(
          (status: InteractionStatus) => status === InteractionStatus.Startup
        ),
        catchError((_) => {
          return of(null);
        }),
        // wait 1s for interaction to finish if there's any going on
        // because there's no way to check if there is no interaction going on
        delay(1000)
      )
      .subscribe((result: InteractionStatus | null) => {
        if (!result) {
          this.hasSubscription = false;
          return;
        }

        const accounts = this.msalAuthService.instance.getAllAccounts();

        if (accounts.length <= 0) {
          this.hasSubscription = false;
          return;
        }

        const account = accounts[0];
        this.msalAuthService.instance.setActiveAccount(account);
        this.checkHasSubscription();
      });

    this.msalBroadcastService.msalSubject$
      .pipe(
        filter(
          (msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS
        ),
        takeUntil(this.destroying$),
        catchError((_) => {
          return of(null);
        })
      )
      .subscribe((result: EventMessage | null) => {
        if (!result) {
          this.hasSubscription = false;
          return;
        }

        const payload = result.payload as AuthenticationResult;
        this.msalAuthService.instance.setActiveAccount(payload.account);
        this.checkHasSubscription();
      });
  }

  // When the user logs in, he is first redirected to a waiting page where the application will check if the user is part of any subscription
  // If it is, then he will be redirected to the applications page of that subscription, if it isn't part of any subscription or an error occured
  // When trying to log in, the user will be redirected to a page with a message explaining the situation and the logout button
  checkHasSubscription() {
    this.hasSubscription = undefined;

    this.authService
      .login()
      .pipe(
        catchError((_) => {
          return of(null);
        })
      )
      .subscribe((user) => {
        if (!user) {
          this.hasSubscription = false;
          return;
        }

        this.hasSubscription = true;
        this.router.navigate([AppRoutes.applications]);
      });
  }

  ngOnDestroy(): void {
    this.destroying$.next();
    this.destroying$.complete();
  }

  // Logs out the user
  public logout(): void {
    this.authService.logout();
  }
}
