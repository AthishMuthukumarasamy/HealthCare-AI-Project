
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class Sidebar {
  protected readonly navigationItems = [
    { label: 'Dashboard', icon: '▦', route: '/dashboard' },
    { label: 'Patients', icon: '♙', route: '/patients' },
    { label: 'Providers', icon: '⚕', route: '/providers' },
    { label: 'Appointments', icon: '▣', route: '/appointments' },
    { label: 'Medical Records', icon: '▤', route: '/medical-records' }
  ];
}
