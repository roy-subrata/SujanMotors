import { test, Page } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const ADMIN_CREDENTIALS = { username: 'admin', password: 'Admin@1990' };

const OUT_DIR = path.join(__dirname, 'results');
const JSON_REPORT = path.join(OUT_DIR, 'overflow-report.json');

/*
 * Routes to QA at phone width. Selected to cover the worst offenders:
 * dense data-tables, two-column forms, wizard/grid layouts and the mobile
 * card-view reports.
 */
const ROUTES: { path: string; name: string; table?: boolean }[] = [
    // Shell + core
    { path: '/login', name: 'login' },
    { path: '/', name: 'dashboard' },
    { path: '/shortcuts', name: 'shortcuts' },
    { path: '/notifications', name: 'notifications' },

    // Inventory
    { path: '/inventory/parts', name: 'parts-list', table: true },
    { path: '/inventory/parts/create', name: 'part-form' },
    { path: '/inventory/suppliers', name: 'suppliers-list', table: true },
    { path: '/inventory/warehouses', name: 'warehouses-list' },
    { path: '/inventory/vehicles', name: 'vehicles-list', table: true },
    { path: '/inventory/stock', name: 'stock-list', table: true },
    { path: '/inventory/stock-takes', name: 'stock-takes-list', table: true },
    { path: '/inventory/categories', name: 'categories-list', table: true },
    { path: '/inventory/brands', name: 'brands-list', table: true },
    { path: '/inventory/units', name: 'units-list', table: true },
    { path: '/inventory/discounts', name: 'discounts-list' },
    { path: '/inventory/attribute-groups', name: 'attribute-groups', table: true },

    // Procurement
    { path: '/procurement/purchase-orders', name: 'po-list', table: true },
    { path: '/procurement/purchase-orders/create', name: 'po-form' },
    { path: '/procurement/goods-receipts', name: 'goods-receipts', table: true },
    { path: '/procurement/goods-receipts/create', name: 'goods-receipt-form' },
    { path: '/procurement/purchase-returns', name: 'pr-list', table: true },
    { path: '/procurement/supplier-payments', name: 'supplier-payments', table: true },
    { path: '/procurement/payment-providers', name: 'payment-providers', table: true },
    { path: '/procurement/daily-expenses', name: 'daily-expenses', table: true },

    // Sales
    { path: '/sales/customers', name: 'customers-list', table: true },
    { path: '/sales/sales-orders', name: 'so-list', table: true },
    { path: '/sales/sales-orders/create', name: 'so-form' },
    { path: '/sales/invoices', name: 'invoices-list', table: true },
    { path: '/sales/sales-returns', name: 'sales-returns-list', table: true },
    { path: '/sales/quotations', name: 'quotations-list', table: true },
    { path: '/sales/proforma-invoices', name: 'proforma-list', table: true },
    { path: '/sales/debit-notes', name: 'debit-notes-list', table: true },
    { path: '/sales/pending-deliveries', name: 'pending-deliveries', table: true },
    { path: '/sales/customer-payments', name: 'customer-payments', table: true },
    { path: '/sales/technicians', name: 'technicians-list', table: true },
    { path: '/sales/customer-account-summary', name: 'customer-account-summary' },
    { path: '/sales/till-sessions', name: 'till-sessions' },
    { path: '/sales/till-sessions/history', name: 'till-sessions-history', table: true },

    // Warranty
    { path: '/warranty/registrations', name: 'warranty-regs', table: true },
    { path: '/warranty/claims', name: 'warranty-claims', table: true },

    // Finance
    { path: '/finance/cash-book', name: 'cash-book', table: true },

    // Reports
    { path: '/reports', name: 'reports-hub' },
    { path: '/reports/sales-summary', name: 'report-page', table: true },
    { path: '/reports/profit-loss', name: 'report-pnl' },
    { path: '/reports/vat', name: 'report-vat' },
    { path: '/reports/daily-z-report', name: 'report-z' },

    // HR
    { path: '/hr/employees', name: 'hr-employees', table: true },
    { path: '/hr/employees/create', name: 'hr-employee-form' },
    { path: '/hr/attendance', name: 'hr-attendance', table: true },
    { path: '/hr/leave-requests', name: 'hr-leave', table: true },
    { path: '/hr/holidays', name: 'hr-holidays', table: true },
    { path: '/hr/shifts', name: 'hr-shifts', table: true },
    { path: '/hr/payroll', name: 'hr-payroll', table: true },
    { path: '/hr/advances', name: 'hr-advances', table: true },

    // Audit + Admin
    { path: '/audit/dashboard', name: 'audit-dashboard', table: true },
    { path: '/audit/logs', name: 'audit-logs', table: true },
    { path: '/admin/company-profile', name: 'admin-profile' },
    { path: '/admin/currencies', name: 'admin-currencies', table: true },
    { path: '/admin/exchange-rates', name: 'admin-exchange', table: true },
    { path: '/admin/backups', name: 'admin-backups' },
    { path: '/admin-settings', name: 'admin-settings' },

    // POS — standalone, no shell
    { path: '/pos', name: 'pos', table: true },
];

