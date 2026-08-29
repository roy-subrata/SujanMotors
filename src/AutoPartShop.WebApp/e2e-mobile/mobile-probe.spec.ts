import { test, Page } from '@playwright/test';

const ADMIN_CREDENTIALS = { username: 'admin', password: 'Admin@1990' };

async function login(page: Page) {
    await page.goto('/login');
    await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
    await page.getByPlaceholder('••••••••').fill(ADMIN_CREDENTIALS.password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.waitForURL('http://localhost:4200/**');
    await page.waitForTimeout(1500);
}

test('probe audit-dashboard header, hr-advances buttons, admin-settings tabs', async ({ page }) => {
    await login(page);

    await page.goto('/audit/dashboard');
    await page.waitForTimeout(2200);
    const audit = await page.evaluate(() => {
        const hc = document.querySelector('.page-header .header-content');
        const ha = document.querySelector('.page-header .header-actions');
        const widest: any[] = [];
        if (hc) {
            for (const el of Array.from(hc.querySelectorAll('*'))) {
                const r = (el as HTMLElement).getBoundingClientRect();
                if (r.right < 0 || r.left > 5000) widest.push({
                    cls: String(el.className || '').slice(0, 60),
                    left: r.left, right: r.right, w: r.width,
                    scrollW: (el as HTMLElement).scrollWidth,
                    tag: el.tagName.toLowerCase(),
                });
            }
        }
        return {
            haChildren: ha ? Array.from(ha.children).map((el) => {
                const r = (el as HTMLElement).getBoundingClientRect();
                return { cls: String(el.className || '').slice(0, 60), tag: el.tagName.toLowerCase(), left: r.left, right: r.right, w: r.width, scrollW: (el as HTMLElement).scrollWidth };
            }) : null,
            panels: document.querySelectorAll('.p-datepicker-panel').length,
            widestBeyond300: widest.slice(0, 8),
        };
    });
    console.log('audit:', JSON.stringify(audit, null, 2));

    await page.goto('/hr/advances');
    await page.waitForTimeout(2200);
    const adv = await page.evaluate(() => {
        const btn = document.querySelector('.btn-card-action') as HTMLElement | null;
        return btn ? {
            computed: { width: getComputedStyle(btn).width, minW: getComputedStyle(btn).minWidth, padding: getComputedStyle(btn).padding, flex: getComputedStyle(btn).flexWrap },
            clientW: btn.clientWidth, scrollW: btn.scrollWidth,
            children: Array.from(btn.children).map((c) => {
                const r = (c as HTMLElement).getBoundingClientRect();
                return { cls: String(c.className || ''), tag: c.tagName.toLowerCase(), left: r.left, w: r.width, text: c.textContent?.slice(0, 20) };
            }),
        } : null;
    });
    console.log('advances btn:', JSON.stringify(adv, null, 2));

    await page.goto('/admin-settings');
    await page.waitForTimeout(2200);
    const tabs = await page.evaluate(() => {
        const tl = document.querySelector('.p-tablist-tab-list') as HTMLElement | null;
        return tl ? {
            clientW: tl.clientWidth, scrollW: tl.scrollWidth, overflowX: getComputedStyle(tl).overflowX, display: getComputedStyle(tl).display,
            flexWrap: getComputedStyle(tl).flexWrap, whiteSpace: getComputedStyle(tl).whiteSpace,
            tabs: Array.from(tl.children).map((t) => { const r = (t as HTMLElement).getBoundingClientRect(); return { cls: String(t.className || '').slice(0, 40), text: t.textContent?.slice(0, 20), left: r.left, w: r.width }; }),
        } : null;
    });
    console.log('admin tabs:', JSON.stringify(tabs, null, 2));
});