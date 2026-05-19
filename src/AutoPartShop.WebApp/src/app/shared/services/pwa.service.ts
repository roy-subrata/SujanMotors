import { Injectable, Optional } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { BehaviorSubject, filter } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class PwaService {
    private swUpdate: SwUpdate | null = null;

    private _isOnline = new BehaviorSubject<boolean>(navigator.onLine);
    private _updateAvailable = new BehaviorSubject<boolean>(false);

    isOnline$ = this._isOnline.asObservable();
    updateAvailable$ = this._updateAvailable.asObservable();

    constructor(@Optional() swUpdate: SwUpdate) {
        this.swUpdate = swUpdate;
        this.initOnlineStatus();
        this.initUpdateListener();
        this.initInstallPrompt();
    }

    private get isSwEnabled(): boolean {
        return this.swUpdate?.isEnabled ?? false;
    }

    private initOnlineStatus(): void {
        window.addEventListener('online', () => this._isOnline.next(true));
        window.addEventListener('offline', () => this._isOnline.next(false));
    }

    private initUpdateListener(): void {
        if (!this.isSwEnabled || !this.swUpdate) {
            console.log('Service Worker is not enabled');
            return;
        }

        // Listen for version updates
        this.swUpdate.versionUpdates
            .pipe(filter((evt): evt is VersionReadyEvent => evt.type === 'VERSION_READY'))
            .subscribe(() => {
                console.log('New version available');
                this._updateAvailable.next(true);
            });

        // Check for updates periodically (every 6 hours)
        setInterval(() => {
            this.checkForUpdate();
        }, 6 * 60 * 60 * 1000);
    }

    async checkForUpdate(): Promise<boolean> {
        if (!this.isSwEnabled || !this.swUpdate) {
            return false;
        }

        try {
            const updateFound = await this.swUpdate.checkForUpdate();
            console.log(updateFound ? 'Update found' : 'No update found');
            return updateFound;
        } catch (err) {
            console.error('Error checking for update:', err);
            return false;
        }
    }

    async updateApp(): Promise<void> {
        if (!this.isSwEnabled || !this.swUpdate) {
            return;
        }

        try {
            await this.swUpdate.activateUpdate();
            window.location.reload();
        } catch (err) {
            console.error('Error activating update:', err);
        }
    }

    // Check if the app can be installed (PWA prompt)
    private deferredPrompt: any;
    canInstall$ = new BehaviorSubject<boolean>(false);

    initInstallPrompt(): void {
        window.addEventListener('beforeinstallprompt', (e: Event) => {
            e.preventDefault();
            this.deferredPrompt = e;
            this.canInstall$.next(true);
        });

        window.addEventListener('appinstalled', () => {
            this.deferredPrompt = null;
            this.canInstall$.next(false);
            console.log('PWA was installed');
        });
    }

    async promptInstall(): Promise<boolean> {
        if (!this.deferredPrompt) {
            return false;
        }

        this.deferredPrompt.prompt();
        const { outcome } = await this.deferredPrompt.userChoice;
        this.deferredPrompt = null;
        this.canInstall$.next(false);

        return outcome === 'accepted';
    }

    // Check if running as installed PWA
    isStandalone(): boolean {
        return (
            window.matchMedia('(display-mode: standalone)').matches ||
            (window.navigator as any).standalone === true
        );
    }
}
