import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { AuthService } from 'src/app/services/auth.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-login-mfa',
  templateUrl: './login-mfa.component.html',
})
export class LoginMfaComponent implements OnInit {
  readonly patterns = Patterns;
  readonly appRoutes = AppRoutes;

  returnUrl: string;
  enteredCode: string;
  errorMessage: string;

  constructor(
    private readonly authService: AuthService,
    private readonly userStore: UserStore,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  submit(): void {
    this.authService.loginMfaCode(this.enteredCode).subscribe((response) => {
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