function metricsScript() {
    return () => {
        const vw = window.innerWidth;
        const clipped: any[] = [];
        const walk = (el: Element) => {
            if (el.scrollWidth > el.clientWidth + 1 && el.clientWidth < vw) {
                const cs = getComputedStyle(el);
                // Only flag content that visibly spills outside its own box.
                // hidden/clip are intentional truncation (ellipsis) or input text
                // scroll; auto/scroll are scrollable (fine).
                if (cs.overflowX === 'visible') {
                    clipped.push({
                        tag: el.tagName.toLowerCase(),
                        cls: String(el.className || '').slice(0, 100),
                        scrollW: el.scrollWidth,
                        clientW: el.clientWidth,
                    });
                }
            }
            for (const c of el.children) walk(c);
        };
        walk(document.body);
        return {
            vw,
            docScrollWidth: document.documentElement.scrollWidth,
            max: clipped.length,
            clipped: clipped.slice(0, 30),
        };
    };
}

async function login(page: Page) {
    await page.goto('/login');
    await page.getByPlaceholder('Enter your username or email').fill(ADMIN_CREDENTIALS.username);
    await page.getByPlaceholder('••••••••').fill(ADMIN_CREDENTIALS.password);
    await page.getByRole('button', { name: 'Sign In' }).click();
    await page.waitForURL('http://localhost:4200/**');
    await page.waitForTimeout(1500);
}

test('mobile QA: overflow + layout audit across key routes', async ({ page }) => {
    test.setTimeout(600_000);
    fs.mkdirSync(OUT_DIR, { recursive: true });
    const report: any[] = [];

    await login(page);

    for (const route of ROUTES) {
        const entry: any = { name: route.name, path: route.path };
        try {
            await page.goto(route.path);
            // give lazy chunk + data table time to render
            await page.waitForTimeout(2500);

            entry.metrics = await page.evaluate(metricsScript());
            entry.hasTable = (await page.locator('.p-datatable').count()) > 0;
            entry.hasHorizontalScroll = entry
                ? entry.metrics
                    ? await page.evaluate(() => {
                          const el = document.querySelector('.layout-main-container');
                          return !!el && el.scrollWidth > el.clientWidth + 1;
                      })
                    : false
                : false;

            await page.screenshot({ path: path.join(OUT_DIR, `${route.name}.png`), fullPage: true });
        } catch (err: any) {
            entry.error = String(err?.message || err).slice(0, 300);
            await page.screenshot({ path: path.join(OUT_DIR, `${route.name}-error.png`), fullPage: true }).catch(() => {});
        }
        report.push(entry);
        console.log(
            `\n=== ${route.name} (${route.path}) === vw=${entry.metrics?.vw}\n` +
                `clipped=${entry.metrics?.max} docScrollW=${entry.metrics?.docScrollWidth}` +
                (entry.hasTable ? ' table=yes' : '') +
                (entry.error ? ` ERROR: ${entry.error}` : '')
        );
        for (const o of entry.metrics?.clipped ?? []) {
            console.log(`   <${o.tag} class="${o.cls}"> scrollW=${o.scrollW} clientW=${o.clientW}`);
        }
    }

    fs.writeFileSync(JSON_REPORT, JSON.stringify(report, null, 2));
});

test('mobile QA: hamburger menu opens and sidebar navigates', async ({ page }) => {
    await login(page);
    await page.goto('/');
    await page.waitForTimeout(1200);

    const menuBtn = page.locator('.layout-menu-button').first();
    if ((await menuBtn.count()) === 0) {
        console.log('NO MENU BUTTON FOUND');
        return;
    }
    await menuBtn.click();
    await page.waitForTimeout(800);
    await page.screenshot({ path: path.join(OUT_DIR, '01-menu-open.png') });

    const menuItems = page.locator('.layout-sidebar .p-menuitem-link').count();
    console.log(`menu items visible: ${menuItems}`);
    const sidebarBox = await page.locator('.layout-sidebar').boundingBox();
    console.log('sidebar bounding box:', JSON.stringify(sidebarBox));
});