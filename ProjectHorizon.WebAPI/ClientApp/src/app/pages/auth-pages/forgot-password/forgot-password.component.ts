import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent implements OnInit {
  email: string;
  resetEmailWasSent = false;

  readonly appRoutes = AppRoutes;
  readonly patterns = Patterns;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly authService: AuthService
  ) {}

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
  }

  submit() {
    this.authService
      .forgotPassword(this.email)
      .subscribe(() => (this.resetEmailWasSent = true));
  }
}
