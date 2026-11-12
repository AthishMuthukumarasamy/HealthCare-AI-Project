
import { Component, inject } from '@angular/core';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss'
})
export class Topbar {
  protected readonly auth = inject(Auth);
}
