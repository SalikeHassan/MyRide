import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActiveRide, RidesService } from '../../services/rides.service';
import { PaymentsService } from '../../services/payments.service';
import { PayoutsService } from '../../services/payouts.service';
import { environment } from '../../../environments/environment';

export type RideStatus = 'idle' | 'requested' | 'inprogress' | 'completed' | 'cancelled';

export interface TimelineEvent {
  label: string;
  status: 'success' | 'error' | 'pending' | 'compensating';
  streamId?: string;
}

@Component({
  selector: 'app-ride',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ride.component.html',
  styleUrl: './ride.component.scss'
})
export class RideComponent implements OnInit {
  tenantId = environment.defaultTenantId;
  rideStatus: RideStatus = 'idle';
  loading = false;

  rideId: string | null = null;
  riderId: string | null = null;
  driverId: string | null = null;
  driverName: string | null = null;
  paymentId: string | null = null;

  fareAmount = 25.50;
  fareCurrency = 'GBP';
  timeline: TimelineEvent[] = [];

  activeRides: ActiveRide[] = [];
  loadingRides = false;
  loadingRideIds = new Set<string>();

  constructor(
    private ridesService: RidesService,
    private paymentsService: PaymentsService,
    private payoutsService: PayoutsService
  ) {}

  ngOnInit(): void {
    this.loadActiveRides();
  }

  loadActiveRides(): void {
    this.loadingRides = true;
    this.ridesService.getActiveRides(this.tenantId).subscribe({
      next: (rides) => {
        this.activeRides = rides;
        this.loadingRides = false;
      },
      error: () => {
        this.loadingRides = false;
      }
    });
  }

  isRideLoading(rideId: string): boolean {
    return this.loadingRideIds.has(rideId);
  }

  // ── New ride request flow ──────────────────────────────────────

