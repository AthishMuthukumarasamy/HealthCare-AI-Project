
import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  protected readonly title = signal('HealthConnect AI');

  protected readonly navigationItems = [
    { label: 'Dashboard', icon: '▦', route: '/dashboard' },
    { label: 'Patients', icon: '♙', route: '/patients' },
    { label: 'Providers', icon: '⚕', route: '/providers' },
    { label: 'Appointments', icon: '▣', route: '/appointments' },
    { label: 'Medical Records', icon: '▤', route: '/medical-records' }
  ];

  protected readonly statistics = [
    { label: 'Total Patients', value: '1,248', change: '+12.5%', icon: '♙' },
    { label: 'Total Providers', value: '48', change: '+4.2%', icon: '⚕' },
    { label: 'Appointments', value: '86', change: '+8.1%', icon: '▣' },
    { label: 'Medical Records', value: '2,540', change: '+15.3%', icon: '▤' }
  ];

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
}
