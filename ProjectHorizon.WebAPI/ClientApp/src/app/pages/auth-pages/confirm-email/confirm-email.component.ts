import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { EmailConfirmationDto } from 'src/app/dtos/email-confirmation-dto.model';
import { AppRoutes } from 'src/app/constants/app-routes';
import { AuthService } from 'src/app/services/auth.service';
import { ResultStatus } from 'src/app/constants/result-status';

@Component({
  selector: 'app-confirm-email',
  templateUrl: './confirm-email.component.html',
})
export class ConfirmEmailComponent implements OnInit {
  readonly resultStatus = ResultStatus;
  readonly emailConfirmationDto = new EmailConfirmationDto();

  confirmationResult: ResultStatus;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit() {
    this.emailConfirmationDto.token =
      this.route.snapshot.queryParamMap.get('token') ?? '';
    this.emailConfirmationDto.email =
      this.route.snapshot.queryParamMap.get('email') ?? '';

    this.authService.confirmEmail(this.emailConfirmationDto).subscribe(
      (_) => (this.confirmationResult = ResultStatus.Successful),
      (_) => (this.confirmationResult = ResultStatus.Failed)
    );
  }

  continue() {
    this.router.navigate([AppRoutes.login]);
  }
}
