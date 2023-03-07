import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { map, mergeMap, takeUntil } from 'rxjs/operators';
import { Patterns } from 'src/app/constants/patterns';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { UserStore } from 'src/app/services/user.store';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';

@Component({
  selector: 'app-setup-authenticator',
  templateUrl: './setup-authenticator.component.html',
})
export class SetupAuthenticatorComponent implements OnInit, OnDestroy {
  private readonly unsubscribe$ = new Subject<void>();
  readonly patterns = Patterns;

  constructor(
    private readonly authService: AuthService,
    private readonly userStore: UserStore,
    private readonly modalService: NgbModal,
    private readonly router: Router
  ) {}

  @Input() confirmRoute: string;
  key: string;
  qrUri: string;
  enteredCode: string;

  ngOnInit(): void {
    this.authService
      .generateAuthenticatorKey()
      .pipe(
        takeUntil(this.unsubscribe$),
        mergeMap((key) =>
          this.userStore.getLoggedInUser().pipe(map((user) => ({ key, user })))
        )
      )
      .subscribe((tuple) => {
        const { key, user } = tuple;
        if (user) {
          this.key = key;
          this.qrUri = this.buildUri(key, user);
        }
      });
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  submit(): void {
    this.authService.enableTwoFactor(this.enteredCode).subscribe((response) => {
      if (!response.isSuccessful) {
        const modalRef = this.modalService.open(InfoModalComponent);
        modalRef.componentInstance.title = 'There was a problem!';
        modalRef.componentInstance.content1 =
          'The code is invalid, please try again.';
      } else {
        this.userStore.setLoggedInUser(response.dto);
        this.router.navigate([this.confirmRoute], {
          queryParams: { showActivated: true },
        });
      }
    });
  }

  private buildUri(key: string, user: UserDto): string {
    const encodedIssuer = encodeURIComponent('Endpoint Admin');
    const encodedEmail = encodeURIComponent(user.email);
    return `otpauth://totp/${encodedIssuer}:${encodedEmail}?secret=${key}&issuer=${encodedIssuer}&digits=6`;
  }
}
