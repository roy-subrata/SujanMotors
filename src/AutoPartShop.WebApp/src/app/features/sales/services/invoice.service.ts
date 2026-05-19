import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface CreateInvoiceRequest {
  salesOrderId: string;
  subTotal: number;
  taxAmount: number;
  dueDate: string;
  notes: string;
}

export interface InvoicePaymentResponse {
  id: string;
  invoiceId: string;
  amount: number;
  paymentDate: string;
  paymentMethod: string;
  referenceNumber: string;
}

export interface InvoiceResponse {
  id: string;
  invoiceNumber: string;
  salesOrderId: string;
  salesOrderNumber: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  invoiceDate: string;
  dueDate: string;
  subTotal: number;
  taxAmount: number;
  discountAmount: number;
  grandTotal: number;
  amountPaid: number;
  outstandingAmount: number;
  status: string;
  isOverdue: boolean;
  notes: string;
  payments: InvoicePaymentResponse[];
  createdAt: string;
}

export interface RecordPaymentRequest {
  amount: number;
  paymentDate?: string;
  paymentMethod?: string;
  referenceNumber?: string;
  paymentProviderId?: string;
}

export interface InvoiceListResponse {
  data: InvoiceResponse[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/salesorder/invoices`;

  getAllInvoices(pageNumber: number = 1, pageSize: number = 10, filter: { searchTerm?: string; status?: string; customerId?: string; fromDate?: string; toDate?: string } = {}): Observable<InvoiceListResponse> {
    let params: any = { pageNumber: pageNumber.toString(), pageSize: pageSize.toString() };
    if (filter.searchTerm) params.searchTerm = filter.searchTerm;
    if (filter.status) params.status = filter.status;
    if (filter.customerId) params.customerId = filter.customerId;
    if (filter.fromDate) params.fromDate = filter.fromDate;
    if (filter.toDate) params.toDate = filter.toDate;

    return this.http.get<InvoiceListResponse>(this.apiUrl, { params });
  }

  createInvoice(request: CreateInvoiceRequest): Observable<InvoiceResponse> {
    return this.http.post<InvoiceResponse>(this.apiUrl, request);
  }

  getInvoiceById(id: string): Observable<InvoiceResponse> {
    return this.http.get<InvoiceResponse>(`${this.apiUrl}/${id}`);
  }

  getInvoiceByNumber(invoiceNumber: string): Observable<InvoiceResponse> {
    return this.http.get<InvoiceResponse>(`${this.apiUrl}/number/${invoiceNumber}`);
  }

  getInvoicesByCustomer(customerId: string): Observable<InvoiceResponse[]> {
    return this.http.get<InvoiceResponse[]>(`${this.apiUrl}/customer/${customerId}`);
  }

  issueInvoice(id: string): Observable<InvoiceResponse> {
    return this.http.patch<InvoiceResponse>(`${this.apiUrl}/${id}/issue`, {});
  }

  recordPayment(id: string, request: RecordPaymentRequest): Observable<InvoiceResponse> {
    return this.http.patch<InvoiceResponse>(`${this.apiUrl}/${id}/payment`, request);
  }

  getPrintData(id: string): Observable<InvoicePrintData> {
    return this.http.get<{ data: InvoicePrintData }>(`${this.apiUrl}/${id}/print-data`)
      .pipe(map(r => r.data));
  }
}

export interface InvoicePrintLineItem {
  partId: string;
  partName: string;
  partSku: string;
  displayName: string;
  variantName?: string | null;
  unitName: string;
  unitSymbol: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  lineTotal: number;
}

export interface InvoicePrintData {
  shop: { name: string; address: string; phone: string; email: string; taxNo: string; };
  invoice: InvoiceResponse;
  lines: InvoicePrintLineItem[];
  customer: { name: string; phone: string; email: string; address: string; };
  salesOrderNumber: string;
}
