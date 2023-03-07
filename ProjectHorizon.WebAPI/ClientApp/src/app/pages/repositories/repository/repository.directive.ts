import { Directive, OnDestroy, OnInit, Type } from '@angular/core';
import {
  Router,
  ActivatedRoute,
  Params,
  NavigationEnd,
  Event,
} from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, Observable, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { ApplicationUploadModalComponent } from 'src/app/components/modals/application-upload-modal/application-upload-modal.component';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { AssignProfileModalComponent } from 'src/app/components/modals/assign-profile-modal/assign-profile-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { DeploymentStatus } from 'src/app/constants/deployment-status';
import { UserRole } from 'src/app/constants/user-role';
import {
  ApplicationDto,
  displayArchitecture,
} from 'src/app/dtos/application-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { GraphConfigService } from 'src/app/services/graph-config.service';
import { UserStore } from 'src/app/services/user.store';
import { MultipleSelectDirective } from '../../multiple-select.directive';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';
import { AssignmentProfileDto } from 'src/app/dtos/assignment-profile-dto.model';
import { ShoppingService } from 'src/app/services/shopping-services/shopping.service';
import { ShopAddConfirmationModalComponent } from 'src/app/components/modals/shop-add-confirmation-modal/shop-add-confirmation-modal.component';
import { ShopAddDto } from 'src/app/dtos/shop-add-dto.model';
import { ShopRemoveConfirmationModalComponent } from 'src/app/components/modals/shop-remove-confirmation-modal/shop-remove-confirmation-modal.component';
import { ShopRemoveDto } from 'src/app/dtos/shop-remove-dto';
import { DeploymentScheduleDto } from 'src/app/dtos/deployment-schedule-dto.model';
import { SetDeploymentScheduleComponent } from 'src/app/components/modals/set-deployment-schedule/set-deployment-schedule.component';
import { ClearDeploymentScheduleModalComponent } from 'src/app/components/modals/clear-deployment-schedule-modal/clear-deployment-schedule-modal.component';
import { DeploymentScheduleService } from 'src/app/services/deployment-schedule-services/deployment-schedule.service';
import { DeploymentScheduleClearDto } from 'src/app/dtos/deployment-schedule-clear-dto.model';
import { PhaseState } from 'src/app/constants/phase-state';
import { DeploymentScheduleAssignDto } from 'src/app/dtos/deployment-schedule-assign-dto.model';

