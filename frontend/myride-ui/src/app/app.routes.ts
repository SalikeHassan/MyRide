import { Routes } from '@angular/router';
import { RideComponent } from './pages/ride/ride.component';

export const routes: Routes = [
  { path: '', redirectTo: 'ride', pathMatch: 'full' },
  { path: 'ride', component: RideComponent }
];
