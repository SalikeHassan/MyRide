import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RidesService } from '../../services/rides.service';
import { PaymentsService } from '../../services/payments.service';
import { PayoutsService } from '../../services/payouts.service';
import { environment } from '../../../environments/environment';

export type RideStatus = 'idle' | 'requested' | 'started' | 'completed' | 'cancelled';
export type SimulateFailure = 'none' | 'payment' | 'payout';

export interface TimelineEvent {
  label: string;
  status: 'success' | 'error' | 'pending' | 'compensating';
}

@Component({
  selector: 'app-ride',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ride.component.html',
  styleUrl: './ride.component.scss'
})
export class RideComponent {
  tenantId = environment.defaultTenantId;
  activeTab: 'rider' | 'driver' = 'rider';
  rideStatus: RideStatus = 'idle';
  loading = false;

  rideId: string | null = null;
  paymentId: string | null = null;
  fareAmount = 25.50;
  fareCurrency = 'GBP';
  simulateFailure: SimulateFailure = 'none';
  timeline: TimelineEvent[] = [];

  riderId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
  driverId = '4fa85f64-5717-4562-b3fc-2c963f66afa7';

  constructor(
    private ridesService: RidesService,
    private paymentsService: PaymentsService,
    private payoutsService: PayoutsService
  ) {}

  requestRide(): void {
    this.loading = true;
    this.timeline = [];
    this.rideId = null;

    this.ridesService.startRide({
      riderId: this.riderId,
      driverId: this.driverId,
      fareAmount: this.fareAmount,
      fareCurrency: this.fareCurrency,
      pickupLat: 51.5074,
      pickupLng: -0.1278,
      dropoffLat: 51.5155,
      dropoffLng: -0.0922
    }, this.tenantId).subscribe({
      next: (res) => {
        this.rideId = res.rideId;
        this.rideStatus = 'requested';
        this.addTimeline('Ride Requested', 'success');
        this.loading = false;
      },
      error: (err) => {
        this.addTimeline(`Request Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  cancelRide(): void {
    if (!this.rideId) { return; }
    this.loading = true;

    this.ridesService.cancelRide(this.rideId, 'Rider cancelled', this.tenantId).subscribe({
      next: () => {
        this.rideStatus = 'cancelled';
        this.addTimeline('Ride Cancelled by Rider', 'error');
        this.loading = false;
      },
      error: (err) => {
        this.addTimeline(`Cancel Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  startRide(): void {
    if (!this.rideId) { return; }
    this.loading = true;

    this.ridesService.acceptRide(this.rideId, this.tenantId).subscribe({
      next: () => {
        this.rideStatus = 'started';
        this.addTimeline('Ride Accepted by Driver', 'success');
        this.loading = false;
      },
      error: (err) => {
        this.addTimeline(`Accept Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  completeRide(): void {
    if (!this.rideId) { return; }
    this.loading = true;

    this.ridesService.completeRide(this.rideId, this.tenantId).subscribe({
      next: () => {
        this.rideStatus = 'completed';
        this.addTimeline('Ride Completed by Driver', 'success');
        this.loading = false;
      },
      error: (err) => {
        this.addTimeline(`Complete Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  completePayment(): void {
    if (!this.rideId) { return; }
    this.loading = true;

    const simulatePaymentFailure = this.simulateFailure === 'payment';
    const simulatePayoutFailure = this.simulateFailure === 'payout';

    this.paymentsService.chargeRider({
      payerId: this.riderId,
      payeeId: this.driverId,
      amount: this.fareAmount,
      currency: this.fareCurrency,
      simulateFailure: simulatePaymentFailure
    }, this.tenantId).subscribe({
      next: (res) => {
        this.paymentId = res.paymentId;
        this.addTimeline(`Rider Charged £${this.fareAmount}`, 'success');
        this.processPayoutAfterCharge(simulatePayoutFailure);
      },
      error: (err) => {
        this.addTimeline(`Payment Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  private processPayoutAfterCharge(simulatePayoutFailure: boolean): void {
    this.payoutsService.payDriver({
      recipientId: this.driverId,
      amount: this.fareAmount,
      currency: this.fareCurrency,
      simulateFailure: simulatePayoutFailure
    }, this.tenantId).subscribe({
      next: () => {
        this.addTimeline('Driver Paid', 'success');
        this.loading = false;
      },
      error: () => {
        this.addTimeline('Payout Failed — Refunding Rider', 'compensating');
        this.processRefund();
      }
    });
  }

  private processRefund(): void {
    this.paymentsService.refundRider(this.paymentId!, this.tenantId).subscribe({
      next: () => {
        this.addTimeline('Rider Refunded', 'success');
        this.loading = false;
      },
      error: (err) => {
        this.addTimeline(`Refund Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  resetRide(): void {
    this.rideId = null;
    this.paymentId = null;
    this.rideStatus = 'idle';
    this.timeline = [];
    this.simulateFailure = 'none';
  }

  private addTimeline(label: string, status: TimelineEvent['status']): void {
    this.timeline.push({ label, status });
  }
}
