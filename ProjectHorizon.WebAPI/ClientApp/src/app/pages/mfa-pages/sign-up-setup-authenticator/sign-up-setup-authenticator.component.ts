import { Component, OnInit } from '@angular/core';
import { AppRoutes } from 'src/app/constants/app-routes';

@Component({
  selector: 'app-sign-up-setup-authenticator',
  templateUrl: './sign-up-setup-authenticator.component.html',
})
export class SignUpSetupAuthenticatorComponent implements OnInit {
  readonly appRoutes = AppRoutes;

  constructor() {}

  ngOnInit(): void {}
}
