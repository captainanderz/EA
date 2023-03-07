import { Component, OnDestroy, OnInit } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { UserRole } from 'src/app/constants/user-role';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-users',
  templateUrl: './superadmin-users.component.html',
})
export class SuperAdminUsersComponent implements OnInit, OnDestroy {
  users: ReadonlyArray<UserDto>;
  readonly userRole = UserRole;
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  constructor(public userService: UserService) {}

  ngOnInit(): void {
    this.behaviorSubject$
      .pipe(switchMap(() => this.userService.getSuperAdminUsers()))
      .subscribe((users: ReadonlyArray<UserDto>) => {
        this.users = users;
      });
  }

  update() {
    this.behaviorSubject$.next({});
  }

  ngOnDestroy(): void {
    this.behaviorSubject$.complete();
  }

  toggleTwoFactorAuthentication(email: string): void {
    this.userService
      .toggleTwoFactorAuthentication(email)
      .subscribe((_) => this.update());
  }

  removeSuperAdminRole(superAdminId: string): void {
    this.userService
      .removeUserSuperAdmin(superAdminId)
      .subscribe((_) => this.update());
  }
}
