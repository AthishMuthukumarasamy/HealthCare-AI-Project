
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface PaginatedResponse<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  data: T[];
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
}
