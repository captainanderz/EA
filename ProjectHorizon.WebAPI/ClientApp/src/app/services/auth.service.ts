import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserDto } from '../dtos/user-dto.model';
import { Response } from '../dtos/response.model';
import { LoginDto } from '../dtos/login-dto.model';
import { EmailConfirmationDto } from '../dtos/email-confirmation-dto.model';
import { PasswordResetDto } from '../dtos/password-reset-dto.model';
import { RegistrationDto } from '../dtos/registration-dto.model';
import { FarPayResult } from '../dtos/far-pay-result.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authUrl: string = 'api/auth';

  constructor(private readonly httpClient: HttpClient) {}

  register(
    registrationDto: RegistrationDto
  ): Observable<Response<FarPayResult>> {
    return this.httpClient.post<Response<FarPayResult>>(
      `${this.authUrl}/register`,
      registrationDto
    );
  }

  login(loginDto: LoginDto): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/login`,
      loginDto
    );
  }

  loginMfaCode(code: string): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/login-mfa-code`,
      { code }
    );
  }

  loginRecoveryCode(code: string): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/login-recovery-code`,
      `\"${code}\"`,
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/json',
        }),
      }
    );
  }

  confirmEmail(emailConfirmationDto: EmailConfirmationDto): Observable<any> {
    return this.httpClient.post(
      `${this.authUrl}/confirm-email`,
      emailConfirmationDto
    );
  }

  confirmChangeEmail(
    emailConfirmationDto: EmailConfirmationDto
  ): Observable<any> {
    return this.httpClient.post(
      `${this.authUrl}/confirm-change-email`,
      emailConfirmationDto
    );
  }

  forgotPassword(email: string): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/forgot-password`,
      `\"${email}\"`, // \" and Content-Type needed to post a simple string in body
      {
        headers: new HttpHeaders({
          'Content-Type': 'application/json',
        }),
      }
    );
  }

  resetPassword(passwordResetDto: PasswordResetDto): Observable<any> {
    return this.httpClient.post(
      `${this.authUrl}/reset-password`,
      passwordResetDto
    );
  }

  generateAuthenticatorKey(): Observable<string> {
    return this.httpClient.get(`${this.authUrl}/authenticator-key`, {
      responseType: 'text',
    });
  }

  enableTwoFactor(code: string): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/enable-two-factor`,
      { code }
    );
  }

  recoveryCodesNumber(): Observable<number> {
    return this.httpClient.get<number>(`${this.authUrl}/recovery-codes-number`);
  }

  generateNewTwoFactorRecoveryCodes(): Observable<string[]> {
    return this.httpClient.post<string[]>(
      `${this.authUrl}/generate-new-two-factor-recovery-codes`,
      null
    );
  }

  getNewTokens(): Observable<Response<UserDto>> {
    return this.httpClient.post<Response<UserDto>>(
      `${this.authUrl}/new-tokens`,
      null
    );
  }

  revokeRefreshToken(): Observable<any> {
    return this.httpClient.post(`${this.authUrl}/revoke-refresh-token`, null);
  }

  checkEmail(email: string): Observable<boolean> {
    const params = new HttpParams().set('email', email);

    return this.httpClient.get<boolean>(`${this.authUrl}/check-email`, {
      params,
    });
  }

  checkAcceptedTerms(): Observable<boolean> {
    return this.httpClient.get<boolean>(`${this.authUrl}/check-accepted-terms`);
  }
}
