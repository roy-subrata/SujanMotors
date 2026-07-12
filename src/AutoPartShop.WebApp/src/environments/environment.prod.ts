// =============================================================================
// PRODUCTION Environment Configuration
// =============================================================================
//
// WHAT THIS FILE DOES:
//   Provides environment-specific configuration for PRODUCTION builds.
//   This file replaces environment.ts when you build with --configuration=production
//   (via the fileReplacements in angular.json).
//
// HOW IT'S USED:
//   - Angular CLI swaps this file in during `ng build --configuration=production`
//   - The apiUrl is used for all HTTP API calls and SignalR hub connections
//   - This file is BAKED INTO the build output at compile time (not runtime)
//
// WHY RELATIVE URL:
//   In Docker deployment, nginx acts as a reverse proxy:
//     - Browser requests http://domain/api/... → nginx → API container:8080
//     - Browser requests http://domain/hubs/... → nginx → API container:8080
//   Using a relative URL "/api" means the browser uses the SAME origin (domain + port),
//   and nginx handles routing. This is simpler and works across environments.
//
// FILE REPLACEMENT (angular.json):
//   "production" configuration replaces:
//     environment.ts → environment.prod.ts
//
// =============================================================================

export const environment = {
    // Marks this as a production build (enables Angular optimizations)
    production: true,

    // Relative URL: browser sends API requests to the same domain it was loaded from.
    // nginx reverse-proxies /api/* to the API container.
    // Previous value was Azure App Service URL — changed for VPS Docker deployment.
    apiUrl: '/api',
};
