import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, Observable, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { ClearDeploymentScheduleModalComponent } from 'src/app/components/modals/clear-deployment-schedule-modal/clear-deployment-schedule-modal.component';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { UserRole } from 'src/app/constants/user-role';
import { DeploymentScheduleDto } from 'src/app/dtos/deployment-schedule-dto.model';
import { DeploymentScheduleRemoveDto } from 'src/app/dtos/deployment-schedule-remove-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { DeploymentScheduleService } from 'src/app/services/deployment-schedule-services/deployment-schedule.service';
import { UserStore } from 'src/app/services/user.store';
import { MultipleSelectDirective } from '../multiple-select.directive';

@Component({
  selector: 'app-deployment-schedules',
  templateUrl: './deployment-schedules.component.html',
  styleUrls: ['./deployment-schedules.component.scss'],
})
export class DeploymentSchedulesComponent extends MultipleSelectDirective<
  number,
  DeploymentScheduleDto,
  string
> {
  readonly appRoutes = AppRoutes;

  readonly userRole = UserRole;

  loggedInUser: UserDto;
  searchTerm: '';

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  constructor(
    protected readonly deploymentScheduleService: DeploymentScheduleService,
    protected readonly userStore: UserStore,
    protected readonly router: Router,
    protected readonly activatedRoute: ActivatedRoute,
    protected readonly modalService: NgbModal
  ) {
    super(userStore);
  }

  ngOnInit() {
    super.ngOnInit();
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber')) this.pgNr = params['pageNumber'];
      else this.pgNr = 1;

      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';

      this.update();
    });

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          combineLatest([
            this.deploymentScheduleService.getDeploymentSchedulesPaged(
              this.pgNr,
              this.pageSize,
              this.searchTerm
            ),
            this.deploymentScheduleService.getAllDeploymentScheduleIds(),
          ])
        )
      )
      .subscribe(([deploymentSchedulesPaged, deploymentSchedulesIds]) => {
        this.allItemsCount = deploymentSchedulesPaged.allItemsCount;
        this.pagedItems = deploymentSchedulesPaged.pageItems;

        this.selectedItemIds = new Set(
          [...this.selectedItemIds].filter((itemId) =>
            deploymentSchedulesIds.includes(itemId)
          )
        );
      });

    this.userStore
      .getLoggedInUser()
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (loggedInUser: UserDto | undefined): loggedInUser is UserDto =>
            loggedInUser !== undefined
        )
      )
      .subscribe((loggedInUser: UserDto) => {
        this.loggedInUser = loggedInUser;

        this.update();
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  update() {
    this.behaviorSubject$.next({});
  }

  getAllItemIds(): Observable<number[]> {
    return this.deploymentScheduleService.getAllDeploymentScheduleIds();
  }

  pageChange() {
    const queryParams: Params = {
      pageNumber: this.pgNr,
    };

    if (this.searchTerm.trim() !== '') queryParams.searchTerm = this.searchTerm;

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }

  applyBulkActions() {
    if (this.selectedOption == 'Copy') {
      this.startMultipleCopyDeploymentSchedule();
    }
    if (this.selectedOption == 'Delete') {
      this.startMultipleDeleteDeploymentSchedule();
    }
  }

  copyDeploymentSchedule(deploymentScheduleId: number) {
    this.deploymentScheduleService
      .copyDeploymentSchedule([deploymentScheduleId])
      .subscribe(() => {
        this.update();
      });
  }

  startMultipleCopyDeploymentSchedule() {
    this.deploymentScheduleService
      .copyDeploymentSchedule(Array.from(this.selectedItemIds))
      .subscribe(() => {
        this.update();
      });
  }

  startDeleteDeploymentSchedule(deploymentScheduleId: number) {
    const confirmationModalRef = this.modalService.open(
      ClearDeploymentScheduleModalComponent
    );

    confirmationModalRef.componentInstance.content1 =
      'By continuing the deployment schedule will be deleted from Endpoint Admin.';

    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: DeploymentScheduleRemoveDto) => {
        dto.ids = [deploymentScheduleId];

        this.deploymentScheduleService
          .deleteDeploymentSchedule(dto)
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }

  startMultipleDeleteDeploymentSchedule() {
    const confirmationModalRef = this.modalService.open(
      ClearDeploymentScheduleModalComponent
    );

    confirmationModalRef.componentInstance.content1 =
      'By continuing the deployment schedule will be deleted from Endpoint Admin.';

    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: DeploymentScheduleRemoveDto) => {
        dto.ids = Array.from(this.selectedItemIds);

        this.deploymentScheduleService
          .deleteDeploymentSchedule(dto)
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }
}
