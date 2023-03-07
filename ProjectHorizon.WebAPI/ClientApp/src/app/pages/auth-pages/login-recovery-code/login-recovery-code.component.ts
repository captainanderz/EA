import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from 'src/app/constants/app-routes';
import { AuthService } from 'src/app/services/auth.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-login-recovery-code',
  templateUrl: './login-recovery-code.component.html',
})
export class LoginRecoveryCodeComponent implements OnInit {
  readonly appRoutes = AppRoutes;

  returnUrl: string;
  enteredCode: string;
  errorMessage: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly userStore: UserStore
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  submit(): void {
    this.authService
      .loginRecoveryCode(this.enteredCode)
      .subscribe((response) => {
        if (!response.isSuccessful) this.errorMessage = response.errorMessage;
        else {
          this.userStore.setLoggedInUser(response.dto);

          if (response.dto.subscriptionId)
            this.router.navigateByUrl(this.returnUrl);
          else this.router.navigate([AppRoutes.createSubscription]);
        }
      });
  }
}
