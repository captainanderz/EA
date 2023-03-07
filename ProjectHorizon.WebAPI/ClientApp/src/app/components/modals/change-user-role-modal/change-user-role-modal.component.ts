import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { displayUserRole, UserRole } from 'src/app/constants/user-role';

@Component({
  selector: 'app-change-user-role-modal',
  templateUrl: './change-user-role-modal.component.html',
  styleUrls: ['./change-user-role-modal.component.scss'],
})
export class ChangeUserRoleModalComponent {
  readonly displayUserRole = displayUserRole;
  readonly userRoles: ReadonlyArray<UserRole>;
  selectedUserRole = UserRole.Reader;

  constructor(public activeModal: NgbActiveModal) {
    this.userRoles = Object.values(UserRole).filter(
      (userRole) => userRole !== UserRole.SuperAdmin
    );
  }
}
