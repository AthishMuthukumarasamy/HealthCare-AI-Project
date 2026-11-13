
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface PaginatedResponse<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  data: T[];
}

export interface Appointment {
  id: number;
  patientId: number;
  patientName: string;
  providerId: number;
  providerName: string;
  appointmentDate: string;
  reason: string;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class Dashboard {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getPatientCount(): Observable<PaginatedResponse<unknown>> {
    return this.http.get<PaginatedResponse<unknown>>(
      `${this.apiUrl}/patients`
    );
  }

  getProviderCount(): Observable<PaginatedResponse<unknown>> {
    return this.http.get<PaginatedResponse<unknown>>(
      `${this.apiUrl}/providers`
    );
  }

  getAppointmentCount(): Observable<PaginatedResponse<unknown>> {
    return this.http.get<PaginatedResponse<unknown>>(
      `${this.apiUrl}/appointments`
    );
  }

  getMedicalRecordCount(): Observable<PaginatedResponse<unknown>> {
    return this.http.get<PaginatedResponse<unknown>>(
      `${this.apiUrl}/medicalrecords`
    );
  }

  getRecentAppointments(
    pageNumber: number = 1,
    pageSize: number = 5
  ): Observable<PaginatedResponse<Appointment>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('sortBy', 'appointmentDate')
      .set('sortOrder', 'asc');

    return this.http.get<PaginatedResponse<Appointment>>(
      `${this.apiUrl}/appointments`,
      { params }
    );
  }
}
