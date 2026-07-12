// Staging / Test environment — used by `ng build --configuration staging`.
// Swapped in for environment.ts via fileReplacements in angular.json.
//
// apiUrl points to the staging API (separate Azure App Service or
// a different port on the same server). The staging SWA fetches
// from this URL at runtime.
//
// ▶ Replace the URL below with your staging API App Service URL.
export const environment = {
    production: false,
    apiUrl: 'https://sujanmotors-api-test.azurewebsites.net/api'
};
