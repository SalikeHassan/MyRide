import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RequestRideRequest {
  fareAmount: number;
  fareCurrency: string;
  pickupLat: number;
  pickupLng: number;
  dropoffLat: number;
  dropoffLng: number;
}

export interface RequestRideResponse {
  rideId: string;
  riderId: string;
  driverId: string;
  driverName: string;
  message: string;
}

export interface ActiveRide {
  rideId: string;
  tenantId: string;
  riderId: string;
  driverId: string;
  driverName: string;
  status: string;
  fareAmount: number;
  fareCurrency: string;
  lastUpdatedOn: string;
}

@Injectable({ providedIn: 'root' })
export class RidesService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/rides`;

  constructor(private http: HttpClient) {}

  private headers(tenantId: string): HttpHeaders {
    return new HttpHeaders({ 'X-Tenant-Id': tenantId });
  }

  requestRide(request: RequestRideRequest, tenantId: string): Observable<RequestRideResponse> {
    return this.http.post<RequestRideResponse>(`${this.baseUrl}/start`, request, { headers: this.headers(tenantId) });
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

  getActiveRides(tenantId: string): Observable<ActiveRide[]> {
    return this.http.get<ActiveRide[]>(`${this.baseUrl}/active`, { headers: this.headers(tenantId) });
  }
}
