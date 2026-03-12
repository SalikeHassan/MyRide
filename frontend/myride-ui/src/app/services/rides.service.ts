import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StartRideRequest {
  riderId: string;
  driverId: string;
  fareAmount: number;
  fareCurrency: string;
  pickupLat: number;
  pickupLng: number;
  dropoffLat: number;
  dropoffLng: number;
}

export interface RideResponse {
  rideId: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class RidesService {
  private readonly baseUrl = `${environment.apiUrl}/api/rides`;

  constructor(private http: HttpClient) {}

  private headers(tenantId: string): HttpHeaders {
    return new HttpHeaders({ 'X-Tenant-Id': tenantId });
  }

  startRide(request: StartRideRequest, tenantId: string): Observable<RideResponse> {
    return this.http.post<RideResponse>(`${this.baseUrl}/start`, request, { headers: this.headers(tenantId) });
  }

  acceptRide(rideId: string, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${rideId}/accept`, {}, { headers: this.headers(tenantId) });
  }

  completeRide(rideId: string, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${rideId}/complete`, {}, { headers: this.headers(tenantId) });
  }

  cancelRide(rideId: string, reason: string, tenantId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${rideId}/cancel`, { reason }, { headers: this.headers(tenantId) });
  }
}
