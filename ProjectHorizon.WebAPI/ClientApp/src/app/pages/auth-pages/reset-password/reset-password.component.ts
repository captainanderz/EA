import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { ResultStatus } from 'src/app/constants/result-status';
import { PasswordResetDto } from 'src/app/dtos/password-reset-dto.model';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent implements OnInit {
  readonly resultStatusValues = ResultStatus;
  readonly patterns = Patterns;
  readonly passwordResetDto = new PasswordResetDto();

  resultStatus: ResultStatus;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit() {
    this.passwordResetDto.token =
      this.route.snapshot.queryParamMap.get('token') ?? '';
    this.passwordResetDto.email =
      this.route.snapshot.queryParamMap.get('email') ?? '';
  }

  submit() {
    if (
      this.passwordResetDto.newPassword !== this.passwordResetDto.repeatPassword
    )
      return;

    this.authService.resetPassword(this.passwordResetDto).subscribe(
      () => (this.resultStatus = ResultStatus.Successful),
      () => (this.resultStatus = ResultStatus.Failed)
    );
  }

  goToLogin() {
    this.router.navigate([AppRoutes.login]);
  }

  goToForgotPassword() {
    this.router.navigate([AppRoutes.forgotPassword]);
  }
}
