import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AppSettingsService {
  constructor() {}

  setSnow(value: boolean) {
    localStorage.setItem('snow', String(value));
  }

  getSnow(): boolean {
    return localStorage.getItem('snow') === 'true';
  }

  toggleSnow() {
    this.setSnow(!this.getSnow());
  }
}
