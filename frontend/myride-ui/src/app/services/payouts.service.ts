import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PayDriverRequest {
  recipientId: string;
  amount: number;
  currency: string;
  simulateFailure: boolean;
}

@Injectable({ providedIn: 'root' })
export class PayoutsService {
  private readonly baseUrl = `${environment.apiUrl}/api/payouts`;

  constructor(private http: HttpClient) {}

  private headers(tenantId: string): HttpHeaders {
    return new HttpHeaders({ 'X-Tenant-Id': tenantId });
  }

  payDriver(request: PayDriverRequest, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/pay`, request, { headers: this.headers(tenantId) });
  }

  cancelPayout(payoutId: string, reason: string, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${payoutId}/cancel`, { reason }, { headers: this.headers(tenantId) });
  }
}
