export class AzureScopes {
  static readonly ApiScope = () => {
    const request = new XMLHttpRequest();
    request.open('GET', '/api/application-information', false); // request application settings synchronous
    request.send(null);
    const response = JSON.parse(request.responseText);
    return response.apiScope;
  };
}
