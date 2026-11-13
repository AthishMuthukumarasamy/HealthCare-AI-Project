
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { Dashboard as DashboardService } from '../../core/services/dashboard';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);

  protected readonly title = signal('HealthConnect AI');
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal('');

  protected readonly navigationItems = [
    { label: 'Dashboard', icon: '▦', route: '/dashboard' },
    { label: 'Patients', icon: '♙', route: '/patients' },
    { label: 'Providers', icon: '⚕', route: '/providers' },
    { label: 'Appointments', icon: '▣', route: '/appointments' },
    { label: 'Medical Records', icon: '▤', route: '/medical-records' }
  ];

  protected readonly statistics = signal([
    { label: 'Total Patients', value: '—', change: '', icon: '♙' },
    { label: 'Total Providers', value: '—', change: '', icon: '⚕' },
    { label: 'Appointments', value: '—', change: '', icon: '▣' },
    { label: 'Medical Records', value: '—', change: '', icon: '▤' }
  ]);

  protected readonly appointments = [
    {
      patient: 'Olivia Anderson',
      provider: 'Dr. Rahul Kumar',
      date: 'Nov 13, 2026',
      time: '09:30 AM',
      type: 'General Checkup',
      status: 'Confirmed'
    },
    {
      patient: 'James Wilson',
      provider: 'Dr. Sarah Mitchell',
      date: 'Nov 13, 2026',
      time: '10:15 AM',
      type: 'Cardiology',
      status: 'Scheduled'
    },
    {
      patient: 'Sophia Martinez',
      provider: 'Dr. David Chen',
      date: 'Nov 13, 2026',
      time: '11:00 AM',
      type: 'Dermatology',
      status: 'Pending'
    },
    {
      patient: 'Noah Thompson',
      provider: 'Dr. Emily Brown',
      date: 'Nov 13, 2026',
      time: '01:30 PM',
      type: 'General Checkup',
      status: 'Confirmed'
    }
  ];

  constructor() {
    this.loadDashboardStatistics();
  }

  private loadDashboardStatistics(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    forkJoin({
      patients: this.dashboardService.getPatientCount(),
      providers: this.dashboardService.getProviderCount(),
      appointments: this.dashboardService.getAppointmentCount(),
      medicalRecords: this.dashboardService.getMedicalRecordCount()
    }).subscribe({
      next: response => {
        this.statistics.set([
          {
            label: 'Total Patients',
            value: response.patients.totalRecords.toLocaleString(),
            change: '',
            icon: '♙'
          },
          {
            label: 'Total Providers',
            value: response.providers.totalRecords.toLocaleString(),
            change: '',
            icon: '⚕'
          },
          {
            label: 'Appointments',
            value: response.appointments.totalRecords.toLocaleString(),
            change: '',
            icon: '▣'
          },
          {
            label: 'Medical Records',
            value: response.medicalRecords.totalRecords.toLocaleString(),
            change: '',
            icon: '▤'
          }
        ]);

        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        if (error.status === 401 || error.status === 403) {
          this.errorMessage.set(
            'You do not have permission to load dashboard statistics.'
          );
        } else {
          this.errorMessage.set(
            'Unable to load dashboard statistics. Please try again.'
          );
        }
      }
    });
  }
}
