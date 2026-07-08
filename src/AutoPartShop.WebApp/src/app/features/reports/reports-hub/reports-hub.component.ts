import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { REPORT_GROUPS, ReportGroupInfo } from '../report-configs';

/**
 * Reports hub: category cards linking to every configured report page,
 * plus pointers to the report-grade pages that live in other modules.
 */
@Component({
    selector: 'app-reports-hub',
    standalone: true,
    imports: [CommonModule, RouterModule, PageContainerComponent, PageHeaderComponent],
    templateUrl: './reports-hub.component.html',
    styleUrls: ['./reports-hub.component.scss']
})
export class ReportsHubComponent {
    readonly groups: ReportGroupInfo[] = REPORT_GROUPS;

    /** Existing report-grade pages elsewhere in the app, surfaced here for discoverability. */
    readonly relatedReports = [
        { title: 'Daily Cash Book', icon: 'pi pi-book', link: '/finance/cash-book' },
        { title: 'Customer Statements', icon: 'pi pi-user', link: '/sales/customer-account-summary' },
        { title: 'Supplier Statements', icon: 'pi pi-truck', link: '/procurement/supplier-account-summary' }
    ];

    get totalReports(): number {
        return this.groups.reduce((n, g) => n + g.reports.length, 0);
    }
}
