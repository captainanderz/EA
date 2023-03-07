import { Component, OnInit } from '@angular/core';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo } from '@azure/msal-browser';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { SubscriptionService } from 'src/app/services/subscription.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent implements OnInit {
  // Fields
  searchTerm = '';
  activeAccount: AccountInfo | null;
  subscriptionDto: SubscriptionDto | null;

  // Constructor
  public constructor(
    private authService: AuthService,
    private msalService: MsalService,
    private subscriptionService: SubscriptionService
  ) {}

  // Methods
  public ngOnInit(): void {
    // Sets the logged in user as active user
    this.activeAccount = this.msalService.instance.getActiveAccount();
    this.subscriptionService.get().subscribe((subscriptionDto) => {
      this.subscriptionDto = subscriptionDto;
    });
  }

  // Handles the logout logic
  public logout(): void {
    this.authService.logout();
  }
}
