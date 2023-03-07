import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { RegistrationDto } from '../dtos/registration-dto.model';

@Injectable({
  providedIn: 'root',
})
export class RegistrationDataService {
  private dataSource = new BehaviorSubject<RegistrationDto>(
    new RegistrationDto()
  );
  currentRegistrationInfo = this.dataSource.asObservable();

  constructor() {}

  changeRegistrationInfo(registrationDto: RegistrationDto) {
    this.dataSource.next(registrationDto);
  }
}
