import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { I18nService } from '../services/i18n.service';

/**
 * Sends every HTTP request with an `Accept-Language` header set to the currently
 * selected UI language, so the API can render server-side documents (invoices,
 * quotations, reports, etc.) in the same language the user sees in the app.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const i18nService = inject(I18nService);

  const isAssetRequest = req.url.startsWith('/assets/') || req.url.includes('/assets/');

  const langReq = isAssetRequest
    ? req
    : req.clone({ setHeaders: { 'Accept-Language': i18nService.getCurrentLanguage() } });

  return next(langReq);
};
