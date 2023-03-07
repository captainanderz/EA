import { HttpErrorResponse } from '@angular/common/http';
import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  MsalGuardConfiguration,
  MsalService,
  MSAL_GUARD_CONFIG,
} from '@azure/msal-angular';
import { InteractionType, RedirectRequest } from '@azure/msal-browser';
import { AppRoutes } from 'src/app/constants/app-routes';
import { SubscriptionState } from 'src/app/constants/subscription-state';
import { LoginDto } from 'src/app/dtos/login-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { UserStore } from 'src/app/services/user.store';
import { Response } from '../../../dtos/response.model';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  readonly appRoutes = AppRoutes;
  readonly loginDto = new LoginDto();

  shouldAcceptTerms: boolean;
  errorMessage: string;
  returnUrl: string;

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private route: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly userStore: UserStore,
    private readonly router: Router,
    private msalAuthService: MsalService
  ) {}

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  submit() {
    this.authService.login(this.loginDto).subscribe(
      (response: Response<UserDto>) => {
        if (!response.isSuccessful) this.errorMessage = response.errorMessage;
        else if (
          response.dto.subscriptionId == '00000000-0000-0000-0000-000000000000'
        ) {
          this.errorMessage = 'You are not part of any subscription!';
        } else {
          this.userStore.setLoggedInUser(response.dto);
          this.router.navigateByUrl(this.returnUrl);
        }
      },
      (error: HttpErrorResponse) => {
        if (error.status === 429) {
          this.errorMessage =
            'Too many login attempts! You can try again in one minute.';
        }
      }
    );
  }

  goToForgotPassword() {
    this.router.navigate([AppRoutes.forgotPassword], {
      queryParams: { email: this.loginDto.email },
    });
  }

  azureLogin() {
    this.msalAuthService.instance.setActiveAccount(null);
    if (this.msalGuardConfig.interactionType === InteractionType.Redirect) {
      if (this.msalGuardConfig.authRequest) {
        this.msalAuthService.loginRedirect({
          ...this.msalGuardConfig.authRequest,
        } as RedirectRequest);
      } else {
        this.msalAuthService.loginRedirect();
      }
    }
  }
}
