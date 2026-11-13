
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import {
  Appointment,
  Dashboard as DashboardService
} from '../../core/services/dashboard';

interface DashboardAppointment {
  patient: string;
  provider: string;
  date: string;
  time: string;
  type: string;
  status: string;
}

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

  protected readonly isAppointmentsLoading = signal(false);
  protected readonly appointmentsError = signal('');

  protected readonly currentPage = signal(1);
  protected readonly pageSize = 5;
  protected readonly totalPages = signal(0);
  protected readonly totalAppointmentRecords = signal(0);

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

  protected readonly appointments = signal<DashboardAppointment[]>([]);

  constructor() {
    this.loadDashboardStatistics();
    this.loadAppointmentsPage(1);
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

  protected loadAppointmentsPage(page: number): void {
    if (
      this.isAppointmentsLoading() ||
      page < 1 ||
      (this.totalPages() > 0 && page > this.totalPages())
    ) {
      return;
    }

    this.isAppointmentsLoading.set(true);
    this.appointmentsError.set('');

    this.dashboardService
      .getRecentAppointments(page, this.pageSize)
      .subscribe({
        next: response => {
          const sortedAppointments = [...response.data].sort(
            (first, second) =>
              new Date(first.appointmentDate).getTime() -
              new Date(second.appointmentDate).getTime()
          );

          this.appointments.set(
            sortedAppointments.map(appointment =>
              this.formatAppointment(appointment)
            )
          );

          this.currentPage.set(response.pageNumber);
          this.totalPages.set(response.totalPages);
          this.totalAppointmentRecords.set(response.totalRecords);
          this.isAppointmentsLoading.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.isAppointmentsLoading.set(false);

          if (error.status === 401 || error.status === 403) {
            this.appointmentsError.set(
              'You do not have permission to load appointments.'
            );
          } else {
            this.appointmentsError.set(
              'Unable to load appointments. Please try again.'
            );
          }
        }
      });
  }

  protected goToPreviousPage(): void {
    if (this.currentPage() > 1) {
      this.loadAppointmentsPage(this.currentPage() - 1);
    }
  }

  protected goToNextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.loadAppointmentsPage(this.currentPage() + 1);
    }
  }

  private formatAppointment(
    appointment: Appointment
  ): DashboardAppointment {
    const appointmentDate = new Date(appointment.appointmentDate);

    return {
      patient: appointment.patientName,
      provider: `Dr. ${appointment.providerName}`,
      date: appointmentDate.toLocaleDateString('en-IN', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
      }),
      time: appointmentDate.toLocaleTimeString('en-IN', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: true
      }),
      type: appointment.reason,
      status: appointment.status
    };
  }
}
