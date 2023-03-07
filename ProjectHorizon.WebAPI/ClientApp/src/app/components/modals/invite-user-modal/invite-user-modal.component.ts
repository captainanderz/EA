import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Patterns } from 'src/app/constants/patterns';
import { UserRole } from 'src/app/constants/user-role';
import { UserInvitationDto } from 'src/app/dtos/user-invitation-dto.model';
import { UserService } from 'src/app/services/user.service';
import { isPersonNameValid } from 'src/app/utility';
import { InfoModalComponent } from '../info-modal/info-modal.component';

@Component({
  selector: 'invite-user-modal',
  templateUrl: './invite-user-modal.component.html',
})
export class InviteUserModalComponent implements OnInit {
  readonly userInvitationDto = new UserInvitationDto();

  readonly userRoles = UserRole;
  readonly patterns = Patterns;
  isPersonNameValid = isPersonNameValid;

  readonly roleSelectionTag = 'Select user role';

  constructor(
    public activeModal: NgbActiveModal,
    private readonly modalService: NgbModal,
    private readonly userService: UserService
  ) {}

  ngOnInit(): void {
    this.userInvitationDto.userRole = this.roleSelectionTag;
  }

  submit(form: NgForm): void {
    if (
      form.value.userRole === this.roleSelectionTag ||
      !isPersonNameValid(this.userInvitationDto.firstName) ||
      !isPersonNameValid(this.userInvitationDto.lastName)
    )
      return;

    this.userService.inviteUser(this.userInvitationDto).subscribe(
      (_) => {
        this.invitationSucceded();
      },
      (error) => {
        switch (error.status) {
          case 400: {
            this.invitationFailed(error.error);
            break;
          }
          case 500: {
            this.invitationFailed();
            break;
          }
        }
      }
    );
    this.activeModal.close();
  }

  private invitationFailed(errorMessage?: string): void {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Invitation failed';
    modalRef.componentInstance.content1 = `Failed to send invitation to ${this.userInvitationDto.email}`;

    if (errorMessage) modalRef.componentInstance.content2 = errorMessage;
  }

  private invitationSucceded(): void {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Invitation sent';
    modalRef.componentInstance.content1 = `Successfully sent invitation to ${this.userInvitationDto.email}`;
  }
}
