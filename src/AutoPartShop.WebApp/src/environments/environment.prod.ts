// Production environment — used by `ng build` (default configuration = production),
// swapped in for environment.ts via the `fileReplacements` in angular.json.
//
// apiUrl MUST be the absolute base URL of the API on Azure App Service, ending in /api,
// because the Static Web App and the API live on different origins. The SignalR hub URL
// is derived from this value (apiUrl.replace('/api','') + '/hubs/...'), so this one
// setting covers REST + real-time.
//
// ▶ Replace <API_APP_NAME> with your App Service name (or a custom domain). After changing
//   it, also add the Static Web App origin to the API's Cors__AllowedOrigins app setting.
export const environment = {
    production: true,
    apiUrl: 'https://sujanmotors-api-gtetffcscjg3cyfe.southeastasia-01.azurewebsites.net/api'
};
