import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from 'src/app/constants/app-routes';
import { ResultStatus } from 'src/app/constants/result-status';
import { EmailConfirmationDto } from 'src/app/dtos/email-confirmation-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { UserService } from 'src/app/services/user.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-confirm-change-email',
  templateUrl: './confirm-change-email.component.html',
})
export class ConfirmChangeEmailComponent implements OnInit {
  readonly resultStatus = ResultStatus;
  readonly emailConfirmationDto = new EmailConfirmationDto();

  confirmationResult: ResultStatus;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly userService: UserService,
    private readonly userStore: UserStore,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit() {
    this.emailConfirmationDto.email =
      this.route.snapshot.queryParamMap.get('email') ?? '';
    this.emailConfirmationDto.token =
      this.route.snapshot.queryParamMap.get('token') ?? '';

    this.authService.confirmChangeEmail(this.emailConfirmationDto).subscribe(
      (_) => {
        this.confirmationResult = ResultStatus.Successful;

        this.userService
          .get()
          .subscribe((userDto: UserDto) =>
            this.userStore.setLoggedInUser(userDto)
          );
      },
      (_) => (this.confirmationResult = ResultStatus.Failed)
    );
  }

  continue() {
    this.router.navigate([AppRoutes.root]);
  }
}
