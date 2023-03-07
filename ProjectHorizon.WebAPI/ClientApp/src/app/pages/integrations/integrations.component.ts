import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppRoutes } from 'src/app/constants/app-routes';
import { UserRole } from 'src/app/constants/user-role';
import { GraphConfigDto } from 'src/app/dtos/graph-config-dto.model';
import { OrganizationDto } from 'src/app/dtos/organization-dto.model';
import { GraphConfigService } from 'src/app/services/graph-config.service';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-integrations',
  templateUrl: './integrations.component.html',
  styleUrls: ['./integrations.component.scss'],
})
export class IntegrationsComponent implements OnInit, OnDestroy {
  isGraphConfigAvailable = true;
  isGraphCommunicationAvailable = true;
  connectionResult = '';
  connectionError = '';
  graphConfigDto = new GraphConfigDto();
  organizationDto?: OrganizationDto;

  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly userStore: UserStore,
    private readonly graphConfigService: GraphConfigService,
    private readonly subscriptionService: SubscriptionService,
    private readonly router: Router
  ) {}

  ngOnInit() {
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        if (
          (loggedInUser?.userRole == UserRole.SuperAdmin ||
            loggedInUser?.userRole == UserRole.Administrator) &&
          loggedInUser?.subscriptionId
        ) {
          this.graphConfigDto.subscriptionId = loggedInUser.subscriptionId;
          this.getGraphConfig();
        } else this.router.navigate([AppRoutes.root]);
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  submit() {
    this.graphConfigService
      .createGraphConfig(this.graphConfigDto)
      .subscribe((_) => {
        this.getGraphConfig();
      });
  }

  private getGraphConfig() {
    this.graphConfigService.hasGraphConfig().subscribe((result) => {
      this.isGraphConfigAvailable = result;
      if (result) {
        this.getGraphStatus();
      }
    });
  }

  getGraphStatus() {
    this.graphConfigService.checkGraphConfigStatus().subscribe(
      (_) => {
        this.isGraphCommunicationAvailable = true;
        this.connectionResult = 'Success';
        this.connectionError = '';
        this.getOrganization();
      },
      (error) => {
        this.isGraphCommunicationAvailable = false;
        this.connectionResult = 'Failure';
        this.connectionError = error.error;
      }
    );
  }

  getOrganization() {
    this.subscriptionService.getOrganization().subscribe((dto) => {
      console.log(dto);
      this.organizationDto = dto;
    });
  }

  removeGraphConfig() {
    this.graphConfigService
      .removeGraphConfig()
      .subscribe((_) => (this.isGraphConfigAvailable = false));
  }
}
