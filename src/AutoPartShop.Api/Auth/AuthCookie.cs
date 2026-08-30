using System.Net;
using System.Security.Claims;

namespace AutoPartShop.Api.Auth;

/// <summary>
/// Helpers for the httpOnly auth cookies that web browsers use instead of the
/// Authorization header.
///
/// Dual-mode: the backend accepts a Bearer header OR the cookie on every endpoint.
/// Mobile clients keep using Bearer (body tokens returned as before); web SPA
/// switches to cookies entirely — tokens never leave the server once set.
///
/// Cookie names: ap_access (short-lived), ap_refresh (session lifetime).
/// Both: HttpOnly, SameSite=Lax, Secure outside Development, path-scoped.
/// </summary>
public static class AuthCookie
{
    public const string AccessName = "ap_access";
    public const string RefreshName = "ap_refresh";

    /// <summary>
    /// Sets both auth cookies after a successful login or refresh.
    /// The web SPA reads these automatically; mobile ignores them.
    /// </summary>
    public static void SetAuthCookies(
        HttpResponse response,
        string accessToken,
        int accessExpiryMinutes,
        string refreshToken,
        DateTime refreshExpiresAt,
        bool isDevelopment)
    {
        var secure = !isDevelopment;

        response.Cookies.Append(AccessName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(accessExpiryMinutes),
            IsEssential = true,
        });

        response.Cookies.Append(RefreshName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth/",
            Expires = refreshExpiresAt,
            IsEssential = true,
        });
    }

    /// <summary>
    /// Expires both auth cookies (logout, password change, session compromise).
    /// </summary>
    public static void ClearAuthCookies(HttpResponse response, bool isDevelopment)
    {
        var secure = !isDevelopment;
        var epoch = DateTimeOffset.UnixEpoch;

        response.Cookies.Append(AccessName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.Zero,
            IsEssential = true,
        });

        response.Cookies.Append(RefreshName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth/",
            MaxAge = TimeSpan.Zero,
            IsEssential = true,
        });
    }
}
