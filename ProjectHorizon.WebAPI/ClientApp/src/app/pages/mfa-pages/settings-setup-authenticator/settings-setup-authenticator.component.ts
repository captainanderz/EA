import { Component, OnInit } from '@angular/core';
import { AppRoutes } from 'src/app/constants/app-routes';

@Component({
  selector: 'app-settings-setup-authenticator',
  templateUrl: './settings-setup-authenticator.component.html',
})
export class SettingsSetupAuthenticatorComponent implements OnInit {
  readonly appRoutes = AppRoutes;
  constructor() {}

  ngOnInit(): void {}
}
