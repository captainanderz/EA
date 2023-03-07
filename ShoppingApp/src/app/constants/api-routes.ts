import { environment } from 'src/environments/environment';

export class ApiRoutes {
  public static readonly root = environment.apiUrl + '/api';
  public static readonly shopping = ApiRoutes.root + '/shopping';
  public static readonly applications = ApiRoutes.shopping + '/applications';
  public static readonly requests =
    ApiRoutes.shopping + '/subordinates/requests';
  public static readonly login = ApiRoutes.root + '/azure-auth/simple-login';
  public static readonly subscriptionLogo =
    ApiRoutes.shopping + '/subscription-logo';
}
