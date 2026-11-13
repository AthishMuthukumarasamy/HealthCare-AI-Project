
import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
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

interface DashboardStatistic {
  label: string;
  value: string;
  icon: string;
  tone: string;
  description: string;
}

interface DistributionItem {
  label: string;
  value: number;
  percentage: number;
  tone: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private readonly dashboardService = inject(DashboardService);

  protected readonly today = new Date();

  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal('');

  protected readonly isAppointmentsLoading = signal(false);
  protected readonly appointmentsError = signal('');

  protected readonly currentPage = signal(1);
  protected readonly pageSize = 5;
  protected readonly totalPages = signal(0);
  protected readonly totalAppointmentRecords = signal(0);

  protected readonly statistics = signal<DashboardStatistic[]>([
    {
      label: 'Total Patients',
      value: '—',
      icon: '♙',
      tone: 'blue',
      description: 'Registered patients'
    },
    {
      label: 'Healthcare Providers',
      value: '—',
      icon: '⚕',
      tone: 'green',
      description: 'Available professionals'
    },
    {
      label: 'Appointments',
      value: '—',
      icon: '▣',
      tone: 'purple',
      description: 'Total scheduled visits'
    },
    {
      label: 'Medical Records',
      value: '—',
      icon: '▤',
      tone: 'orange',
      description: 'Stored medical records'
    }
  ]);

  protected readonly distribution = signal<DistributionItem[]>([]);

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
        const patientCount = response.patients.totalRecords;
        const providerCount = response.providers.totalRecords;
        const appointmentCount = response.appointments.totalRecords;
        const medicalRecordCount = response.medicalRecords.totalRecords;

        this.statistics.set([
          {
            label: 'Total Patients',
            value: patientCount.toLocaleString(),
            icon: '♙',
            tone: 'blue',
            description: 'Registered patients'
          },
          {
            label: 'Healthcare Providers',
            value: providerCount.toLocaleString(),
            icon: '⚕',
            tone: 'green',
            description: 'Available professionals'
          },
          {
            label: 'Appointments',
            value: appointmentCount.toLocaleString(),
            icon: '▣',
            tone: 'purple',
            description: 'Total scheduled visits'
          },
          {
            label: 'Medical Records',
            value: medicalRecordCount.toLocaleString(),
            icon: '▤',
            tone: 'orange',
            description: 'Stored medical records'
          }
        ]);

        const total =
          patientCount +
          providerCount +
          appointmentCount +
          medicalRecordCount;

        this.distribution.set([
          {
            label: 'Patients',
            value: patientCount,
            percentage: total > 0 ? (patientCount / total) * 100 : 0,
            tone: 'blue'
          },
          {
            label: 'Providers',
            value: providerCount,
            percentage: total > 0 ? (providerCount / total) * 100 : 0,
            tone: 'green'
          },
          {
            label: 'Appointments',
            value: appointmentCount,
            percentage: total > 0 ? (appointmentCount / total) * 100 : 0,
            tone: 'purple'
          },
          {
            label: 'Medical Records',
            value: medicalRecordCount,
            percentage: total > 0 ? (medicalRecordCount / total) * 100 : 0,
            tone: 'orange'
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
