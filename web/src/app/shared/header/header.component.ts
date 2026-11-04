import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly signedIn = this.auth.signedIn;

  // Signed out: open the sign-in page. Signed in: sign out, then return to the sign-in page.
  protected onAccountClick(): void {
    if (this.signedIn()) {
      this.auth.logout();
    }

    this.router.navigateByUrl('/login');
  }

  protected onCartClick(): void {
    this.router.navigateByUrl('/cart');
  }
}
