import { Component, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';
import { SignalRConstants } from 'src/app/constants/signalr-constants';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-hangfire',
  templateUrl: './hangfire.component.html',
  styleUrls: ['./hangfire.component.scss'],
})
export class HangfireComponent implements OnInit {
  private readonly unsubscribe$ = new Subject<void>();

  constructor(private userStore: UserStore) {}

  ngOnInit(): void {
    this.userStore
      .getLoggedInUser()
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (loggedInUser: UserDto | undefined): loggedInUser is UserDto =>
            loggedInUser !== undefined
        )
      )
      .subscribe((loggedInUser) => {
        this.setCookie('HangfireCookie', loggedInUser.accessToken, 10);
      });
  }

  public ngOnDestroy() {
    this.setCookie('HangfireCookie', '', -1);
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  setCookie(name: string, value: string, minutes: number) {
    if (minutes) {
      var date = new Date();
      date.setTime(date.getTime() + minutes * 60 * 1000);
      var expires = '; expires=' + date.toString();
    } else {
      var expires = '';
    }

    document.cookie = name + '=' + value + expires + '; path=/';
  }
}
