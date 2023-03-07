import { Component } from '@angular/core';
import { DeploymentStatus } from 'src/app/constants/deployment-status';
import { UserStore } from 'src/app/services/user.store';
import { PublicApplicationService } from 'src/app/services/application-services/public-application.service';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { ConfirmationType } from 'src/app/constants/confirmation-type';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { PublicApplicationUploadModalComponent } from 'src/app/components/modals/public-application-upload-modal/public-application-upload-modal.component';
import { PublicApplicationDto } from 'src/app/dtos/public-application-dto.model';
import { RepositoryDirective } from '../repository/repository.directive';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { GraphConfigService } from 'src/app/services/graph-config.service';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';
import { PublicShoppingService } from 'src/app/services/shopping-services/public-shopping.service';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { PublicDeploymentScheduleService } from 'src/app/services/deployment-schedule-services/public-deployment-schedule.service';
import { SetDeploymentScheduleComponent } from 'src/app/components/modals/set-deployment-schedule/set-deployment-schedule.component';
import { DeploymentScheduleDto } from 'src/app/dtos/deployment-schedule-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';

@Component({
  selector: 'app-public-repository',
  templateUrl: './public-repository.component.html',
  providers: [
    {
      provide: ApplicationService,
      useClass: PublicApplicationService,
    },
  ],
  styleUrls: ['./../repository.scss'],
})
export class PublicRepositoryComponent extends RepositoryDirective<PublicApplicationDto> {
  UserDto = UserDto;

  applicationService = this.applicationService as PublicApplicationService;
  confirmationType: ConfirmationType | undefined = undefined;
  indexPublicApplicationToggled: number | undefined = undefined;

  constructor(
    private readonly subscriptionService: SubscriptionService,
    publicApplicationService: ApplicationService<PublicApplicationDto>,
    graphConfigService: GraphConfigService,
    modalService: NgbModal,
    userStore: UserStore,
    router: Router,
    route: ActivatedRoute,
    assignmentProfileService: AssignmentProfileService,
    deploymentScheduleService: PublicDeploymentScheduleService,
    shoppingService: PublicShoppingService
  ) {
    super(
      publicApplicationService,
      graphConfigService,
      modalService,
      userStore,
      router,
      route,
      assignmentProfileService,
      deploymentScheduleService,
      shoppingService
    );
  }

  openApplicationUploadModal() {
    super.openApplicationUploadModal(PublicApplicationUploadModalComponent);
  }

  protected openSetDeploymentScheduleModal(): Promise<
    [DeploymentScheduleDto | undefined, boolean]
  > {
    const modalRef = this.modalService.open(SetDeploymentScheduleComponent, {
      size: 'lg',
      scrollable: true,
    });

    modalRef.componentInstance.enableCheckbox = true;

    return modalRef.result;
  }

