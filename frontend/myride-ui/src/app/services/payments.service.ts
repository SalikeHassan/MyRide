import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChargeRiderRequest {
  rideId: string;
  payerId: string;
  payeeId: string;
  amount: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/payments`;

  constructor(private http: HttpClient) {}

  private headers(tenantId: string): HttpHeaders {
    return new HttpHeaders({ 'X-Tenant-Id': tenantId });
  }

  chargeRider(request: ChargeRiderRequest, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/charge`, request, { headers: this.headers(tenantId) });
  }

  refundRider(paymentId: string, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${paymentId}/refund`, {}, { headers: this.headers(tenantId) });
  }
}