@Directive({
  selector: '[appRepository]',
})
export class RepositoryDirective<TApplication extends ApplicationDto>
  extends MultipleSelectDirective<number, TApplication, string>
  implements OnInit, OnDestroy
{
  readonly deploymentStatus = DeploymentStatus;
  readonly userRole = UserRole;
  readonly displayArchitecture = displayArchitecture;
  readonly phaseState = PhaseState;

  subscriptionDto: SubscriptionDto;
  loggedInUser: UserDto;
  searchTerm = '';

  protected readonly behaviorSubject$ = new BehaviorSubject<{}>({});
  protected readonly unsubscribe$ = new Subject<void>();

  constructor(
    protected readonly applicationService: ApplicationService<TApplication>,
    protected readonly graphConfigService: GraphConfigService,
    protected readonly modalService: NgbModal,
    protected readonly userStore: UserStore,
    protected readonly router: Router,
    protected readonly activatedRoute: ActivatedRoute,
    protected readonly assignmentProfileService: AssignmentProfileService,
    protected readonly deploymentScheduleService: DeploymentScheduleService,
    protected readonly shoppingService: ShoppingService
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

        if (!loggedInUser.subscriptionId) {
          this.router.navigate([AppRoutes.createSubscription]);
        }

        this.subscriptionDto =
          loggedInUser.subscriptions[
            this.loggedInUser.currentSubscriptionIndex
          ];

        this.update();
      });

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          this.applicationService.getApplicationsPaged(
            this.pgNr,
            this.pageSize,
            this.searchTerm
          )
        ),
        switchMap(() =>
          combineLatest([
            this.applicationService.getApplicationsPaged(
              this.pgNr,
              this.pageSize,
              this.searchTerm
            ),
            this.applicationService.getAllApplicationIds(),
          ])
        )
      )
      .subscribe(([applications, applicationIds]) => {
        this.allItemsCount = applications.allItemsCount;
        this.pagedItems = applications.pageItems;

        this.selectedItemIds = new Set(
          [...this.selectedItemIds].filter((itemId) =>
            applicationIds.includes(itemId)
          )
        );
      });

    this.router.events
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (event: Event): event is NavigationEnd =>
            event instanceof NavigationEnd
        )
      )
      .subscribe((_) => this.update());
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
    this.behaviorSubject$.complete();
  }

  update() {
    this.behaviorSubject$.next({});
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

  getAllItemIds(): Observable<number[]> {
    return this.applicationService.getAllApplicationIds();
  }

  protected openApplicationUploadModal(
    applicationUploadModalComponentType: Type<
      ApplicationUploadModalComponent<TApplication>
    >
  ) {
    const modalRef = this.modalService.open(
      applicationUploadModalComponentType,
      {
        size: 'lg',
        backdrop: 'static',
        keyboard: false,
      }
    );

    modalRef.componentInstance.applicationAddStarted.subscribe(() =>
      this.applicationAddStarted()
    );
  }

  applicationAddStarted(): void {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Processing';
    modalRef.componentInstance.content1 =
      'The application is being processed and added to the repository.';
    modalRef.componentInstance.content2 =
      'You will get a notification when it is completed.';
  }

  startDownloadApplication(applicationId: number): void {
    this.applicationService
      .getDownloadUriForApplication(applicationId)
      .subscribe(
        (packageUri) => {
          const a = document.createElement('a');
          a.href = packageUri;
          a.download = '';
          a.type = 'application/octet-stream';
          a.referrerPolicy = 'no-referrer';
          a.click();

          this.showDownloadModal(
            'Download started',
            `The archive file will appear at the bottom of the screen or in your browser's downloads section.`
          );
        },
        (_) =>
          this.showDownloadModal(
            'Download failed',
            'There was an error when trying to download your application.',
            'Please try again later or contact support.'
          )
      );
  }

  protected showDownloadModal(
    title: string,
    content1: string,
    content2: string = ''
  ) {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = title;
    modalRef.componentInstance.content1 = content1;
    modalRef.componentInstance.content2 = content2;
  }

  startDeploySingleApplication(application: ApplicationDto): void {
    if (application.deploymentStatus !== DeploymentStatus.SuccessfulUpToDate)
      this.deployApplications(
        [application.id],
        'The application deployment to Microsoft Endpoint Manager has started.',
        'All users on this subscription will get a notification when it is completed.'
      );
    else
      this.showDeploymentConfirmationModal(
        `${application.name} is already deployed with the latest version. Clicking Continue will redeploy this application.`,
        undefined,
        [application.id],
        'The application deployment to Microsoft Endpoint Manager has started.',
        'All users on this subscription will get a notification when it is completed.'
      );
  }

  startMultipleDeployment() {
    this.showBulkDeploymentConfirmationModal(Array.from(this.selectedItemIds));
  }

  protected showBulkDeploymentConfirmationModal(
    applicationIdsToDeploy: Array<number>
  ): void {
    this.showDeploymentConfirmationModal(
      `Clicking Continue will start the deployment of ${applicationIdsToDeploy.length} applications to Microsoft Endpoint Manager!`,
      undefined,
      applicationIdsToDeploy,
      `The deployment of ${applicationIdsToDeploy.length} applications to Microsoft Endpoint Manager has started.`,
      'All users on this subscription will get notifications when the deployments are completed.'
    );
  }

  showConfirmationModal(
    content1Confirmation: string,
    content2Confirmation = ''
  ): Observable<any> {
    const confirmationModalRef = this.modalService.open(
      ConfirmationModalComponent
    );

    confirmationModalRef.componentInstance.content1 = content1Confirmation;
    confirmationModalRef.componentInstance.content2 = content2Confirmation;

    return confirmationModalRef.componentInstance.continue;
  }

  protected showDeploymentConfirmationModal(
    content1Confirmation: string,
    content2Confirmation = '',
    applicationIdsToDeploy: number[],
    content1Started: string,
    content2Started = ''
  ) {
    const confirmationModalRef = this.modalService.open(
      ConfirmationModalComponent
    );

    confirmationModalRef.componentInstance.content1 = content1Confirmation;
    confirmationModalRef.componentInstance.content2 = content2Confirmation;

    confirmationModalRef.componentInstance.continue.subscribe(() =>
      this.deployApplications(
        applicationIdsToDeploy,
        content1Started,
        content2Started
      )
    );
  }

  protected showDeploymentStartedModal(content1: string, content2 = '') {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Deployment started';
    modalRef.componentInstance.content1 = content1;
    modalRef.componentInstance.content2 = content2;

    modalRef.closed.subscribe(() => this.update());
  }

  protected showDeploymentHaltedModal(content1: string, content2 = '') {
    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Deployment halted';
    modalRef.componentInstance.content1 = content1;
    modalRef.componentInstance.content2 = content2;
  }

  private deployApplications(
    applicationIdsToDeploy: number[],
    content1: string,
    content2 = ''
  ) {
    const subscriptionName =
      this.loggedInUser.subscriptions[
        this.loggedInUser.currentSubscriptionIndex
      ].name;

    this.graphConfigService.hasGraphConfig().subscribe(
      (hasGraphConfig) => {
        if (hasGraphConfig)
          this.applicationService
            .deployApplications(applicationIdsToDeploy)
            .subscribe(
              (_) => this.showDeploymentStartedModal(content1, content2),
              (error) =>
                this.showDeploymentHaltedModal(
                  `Deployment cannot be done because ${error.error.toLowerCase()}`
                )
            );
        else {
          this.showDeploymentHaltedModal(
            `Deploy isn't available because the integration with Microsoft Endpoint Manager has not been configured.`,
            `An administrator for this subscription(${subscriptionName})
        should first configure it from the Settings -> Integrations page.`
          );
        }
      },
      (_) =>
        this.showDeploymentHaltedModal(
          `Deploy isn't available because the integration with Microsoft Endpoint Manager has not been configured.`,
          `An administrator for this subscription(${subscriptionName})
    should first configure it from the Settings -> Integrations page.`
        )
    );
  }

  deleteApplication(application: ApplicationDto): void {
    const confirmationModalRef = this.modalService.open(
      ConfirmationModalComponent
    );
    confirmationModalRef.componentInstance.content1 = `By continuing the application ${application.name} will be removed permanently.`;
    confirmationModalRef.componentInstance.continue.subscribe(() =>
      this.applicationService
        .deleteApplications([application.id])
        .subscribe(() => {
          this.deselectItems(new Set([application.id]));

          this.update();
        })
    );
  }

  protected openAssignProfileModal(): Promise<
    AssignmentProfileDto | undefined
  > {
    const modalRef = this.modalService.open(AssignProfileModalComponent, {
      size: 'lg',
      scrollable: true,
    });

    return modalRef.result;
  }

  protected openSetDeploymentScheduleModal(): Promise<
    [DeploymentScheduleDto | undefined, boolean]
  > {
    const modalRef = this.modalService.open(SetDeploymentScheduleComponent, {
      size: 'lg',
      scrollable: true,
    });

    return modalRef.result;
  }

  // They are implemented in children classes
  startMultipleAssignmentProfileAssignments() {}

  startMultipleAssignmentProfileClears() {}

  applyBulkActions() {
    if (this.selectedOption == 'Deploy') {
      this.startMultipleDeployment();
    }
    if (this.selectedOption == 'Assign assignment profile') {
      this.startMultipleAssignmentProfileAssignments();
    }
    if (this.selectedOption == 'Clear assigned assignment profile') {
      this.startMultipleAssignedAssignmentProfileClears();
    }
    if (this.selectedOption == 'Assign deployment schedule') {
      this.startMultipleDeploymentScheduleAssignments();
    }
    if (this.selectedOption == 'Clear assigned deployment schedule') {
      this.startMultipleAssignedDeploymentScheduleClears();
    }
    if (this.selectedOption == 'Delete deployment schedule patch-app') {
      this.startMultipleDeploymentSchedulePatchAppDelete();
    }
    if (this.selectedOption == 'Delete') {
      this.startMultipleDelete();
    }
    if (this.selectedOption == 'Download') {
      this.startMultipleDownload(Array.from(this.selectedItemIds));
    }
    if (this.selectedOption == 'Add to shop') {
      this.startMultipleAddToShop(Array.from(this.selectedItemIds));
    }
    if (this.selectedOption == 'Remove from shop') {
      this.startMultipleRemoveFromShop(Array.from(this.selectedItemIds));
    }
  }

  addToShop(application: ApplicationDto) {
    if (
      application.deploymentStatus == DeploymentStatus.Failed ||
      application.deploymentStatus == DeploymentStatus.InProgress
    ) {
    } else {
      const confirmationModalRef = this.modalService.open(
        ShopAddConfirmationModalComponent
      );
      confirmationModalRef.componentInstance.appDto = application;
      confirmationModalRef.componentInstance.continue.subscribe(
        (dto: ShopAddDto) => {
          dto.applicationIds = [application.id];

          this.shoppingService.addApplications(dto).subscribe((_) => {
            this.update();
          });
        }
      );
    }
  }

  startMultipleAddToShop(applicationIds: Array<number>) {
    const confirmationModalRef = this.modalService.open(
      ShopAddConfirmationModalComponent
    );
    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: ShopAddDto) => {
        dto.applicationIds = applicationIds;

        this.shoppingService.addApplications(dto).subscribe((_) => {
          this.update();
        });
      }
    );
  }

  removeFromShop(application: ApplicationDto) {
    const confirmationModalRef = this.modalService.open(
      ShopRemoveConfirmationModalComponent
    );
    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: ShopRemoveDto) => {
        dto.applicationIds = [application.id];

        this.shoppingService.removeApplications(dto).subscribe((_) => {
          this.update();
        });
      }
    );
  }

  startMultipleRemoveFromShop(applicationIds: Array<number>) {
    const confirmationModalRef = this.modalService.open(
      ShopRemoveConfirmationModalComponent
    );
    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: ShopRemoveDto) => {
        dto.applicationIds = applicationIds;

        this.shoppingService.removeApplications(dto).subscribe((_) => {
          this.update();
        });
      }
    );
  }

  startAssignedAssignmentProfileClear(applicationId: number) {
    this.assignmentProfileService
      .clearAssignmentProfileFromPublicApplications([applicationId])
      .subscribe((_) => {
        this.update();
      });
  }

  startMultipleAssignedAssignmentProfileClears() {
    this.showConfirmationModal(
      'Clicking Continue will start clearing the assigned profiles of the selected applications'
    ).subscribe(() => {
      this.assignmentProfileService
        .clearAssignmentProfileFromPublicApplications(
          Array.from(this.selectedItemIds)
        )
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startDeploymentScheduleAssignment(applicationId: number) {
    this.openSetDeploymentScheduleModal().then(
      ([deploymentSchedule, autoUpdate]) => {
        if (!deploymentSchedule) {
          return;
        }

        this.deploymentScheduleService
          .assignDeploymentScheduleToApplications(deploymentSchedule.id, {
            applicationIds: [applicationId],
            autoUpdate: autoUpdate,
          })
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }

  startMultipleDeploymentScheduleAssignments() {
    this.openSetDeploymentScheduleModal().then(
      ([deploymentSchedule, autoUpdate]) => {
        if (!deploymentSchedule) {
          return;
        }

        this.deploymentScheduleService
          .assignDeploymentScheduleToApplications(deploymentSchedule.id, {
            applicationIds: Array.from(this.selectedItemIds),
            autoUpdate: autoUpdate,
          })
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }

  startAssignedDeploymentScheduleClear(applicationId: number) {
    const confirmationModalRef = this.modalService.open(
      ClearDeploymentScheduleModalComponent
    );
    confirmationModalRef.componentInstance.content1 =
      'By continuing the deployment schedule will be deleted from Endpoint Admin.';

    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: DeploymentScheduleClearDto) => {
        dto.applicationIds = [applicationId];

        this.deploymentScheduleService
          .clearDeploymentScheduleFromApplications(dto)
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }

  startMultipleAssignedDeploymentScheduleClears() {
    const confirmationModalRef = this.modalService.open(
      ClearDeploymentScheduleModalComponent
    );

    confirmationModalRef.componentInstance.content1 =
      'By continuing the deployment schedule will be deleted from Endpoint Admin.';

    confirmationModalRef.componentInstance.continue.subscribe(
      (dto: DeploymentScheduleClearDto) => {
        dto.applicationIds = Array.from(this.selectedItemIds);

        this.deploymentScheduleService
          .clearDeploymentScheduleFromApplications(dto)
          .subscribe((_) => {
            this.update();
          });
      }
    );
  }

  startDeploymentSchedulePatchAppDelete(id: number) {
    this.showConfirmationModal(
      'Clicking Continue, the patch-app instance will be removed permanently from Microsoft Endpoint Manager.'
    ).subscribe(() => {
      this.deploymentScheduleService
        .deleteDeploymentSchedulePatchAppFromApplications([id])
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startMultipleDeploymentSchedulePatchAppDelete() {
    this.showConfirmationModal(
      'Clicking Continue, the patch-app instances will be removed permanently from Microsoft Endpoint Manager.'
    ).subscribe(() => {
      this.deploymentScheduleService
        .deleteDeploymentSchedulePatchAppFromApplications(
          Array.from(this.selectedItemIds)
        )
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startMultipleDelete() {
    this.showConfirmationModal(
      'Clicking Continue, the selected applications will be deleted from Endpoint Admin.'
    ).subscribe(() => {
      this.applicationService
        .deleteApplications(Array.from(this.selectedItemIds))
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startMultipleDownload(applicationIds: Array<number>) {
    const applicationId = applicationIds.pop();

    if (applicationId == undefined) {
      return;
    }

    this.applicationService
      .getDownloadUriForApplication(applicationId)
      .subscribe(
        (packageUri) => {
          const a = document.createElement('a');
          a.href = packageUri;
          a.download = '';
          a.type = 'application/octet-stream';
          a.referrerPolicy = 'no-referrer';
          a.click();

          setTimeout(() => this.startMultipleDownload(applicationIds), 5000);
        },
        (_) => console.log('Multiple Download Failed')
      );
  }
}
