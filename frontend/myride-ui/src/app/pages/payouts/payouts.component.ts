import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PayoutsService } from '../../services/payouts.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-payouts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payouts.component.html',
  styleUrl: './payouts.component.scss'
})
export class PayoutsComponent {
  tenantId = environment.defaultTenantId;
  loading = false;
  logs: { type: 'success' | 'error' | 'info'; message: string }[] = [];

  payForm = {
    recipientId: '4fa85f64-5717-4562-b3fc-2c963f66afa7',
    amount: 20.00,
    currency: 'GBP',
    simulateFailure: false
  };

  cancelPayoutId = '';
  cancelReason = 'Payout cancelled by system';

  constructor(private payoutsService: PayoutsService) {}

  payDriver(): void {
    this.loading = true;
    this.payoutsService.payDriver(this.payForm, this.tenantId).subscribe({
      next: () => {
        this.log('success', 'Driver paid successfully.');
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Pay failed: ${err.error?.message || err.message}`);
        this.loading = false;
      }
    });
  }

  cancelPayout(): void {
    if (!this.cancelPayoutId) { return; }
    this.loading = true;
    this.payoutsService.cancelPayout(this.cancelPayoutId, this.cancelReason, this.tenantId).subscribe({
      next: () => {
        this.log('success', `Payout ${this.cancelPayoutId} cancelled.`);
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
