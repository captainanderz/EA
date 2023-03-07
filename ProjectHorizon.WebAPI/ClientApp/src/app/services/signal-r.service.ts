import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HubConnection } from '@microsoft/signalr';
import { SignalRConstants } from '../constants/signalr-constants';
import { UserStoreKeys } from '../constants/user-store-keys';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  public readonly connection: HubConnection;

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${SignalRConstants.SignalUrl}`, {
        accessTokenFactory: () =>
          localStorage.getItem(UserStoreKeys.accessToken)!,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
}
