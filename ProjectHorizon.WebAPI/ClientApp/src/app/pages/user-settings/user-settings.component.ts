import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { takeUntil, mergeMap, tap } from 'rxjs/operators';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { ChangePasswordDto } from 'src/app/dtos/change-password-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { UserService } from 'src/app/services/user.service';
import { isPersonNameValid } from 'src/app/utility';
import { UserStore } from '../../services/user.store';

@Component({
  selector: 'app-user-settings',
  templateUrl: './user-settings.component.html',
})
export class UserSettingsComponent implements OnInit, OnDestroy {
  readonly appRoutes = AppRoutes;
  readonly changePasswordDto = new ChangePasswordDto();
  userDto = new UserDto();
  userImage: SafeUrl | string;
  userEmail?: string;
  addOrReplacePictureText: string;
  showDeletePicture: boolean;
  recoveryCodesNumber: number;

  @ViewChild('changePasswordForm') changePasswordForm: NgForm;

  readonly patterns = Patterns;
  isPersonNameValid = isPersonNameValid;

  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly userStore: UserStore,
    private readonly userService: UserService,
    private readonly authService: AuthService,
    private readonly modalService: NgbModal,
    private readonly sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        if (loggedInUser) {
          this.userDto = loggedInUser;
          this.userEmail = loggedInUser.email;
          if (loggedInUser.profilePictureSmall) {
            this.addOrReplacePictureText = 'Replace photo';
            this.showDeletePicture = true;
          } else {
            this.addOrReplacePictureText = 'Add photo';
            this.showDeletePicture = false;
          }
        }
      });

    this.loadProfilePicture();

    this.authService
      .recoveryCodesNumber()
      .subscribe((n) => (this.recoveryCodesNumber = n));
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  onProfilePictureSelected(event: any): void {
    const file: File = event.target.files[0];
    this.userService
      .changeProfilePicture(file)
      .pipe(
        mergeMap((_) => this.userService.get()),
        tap((_) => this.loadProfilePicture())
      )
      .subscribe((u) => {
        this.userStore.setLoggedInUser(u);
      });
  }

  deleteProfilePicture(): void {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `This will permanently delete your profile picture.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.userService
        .deleteProfilePicture()
        .pipe(
          mergeMap((_) => this.userService.get()),
          tap((_) => this.loadProfilePicture())
        )
        .subscribe((u) => {
          this.userStore.setLoggedInUser(u);
        });
    });
  }

  submitChangeSettings(): void {
    if (
      !isPersonNameValid(this.userDto.firstName) ||
      !isPersonNameValid(this.userDto.lastName)
    )
      return;

    this.userService
      .changeUserDetails({
        email: this.userDto.email,
        firstName: this.userDto.firstName,
        lastName: this.userDto.lastName,
        phoneNumber:
          this.userDto.phoneNumber == '' ? null : this.userDto.phoneNumber,
      })
      .pipe(
        tap((needToConfirmEmail) => {
          if (needToConfirmEmail) {
            this.openInfoModal(
              'One more thing',
              'You will need to confirm your new email address by clicking the link in the email we just sent you.'
            );
          } else if (this.userEmail != this.userDto.email) {
            this.openInfoModal(
              'There was a problem!',
              'There is already another account with this email address.'
            );
          }
        }),
        mergeMap((_) => this.userService.get())
      )
      .subscribe(
        (u) => {
          this.userStore.setLoggedInUser(u);
        },
        (_) => {
          this.openInfoModal(
            'There was a problem!',
            "We weren't able to change your settings. Please try again. Contact support if the problem persists."
          );
        }
      );
  }

  submitChangePassword(form: NgForm): void {
    if (
      form.value.newPassword !== form.value.confirmNewPassword ||
      form.value.newPassword === form.value.currentPassword
    )
      return;

    this.userService.changePassword(this.changePasswordDto).subscribe(
      () => {
        this.changePasswordForm.resetForm();

        this.openInfoModal('Success!', 'Password changed successfully');
      },
      () => {
        this.openInfoModal(
          'There was a problem!',
          "We weren't able to change your password. Please try again. Contact support if the problem persists."
        );
      }
    );
  }

  private openInfoModal(title: string, content: string): void {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = title;
    modalRef.componentInstance.content1 = content;
  }

  private loadProfilePicture() {
    this.userService
      .getProfilePicture()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(
        (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.userImage = this.sanitizer.bypassSecurityTrustUrl(objectUrl);
        },
        (_) => {
          this.userImage = '../../../assets/images/blank-profile.png';
        }
      );
  }
}
