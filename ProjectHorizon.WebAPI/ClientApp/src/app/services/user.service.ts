import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ReadonlyUserDto, UserDto } from '../dtos/user-dto.model';
import { ReadonlyBulkChangeUsersRoleDto } from '../dtos/bulk-change-users-role-dto.model';
import { ChangePasswordDto } from '../dtos/change-password-dto.model';
import { UserSettingsDto } from '../dtos/user-settings-dto.model';
import { UserInvitationDto } from '../dtos/user-invitation-dto.model';
import { RegisterInvitationDto } from '../dtos/register-invitation-dto';
import { InvitedUserDto } from '../dtos/register-invitation-dto copy';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly usersUrl: string = 'api/users';

  constructor(private readonly httpClient: HttpClient) {}

  get(): Observable<UserDto> {
    return this.httpClient.get<UserDto>(this.usersUrl);
  }

  getSuperAdminUsers(): Observable<ReadonlyArray<UserDto>> {
    return this.httpClient.get<ReadonlyArray<UserDto>>(
      `${this.usersUrl}/superadmin-users`
    );
  }

  getProfilePicture(): Observable<any> {
    return this.httpClient.get(`${this.usersUrl}/profile-picture`, {
      responseType: 'blob',
    });
  }

  reloadSubscription(): Observable<UserDto> {
    return this.httpClient.get<UserDto>(`${this.usersUrl}/reload-subscription`);
  }

  registerInvitation(
    registerInvitationDto: RegisterInvitationDto
  ): Observable<any> {
    return this.httpClient.post(
      `${this.usersUrl}/register-invitation`,
      registerInvitationDto
    );
  }

  changeCurrentSubscription(newSubscriptionId: string): Observable<UserDto> {
    return this.httpClient.patch<UserDto>(
      `${this.usersUrl}/change-subscription`,
      `\"${newSubscriptionId}\"`,
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/json',
        }),
      }
    );
  }

  findUsersBySubscription(): Observable<ReadonlyArray<ReadonlyUserDto>> {
    return this.httpClient.get<ReadonlyArray<ReadonlyUserDto>>(
      `${this.usersUrl}/list-by-subscription`
    );
  }

  removeUsers(userIds: ReadonlyArray<string>): Observable<any> {
    const options = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      }),
      body: userIds,
    };

    return this.httpClient.delete(this.usersUrl, options);
  }

  changeUsersRole(
    bulkChangeUsersRoleDto: ReadonlyBulkChangeUsersRoleDto
  ): Observable<any> {
    return this.httpClient.patch(
      `${this.usersUrl}/change-role`,
      bulkChangeUsersRoleDto
    );
  }

  changePassword(dto: ChangePasswordDto): Observable<any> {
    return this.httpClient.put(`${this.usersUrl}/change-password`, dto);
  }

  inviteUser(inviteUserDto: UserInvitationDto): Observable<any> {
    return this.httpClient.post(`${this.usersUrl}/invite`, inviteUserDto);
  }

  changeUserDetails(dto: UserSettingsDto): Observable<any> {
    return this.httpClient.put(`${this.usersUrl}/settings`, dto);
  }

  changeProfilePicture(picture: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', picture, picture.name);
    return this.httpClient.put(`${this.usersUrl}/profile-picture`, formData);
  }

  deleteProfilePicture(): Observable<any> {
    return this.httpClient.delete(`${this.usersUrl}/profile-picture`);
  }

  isInvitedUserAlreadyRegistered(
    inviteUserDto: InvitedUserDto
  ): Observable<any> {
    return this.httpClient.put(
      `${this.usersUrl}/is-user-already-registered`,
      inviteUserDto
    );
  }

  toggleTwoFactorAuthentication(id: string): Observable<any> {
    return this.httpClient.put(
      `${this.usersUrl}/toggle-two-factor-authentication/${id}`,
      null
    );
  }

  makeUserSuperAdmin(id: string): Observable<any> {
    return this.httpClient.put(
      `${this.usersUrl}/make-user-super-admin/${id}`,
      null
    );
  }

  removeUserSuperAdmin(id: string): Observable<any> {
    return this.httpClient.put(
      `${this.usersUrl}/remove-user-super-admin/${id}`,
      null
    );
  }
}
