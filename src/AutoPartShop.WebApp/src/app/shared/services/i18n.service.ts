import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

export type Translations = { [key: string]: any };

@Injectable({
    providedIn: 'root'
})
export class I18nService {
    private currentLang = signal<string>('en');
    private translations = signal<Translations>({});
    private fallbackTranslations: Translations = {};
    private translationsLoaded = new BehaviorSubject<boolean>(false);

    constructor(private httpClient: HttpClient) {}

    // Initialize with English fallback and load saved language
    initialize(): Observable<Translations> {
        // Load English as fallback first
        return new Observable(observer => {
            this.httpClient.get<Translations>('/assets/i18n/en.json').subscribe({
                next: (enTranslations) => {
                    // Store English as fallback
                    this.fallbackTranslations = enTranslations;
                    this.translations.set(enTranslations);

                    // Check for saved language preference
                    const savedLang = localStorage.getItem('appLanguage');

                    if (savedLang && savedLang !== 'en') {
                        // Load saved language
                        this.currentLang.set(savedLang);
                        this.loadLanguage(savedLang).subscribe({
                            next: () => {
                                this.translationsLoaded.next(true);
                                observer.next(this.translations());
                                observer.complete();
                            },
                            error: (err) => {
                                console.warn(`Failed to load saved language '${savedLang}', using English`, err);
                                this.currentLang.set('en');
                                this.translationsLoaded.next(true);
                                observer.next(this.translations());
                                observer.complete();
                            }
                        });
                    } else {
                        // Default to English
                        this.currentLang.set('en');
                        this.translationsLoaded.next(true);
                        observer.next(enTranslations);
                        observer.complete();
                    }
                },
                error: (err) => {
                    console.error('Failed to load English fallback translations', err);
                    observer.error(err);
                }
            });
        });
    }

    loadLanguage(lang: string): Observable<Translations> {
        return this.httpClient.get<Translations>(`/assets/i18n/${lang}.json`).pipe(
            tap(translations => {
                this.translations.set(translations);
                this.currentLang.set(lang);
                localStorage.setItem('appLanguage', lang);
                this.translationsLoaded.next(true);
            })
        );
    }

    setLanguage(lang: string): void {
        // Set language immediately in localStorage and signal
        this.currentLang.set(lang);
        localStorage.setItem('appLanguage', lang);

        // Load translations from JSON file
        this.loadLanguage(lang).subscribe({
            error: (err) => {
                console.error(`Failed to load language: ${lang}`, err);
                // Fallback to English translations if HTTP fails
                if (lang !== 'en') {
                    this.translations.set(this.fallbackTranslations);
                }
            }
        });
    }

    getCurrentLanguage(): string {
        return this.currentLang();
    }

    translate(key: string, params?: { [key: string]: string }): string {
        const keys = key.split('.');
        let value: any = this.translations();

        // Try to get translation from current translations
        for (const k of keys) {
            if (value && typeof value === 'object' && k in value) {
                value = value[k];
            } else {
                // If not found, try fallback translations
                value = this.getFallbackTranslation(key);
                break;
            }
        }

        if (typeof value !== 'string') {
            // Last resort: return the key itself
            return key;
        }

        // Replace parameters if provided
        if (params) {
            Object.keys(params).forEach(paramKey => {
                value = value.replace(`{{${paramKey}}}`, params[paramKey]);
            });
        }

        return value;
    }

    private getFallbackTranslation(key: string): string {
        const keys = key.split('.');
        let value: any = this.fallbackTranslations;

        for (const k of keys) {
            if (value && typeof value === 'object' && k in value) {
                value = value[k];
            } else {
                return key; // Return key if not found in fallback either
            }
        }

        return typeof value === 'string' ? value : key;
    }

    // Shorthand method
    t(key: string, params?: { [key: string]: string }): string {
        return this.translate(key, params);
    }

    // Observable for components to subscribe to language changes
    get translationsLoaded$(): Observable<boolean> {
        return this.translationsLoaded.asObservable();
    }

    // Signal for reactive access to current language
    get currentLanguage$() {
        return computed(() => this.currentLang());
    }
}
