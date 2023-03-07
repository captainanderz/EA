import { Component, Inject, OnInit } from '@angular/core';
import {
  MsalGuardConfiguration,
  MsalService,
  MSAL_GUARD_CONFIG,
} from '@azure/msal-angular';
import { InteractionType, RedirectRequest } from '@azure/msal-browser';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  // Constructor
  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private msalAuthService: MsalService
  ) {}

  // Methods
  ngOnInit(): void {
    // this.msalAuthService.logout();
  }

  // Handles the logic of login with an azure account
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
