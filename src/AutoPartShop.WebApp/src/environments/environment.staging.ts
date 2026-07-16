// =============================================================================
// STAGING Environment Configuration
// =============================================================================
//
// WHAT THIS FILE DOES:
//   Provides environment-specific configuration for TEST/STAGING builds.
//   This file replaces environment.ts when you build with --configuration=staging
//   (via the fileReplacements in angular.json).
//
// HOW IT'S USED:
//   - Angular CLI swaps this file in during `ng build --configuration=staging`
//   - The Docker web container passes ENV_NAME=staging as a build arg
//   - This runs: npm run build -- --configuration=staging
//
// WHY STAGING (NOT PRODUCTION):
//   Staging uses the same relative /api URL as production (because nginx proxies),
//   but Angular's "staging" configuration keeps source maps ON for debugging.
//   This helps developers investigate issues on the test environment.
//
// FILE REPLACEMENT (angular.json):
//   "staging" configuration replaces:
//     environment.ts → environment.staging.ts
//
// =============================================================================

export const environment = {
    // Still marked as production-like (enables most optimizations)
    // but the "staging" build config keeps source maps for debugging
    production: true,

    // Same relative URL as production — nginx handles routing in both envs
    apiUrl: '/api',
};