  requestRide(): void {
    this.loading = true;
    this.timeline = [];
    this.rideId = null;
    this.driverId = null;
    this.driverName = null;

    this.ridesService.requestRide({
      fareAmount: this.fareAmount,
      fareCurrency: this.fareCurrency,
      pickupLat: 51.5074,
      pickupLng: -0.1278,
      dropoffLat: 51.5155,
      dropoffLng: -0.0922
    }, this.tenantId).subscribe({
      next: (res) => {
        this.rideId = res.rideId;
        this.riderId = res.riderId;
        this.driverId = res.driverId;
        this.driverName = res.driverName;
        this.rideStatus = 'requested';
        this.addTimeline(
          `Ride Requested — Assigned to ${res.driverName}`,
          'success',
          `${this.tenantId}-ride-${res.rideId}`
        );
        this.loading = false;
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Request Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  acceptRide(): void {
    if (!this.rideId) { return; }
    this.loading = true;

    this.ridesService.acceptRide(this.rideId, this.tenantId).subscribe({
      next: () => {
        this.rideStatus = 'inprogress';
        this.addTimeline(
          'Ride Accepted by Driver — In Progress',
          'success',
          `${this.tenantId}-ride-${this.rideId}`
        );
        this.loading = false;
        this.loadActiveRides();
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
        this.addTimeline(
          'Ride Completed',
          'success',
          `${this.tenantId}-ride-${this.rideId}`
        );
        this.loading = false;
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Complete Failed: ${err.error?.message || err.message}`, 'error');
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
        this.addTimeline(
          'Ride Cancelled',
          'error',
          `${this.tenantId}-ride-${this.rideId}`
        );
        this.loading = false;
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Cancel Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  completePayment(): void {
    if (!this.rideId || !this.riderId || !this.driverId) { return; }
    this.loading = true;

    this.paymentsService.chargeRider({
      rideId: this.rideId,
      payerId: this.riderId,
      payeeId: this.driverId,
      amount: this.fareAmount,
      currency: this.fareCurrency
    }, this.tenantId).subscribe({
      next: (res) => {
        this.paymentId = res.paymentId;
        this.addTimeline(
          `Rider Charged ${this.fareCurrency} ${this.fareAmount.toFixed(2)}`,
          'success',
          `${this.tenantId}-payment-${this.rideId}`
        );
        this.processPayoutAfterCharge(this.rideId!, this.driverId!);
      },
      error: (err) => {
        this.addTimeline(`Payment Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
      }
    });
  }

  // ── Actions from the active rides list ────────────────────────

  acceptRideFromList(ride: ActiveRide): void {
    this.loadingRideIds.add(ride.rideId);

    this.ridesService.acceptRide(ride.rideId, this.tenantId).subscribe({
      next: () => {
        this.addTimeline(
          `${ride.driverName} accepted ride`,
          'success',
          `${this.tenantId}-ride-${ride.rideId}`
        );
        this.loadingRideIds.delete(ride.rideId);
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Accept Failed: ${err.error?.message || err.message}`, 'error');
        this.loadingRideIds.delete(ride.rideId);
      }
    });
  }

  completeRideFromList(ride: ActiveRide): void {
    this.loadingRideIds.add(ride.rideId);

    this.ridesService.completeRide(ride.rideId, this.tenantId).subscribe({
      next: () => {
        this.addTimeline(
          `Ride completed — ${ride.driverName}`,
          'success',
          `${this.tenantId}-ride-${ride.rideId}`
        );
        this.loadingRideIds.delete(ride.rideId);
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Complete Failed: ${err.error?.message || err.message}`, 'error');
        this.loadingRideIds.delete(ride.rideId);
      }
    });
  }

  cancelRideFromList(ride: ActiveRide): void {
    this.loadingRideIds.add(ride.rideId);

    this.ridesService.cancelRide(ride.rideId, 'Cancelled', this.tenantId).subscribe({
      next: () => {
        this.addTimeline(
          `Ride cancelled — ${ride.driverName}`,
          'error',
          `${this.tenantId}-ride-${ride.rideId}`
        );
        this.loadingRideIds.delete(ride.rideId);
        this.loadActiveRides();
      },
      error: (err) => {
        this.addTimeline(`Cancel Failed: ${err.error?.message || err.message}`, 'error');
        this.loadingRideIds.delete(ride.rideId);
      }
    });
  }

  payRideFromList(ride: ActiveRide): void {
    this.loadingRideIds.add(ride.rideId);
    let paymentId: string | null = null;

    this.paymentsService.chargeRider({
      rideId: ride.rideId,
      payerId: ride.riderId,
      payeeId: ride.driverId,
      amount: ride.fareAmount,
      currency: ride.fareCurrency
    }, this.tenantId).subscribe({
      next: (res) => {
        paymentId = res.paymentId;
        this.addTimeline(
          `Rider charged ${ride.fareCurrency} ${ride.fareAmount.toFixed(2)}`,
          'success',
          `${this.tenantId}-payment-${ride.rideId}`
        );
        this.processPayoutAfterCharge(ride.rideId, ride.driverId, paymentId, ride.fareAmount, ride.fareCurrency);
      },
      error: (err) => {
        this.addTimeline(`Payment Failed: ${err.error?.message || err.message}`, 'error');
        this.loadingRideIds.delete(ride.rideId);
      }
    });
  }

  // ── Shared payment helpers ─────────────────────────────────────

  private processPayoutAfterCharge(
    rideId: string,
    driverId: string,
    paymentId?: string | null,
    amount?: number,
    currency?: string
  ): void {
    this.payoutsService.payDriver({
      rideId,
      recipientId: driverId,
      amount: amount ?? this.fareAmount,
      currency: currency ?? this.fareCurrency
    }, this.tenantId).subscribe({
      next: () => {
        this.addTimeline(
          'Driver Paid',
          'success',
          `${this.tenantId}-payout-${rideId}`
        );
        this.loading = false;
        if (rideId !== this.rideId) { this.loadingRideIds.delete(rideId); this.loadActiveRides(); }
      },
      error: () => {
        this.addTimeline('Payout Failed — Refunding Rider', 'compensating');
        this.processRefund(paymentId ?? this.paymentId, rideId);
      }
    });
  }

  private processRefund(paymentId: string | null, rideId?: string): void {
    if (!paymentId) { return; }

    this.paymentsService.refundRider(paymentId, this.tenantId).subscribe({
      next: () => {
        this.addTimeline(
          'Rider Refunded',
          'success',
          `${this.tenantId}-payment-${rideId ?? this.rideId}`
        );
        this.loading = false;
        if (rideId && rideId !== this.rideId) { this.loadingRideIds.delete(rideId); this.loadActiveRides(); }
      },
      error: (err) => {
        this.addTimeline(`Refund Failed: ${err.error?.message || err.message}`, 'error');
        this.loading = false;
        if (rideId && rideId !== this.rideId) { this.loadingRideIds.delete(rideId); }
      }
    });
  }

  resetRide(): void {
    this.rideId = null;
    this.riderId = null;
    this.driverId = null;
    this.driverName = null;
    this.paymentId = null;
    this.rideStatus = 'idle';
    this.timeline = [];
  }

  private addTimeline(label: string, status: TimelineEvent['status'], streamId?: string): void {
    this.timeline.push({ label, status, streamId });
  }

  statusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'requested': return 'Requested';
      case 'inprogress': return 'In Progress';
      case 'completed': return 'Completed';
      case 'cancelled': return 'Cancelled';
      default: return status;
    }
  }
}
