import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { UserStoreKeys } from '../constants/user-store-keys';
import { UserDto } from '../dtos/user-dto.model';

@Injectable({
  providedIn: 'root',
})
export class UserStore {
  // Fields
  private readonly loggedInUser = new BehaviorSubject<UserDto | undefined>(
    undefined
  );

  // Constructor
  constructor() {}

  // Methods
  // Gets the logged in user
  getLoggedInUser(): Observable<UserDto | undefined> {
    var userFromStorage = localStorage.getItem(UserStoreKeys.loggedInUser);

    if (userFromStorage) {
      let userDto = JSON.parse(userFromStorage) as UserDto;

      this.loggedInUser.next(userDto);
    }

    return this.loggedInUser.asObservable();
  }

  setLoggedInUser(newUserDto: UserDto) {
    this.saveUserInLocalStorage(newUserDto);

    this.loggedInUser.next(newUserDto);
  }

  // Saves the user in the local storage so the users remains logged in until he loggs out or the token expires
  saveUserInLocalStorage(newUserDto: UserDto) {
    // saving the userDto also in the local storage
    // because we would lose it otherwise on a full page reload
    localStorage.setItem(
      UserStoreKeys.loggedInUser,
      JSON.stringify(newUserDto)
    );
  }

  // Handles the logout logic, removing the user from the local storage
  logout() {
    localStorage.removeItem(UserStoreKeys.loggedInUser);
  }
}
