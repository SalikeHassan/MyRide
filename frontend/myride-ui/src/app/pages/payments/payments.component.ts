import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentsService } from '../../services/payments.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payments.component.html',
  styleUrl: './payments.component.scss'
})
export class PaymentsComponent {
  tenantId = environment.defaultTenantId;
  loading = false;
  logs: { type: 'success' | 'error' | 'info'; message: string }[] = [];

  chargeForm = {
    payerId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    payeeId: '4fa85f64-5717-4562-b3fc-2c963f66afa7',
    amount: 25.50,
    currency: 'GBP',
    simulateFailure: false
  };

  refundPaymentId = '';

  constructor(private paymentsService: PaymentsService) {}

  chargeRider(): void {
    this.loading = true;
    this.paymentsService.chargeRider(this.chargeForm, this.tenantId).subscribe({
      next: () => {
        this.log('success', `Rider charged successfully.`);
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Charge failed: ${err.error?.message || err.message}`);
        this.loading = false;
      }
    });
  }

  refundRider(): void {
    if (!this.refundPaymentId) { return; }
    this.loading = true;
    this.paymentsService.refundRider(this.refundPaymentId, this.tenantId).subscribe({
      next: () => {
        this.log('success', `Rider refunded for payment ${this.refundPaymentId}.`);
        this.loading = false;
      },
      error: (err) => {
        this.log('error', `Refund failed: ${err.error?.message || err.message}`);
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
