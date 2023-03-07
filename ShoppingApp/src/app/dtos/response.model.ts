export class Response<T> {
  isSuccessful: boolean;
  errorMessage: string;
  dto: T;
}
