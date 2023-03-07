import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-recovery-codes',
  templateUrl: './recovery-codes.component.html',
})
export class RecoveryCodesComponent implements OnInit, OnDestroy {
  private readonly unsubscribe$ = new Subject<void>();

  constructor(private readonly authService: AuthService) {}

  @Input() showActivated = false;
  newCodes: string[];

  ngOnInit(): void {
    this.authService
      .generateNewTwoFactorRecoveryCodes()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((newCodes) => {
        this.newCodes = newCodes.map(
          (c) => `${c.substring(0, 4)}-${c.substring(4)}`
        );
      });
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
