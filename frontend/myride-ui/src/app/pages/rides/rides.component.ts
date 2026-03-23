import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RidesService } from '../../services/rides.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-rides',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rides.component.html',
  styleUrl: './rides.component.scss'
})
export class RidesComponent {
  tenantId = environment.defaultTenantId;
  activeRideId: string | null = null;
  loading = false;
  logs: { type: 'success' | 'error' | 'info'; message: string }[] = [];

  form = {
    riderId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    driverId: '4fa85f64-5717-4562-b3fc-2c963f66afa7',
    fareAmount: 25.50,
    fareCurrency: 'GBP',
    pickupLat: 51.5074,
    pickupLng: -0.1278,
    dropoffLat: 51.5155,
    dropoffLng: -0.0922
  };

  cancelReason = 'Rider requested cancellation';

  constructor(private ridesService: RidesService) {}

  requestRide(): void {
    this.loading = true;
    this.ridesService.requestRide(this.form, this.tenantId).subscribe({
      next: (res) => {
        this.activeRideId = res.rideId;
        this.log('success', `Ride started. ID: ${res.rideId}`);
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Start failed: ${err.error?.message || err.message}`);
        this.loading = false;
      }
    });
  }

  completeRide(): void {
    if (!this.activeRideId) { return; }
    this.loading = true;
    this.ridesService.completeRide(this.activeRideId, this.tenantId).subscribe({
      next: () => {
        this.log('success', `Ride ${this.activeRideId} completed.`);
        this.activeRideId = null;
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Complete failed: ${err.error?.message || err.message}`);
        this.loading = false;
      }
    });
  }

  cancelRide(): void {
    if (!this.activeRideId) { return; }
    this.loading = true;
    this.ridesService.cancelRide(this.activeRideId, this.cancelReason, this.tenantId).subscribe({
      next: () => {
        this.log('success', `Ride ${this.activeRideId} cancelled.`);
        this.activeRideId = null;
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Cancel failed: ${err.error?.message || err.message}`);
        this.loading = false;
      }
    });
  }

  private log(type: 'success' | 'error' | 'info', message: string): void {
    this.logs.unshift({ type, message });
  }

  clearLogs(): void {
    this.logs = [];
  }
}
