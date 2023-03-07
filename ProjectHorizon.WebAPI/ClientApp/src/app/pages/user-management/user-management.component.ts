import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Observable, of, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ChangeUserRoleModalComponent } from 'src/app/components/modals/change-user-role-modal/change-user-role-modal.component';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { InviteUserModalComponent } from 'src/app/components/modals/invite-user-modal/invite-user-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import {
  displayUserBulkOption,
  displayUserRole,
  UserBulkOption,
  UserRole,
} from 'src/app/constants/user-role';
import { BulkChangeUsersRoleDto } from 'src/app/dtos/bulk-change-users-role-dto.model';
import { ReadonlyUserDto, UserDto } from 'src/app/dtos/user-dto.model';
import { UserService } from 'src/app/services/user.service';
import { UserStore } from 'src/app/services/user.store';
import { MultipleSelectDirective } from '../multiple-select.directive';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
})
export class UserManagementComponent
  extends MultipleSelectDirective<string, ReadonlyUserDto, UserBulkOption>
  implements OnInit, OnDestroy
{
  readonly displayUserBulkOption = displayUserBulkOption;
  readonly displayUserRole = displayUserRole;
  readonly userBulkOption = UserBulkOption;
  readonly userRole = UserRole;
  readonly userBulkOptions = Object.values(UserBulkOption);
  readonly bulkChangeUsersRoleDto = new BulkChangeUsersRoleDto();

  loggedInUser: UserDto;
  isUserSuperAdmin: boolean;

  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly modalService: NgbModal,
    protected readonly userStore: UserStore,
    protected readonly activatedRoute: ActivatedRoute,
    private readonly userService: UserService,
    protected readonly router: Router
  ) {
    super(userStore);
  }

  ngOnInit() {
    super.ngOnInit();
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        this.isUserSuperAdmin = loggedInUser?.userRole == UserRole.SuperAdmin;

        if (
          loggedInUser?.userRole == UserRole.SuperAdmin ||
          loggedInUser?.userRole == UserRole.Administrator
        ) {
          this.loggedInUser = loggedInUser;

          this.findUsersByCurrentSubscription();
        } else this.router.navigate([AppRoutes.root]);
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  getAllItemIds(): Observable<string[]> {
    return of(this.pagedItems.map((item) => item.id));
  }

  private findUsersByCurrentSubscription() {
    this.userService
      .findUsersBySubscription()
      .subscribe((users: ReadonlyArray<ReadonlyUserDto>) => {
        this.pagedItems = users;
        this.allItemsCount = users.length;
      });
  }

  applyUserBulkOption() {
    switch (this.selectedOption) {
      case UserBulkOption.RemoveSelected:
        this.openRemoveUsersConfirmationModal(Array.from(this.selectedItemIds));
        break;
      case UserBulkOption.ChangeRoles:
        this.openChangeUserRoleModal(Array.from(this.selectedItemIds));
        break;
    }
  }

  removeUser(userId: string) {
    this.openRemoveUsersConfirmationModal([userId]);
  }

  changeUserRole(userId: string) {
    this.openChangeUserRoleModal([userId]);
  }

  private openRemoveUsersConfirmationModal(
    selectedUserIds: ReadonlyArray<string>
  ) {
    const subscriptionName =
      this.loggedInUser.subscriptions[
        this.loggedInUser.currentSubscriptionIndex
      ].name;

    const modalRef = this.modalService.open(ConfirmationModalComponent, {
      backdrop: 'static',
    });

    modalRef.componentInstance.content1 = `Selected user/(s) from the current subscription (${subscriptionName}) will be removed.`;
    modalRef.componentInstance.content2 = `Do you want to continue?`;

    modalRef.componentInstance.continue.subscribe(() =>
      this.userService
        .removeUsers(selectedUserIds)
        .subscribe((_) => this.findUsersByCurrentSubscription())
    );

    modalRef.closed.subscribe((_) => this.selectedItemIds.clear());
    modalRef.dismissed.subscribe((_) => this.selectedItemIds.clear());
  }

  private openChangeUserRoleModal(selectedUserIds: ReadonlyArray<string>) {
    const modalRef = this.modalService.open(ChangeUserRoleModalComponent, {
      backdrop: 'static',
      windowClass: 'changeUserModalClass',
    });

    modalRef.closed.subscribe((selectedUserRole: UserRole | undefined) => {
      if (selectedUserRole) {
        this.bulkChangeUsersRoleDto.userIds = selectedUserIds;
        this.bulkChangeUsersRoleDto.newUserRole = selectedUserRole;

        this.userService
          .changeUsersRole(this.bulkChangeUsersRoleDto)
          .subscribe((_) => this.findUsersByCurrentSubscription());
      }
    });

    modalRef.dismissed.subscribe((_) => this.selectedItemIds.clear());
  }

  openInviteUserModal() {
    this.modalService.open(InviteUserModalComponent, {
      size: 'md',
    });
  }

  toggleTwoFactorAuthentication(email: string): void {
    this.userService
      .toggleTwoFactorAuthentication(email)
      .subscribe((_) => this.findUsersByCurrentSubscription());
  }

  makeUserSuperAdmin(email: string): void {
    this.userService
      .makeUserSuperAdmin(email)
      .subscribe((_) => this.findUsersByCurrentSubscription());
  }

  selectAll() {
    this.selectAllItems();
    this.selectedItemIds.delete(this.loggedInUser.id);
  }
}