  //TOGGLE
  toggleGlobalAutoUpdate(event: MouseEvent) {
    event.preventDefault();

    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `Modifying the global auto-update will change the auto-update behavior
      for every deployed application.`;
    modalRef.componentInstance.content2 = `Do you want to continue?`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.continueToggle();
    });

    this.confirmationType = ConfirmationType.ToggleGlobalAutoUpdate;
  }

  private _toggleGlobalAutoUpdate() {
    this.subscriptionDto.globalAutoUpdate =
      !this.subscriptionDto.globalAutoUpdate;

    const oldManualApprove = this.subscriptionDto.globalManualApprove;

    if (this.subscriptionDto.globalAutoUpdate) {
      this.subscriptionDto.globalManualApprove = false;
    }

    this.subscriptionService
      .updateSubscriptionAutoUpdate(this.subscriptionDto.globalAutoUpdate)
      .subscribe((autoUpdate) => {
        this.subscriptionDto.globalAutoUpdate = autoUpdate;

        if (!autoUpdate) {
          this.subscriptionDto.globalManualApprove = oldManualApprove;
        }

        this.userStore.setLoggedInUser(this.loggedInUser);
      });
  }

  toggleGlobalManualApprove(event: MouseEvent) {
    event.preventDefault();

    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `Modifying the global manual approve will change the manual approve
      behavior for every deployed application.`;
    modalRef.componentInstance.content2 = `Do you want to continue?`;
    modalRef.componentInstance.continue.subscribe(() => this.continueToggle());

    this.confirmationType = ConfirmationType.ToggleGlobalManualApprove;
  }

  private _toggleGlobalManualApprove() {
    this.subscriptionDto.globalManualApprove =
      !this.subscriptionDto.globalManualApprove;

    const oldAutoUpdate = this.subscriptionDto.globalAutoUpdate;

    if (this.subscriptionDto.globalManualApprove) {
      this.subscriptionDto.globalAutoUpdate = false;
    }

    this.subscriptionService
      .updateSubscriptionManualApprove(this.subscriptionDto.globalManualApprove)
      .subscribe((manualApprove) => {
        this.subscriptionDto.globalManualApprove = manualApprove;

        if (!manualApprove) {
          this.subscriptionDto.globalAutoUpdate = oldAutoUpdate;
        }

        this.userStore.setLoggedInUser(this.loggedInUser);
      });
  }

  togglePublicApplicationAutoUpdate(
    event: MouseEvent,
    indexPublicApplication: number
  ) {
    const pubApp = this.pagedItems[indexPublicApplication];
    this.confirmationType = ConfirmationType.TogglePublicApplicationAutoUpdate;
    this.indexPublicApplicationToggled = indexPublicApplication;

    if (
      !pubApp.autoUpdate &&
      pubApp.deploymentStatus != DeploymentStatus.SuccessfulUpToDate &&
      pubApp.deploymentStatus != DeploymentStatus.InProgress
    ) {
      const message = pubApp.assignedDeploymentSchedule
        ? pubApp.assignedDeploymentSchedule.cronTrigger
          ? undefined
          : 'Enabling auto-update will trigger the deployment schedule for this application.'
        : 'Enabling auto-update will trigger a deployment for this application to Microsoft Endpoint Manager.';

      if (!message) {
        this.continueToggle();
        return;
      }

      event.preventDefault();
      const modalRef = this.modalService.open(ConfirmationModalComponent);

      modalRef.componentInstance.content1 = message;
      modalRef.componentInstance.content2 = `Do you want to continue?`;
      modalRef.componentInstance.continue.subscribe(() => {
        this.continueToggle();
      });
    } else {
      this.continueToggle();
    }
  }

  private _togglePublicApplicationAutoUpdate(indexPublicApplication: number) {
    const pubApp = this.pagedItems[indexPublicApplication];
    const oldManualApprove = pubApp.manualApprove;
    pubApp.autoUpdate = !pubApp.autoUpdate;

    if (pubApp.autoUpdate) {
      pubApp.manualApprove = false;
    }

    this.applicationService
      .updateSubscriptionPublicApplicationAutoUpdate(
        pubApp.id,
        pubApp.autoUpdate
      )
      .subscribe((autoUpdate) => {
        pubApp.autoUpdate = autoUpdate;

        if (!autoUpdate) {
          pubApp.manualApprove = oldManualApprove;
        }
      });
  }

  togglePublicApplicationManualApprove(indexPublicApplication: number) {
    const pubApp = this.pagedItems[indexPublicApplication];
    const oldAutoUpdate = pubApp.autoUpdate;
    pubApp.manualApprove = !pubApp.manualApprove;

    if (pubApp.manualApprove) {
      pubApp.autoUpdate = false;
    }

    this.applicationService
      .updateSubscriptionPublicApplicationManualApprove(
        pubApp.id,
        pubApp.manualApprove
      )
      .subscribe((manualApprove) => {
        pubApp.manualApprove = manualApprove;

        if (!manualApprove) {
          pubApp.autoUpdate = oldAutoUpdate;
        }
      });
  }

  continueToggle() {
    switch (this.confirmationType) {
      case ConfirmationType.ToggleGlobalManualApprove:
        return this._toggleGlobalManualApprove();

      case ConfirmationType.ToggleGlobalAutoUpdate:
        return this._toggleGlobalAutoUpdate();

      case ConfirmationType.TogglePublicApplicationAutoUpdate:
        return this._togglePublicApplicationAutoUpdate(
          this.indexPublicApplicationToggled!
        );
    }
  }

  startAssignmentProfileAssignment(applicationId: number) {
    this.openAssignProfileModal().then((assignmentProfile) => {
      if (!assignmentProfile) {
        return;
      }

      this.assignmentProfileService
        .assignAssignmentProfileToPublicApplications(assignmentProfile.id, [
          applicationId,
        ])
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startMultipleAssignmentProfileAssignments() {
    this.openAssignProfileModal().then((assignmentProfile) => {
      if (!assignmentProfile) {
        return;
      }

      this.showConfirmationModal(
        'Clicking Continue will start assigning profiles to applications.'
      ).subscribe(() => {
        this.assignmentProfileService
          .assignAssignmentProfileToPublicApplications(
            assignmentProfile?.id,
            Array.from(this.selectedItemIds)
          )
          .subscribe((_) => {
            this.update();
          });
      });
    });
  }
}
