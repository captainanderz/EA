import { Component } from '@angular/core';
import { UserStore } from 'src/app/services/user.store';
import { PrivateApplicationService } from 'src/app/services/application-services/private-application.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { PrivateApplicationUploadModalComponent } from 'src/app/components/modals/private-application-upload-modal/private-application-upload-modal.component';
import { ActivatedRoute, Router } from '@angular/router';
import { PrivateApplicationDto } from 'src/app/dtos/private-application-dto.model';
import { RepositoryDirective } from '../repository/repository.directive';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { GraphConfigService } from 'src/app/services/graph-config.service';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';
import { PrivateShoppingService } from 'src/app/services/shopping-services/private-shopping.service';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { PrivateDeploymentScheduleService } from 'src/app/services/deployment-schedule-services/private-deployment-schedule.service';

@Component({
  selector: 'app-private-repository',
  templateUrl: './private-repository.component.html',
  providers: [
    {
      provide: ApplicationService,
      useClass: PrivateApplicationService,
    },
  ],
  styleUrls: ['./../repository.scss'],
})
export class PrivateRepositoryComponent extends RepositoryDirective<PrivateApplicationDto> {
  applicationService = this.applicationService as PrivateApplicationService;

  constructor(
    privateApplicationService: ApplicationService<PrivateApplicationDto>,
    graphConfigService: GraphConfigService,
    modalService: NgbModal,
    userStore: UserStore,
    router: Router,
    route: ActivatedRoute,
    assignmentProfileService: AssignmentProfileService,
    deploymentScheduleService: PrivateDeploymentScheduleService,
    shoppingService: PrivateShoppingService
  ) {
    super(
      privateApplicationService,
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
    super.openApplicationUploadModal(PrivateApplicationUploadModalComponent);
  }

  startAssignedAssignmentProfileClear(applicationId: number) {
    this.assignmentProfileService
      .clearAssignmentProfileFromPrivateApplications([applicationId])
      .subscribe((_) => {
        this.update();
      });
  }

  startMultipleAssignedAssignmentProfileClears() {
    this.showConfirmationModal(
      'Clicking Continue will start clearing the assigned profiles of the selected applications'
    ).subscribe(() => {
      this.assignmentProfileService
        .clearAssignmentProfileFromPrivateApplications(
          Array.from(this.selectedItemIds)
        )
        .subscribe((_) => {
          this.update();
        });
    });
  }

  startAssignmentProfileAssignment(applicationId: number) {
    this.openAssignProfileModal().then((assignmentProfile) => {
      if (!assignmentProfile) {
        return;
      }

      this.assignmentProfileService
        .assignAssignmentProfileToPrivateApplications(assignmentProfile.id, [
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
          .assignAssignmentProfileToPrivateApplications(
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
