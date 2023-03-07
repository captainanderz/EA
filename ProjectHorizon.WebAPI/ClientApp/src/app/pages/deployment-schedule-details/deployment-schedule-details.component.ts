import {
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Location, WeekDay } from '@angular/common';
import { DeploymentSchedulePhaseDto } from 'src/app/dtos/deployment-schedule-phase-dto.model';
import { DeploymentScheduleDetailsDto } from 'src/app/dtos/deployment-schedule-details-dto.model';
import {
  CronSelectorModalComponent,
  Mode,
  MonthWeek,
  TriggerDto,
} from 'src/app/components/modals/cron-selector-modal/cron-selector-modal.component';
import cronstrue from 'cronstrue';
import { Observable, observable, of } from 'rxjs';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { mergeMap } from 'rxjs/operators';
import { Console } from 'console';
import { THIS_EXPR } from '@angular/compiler/src/output/output_ast';
import { DeploymentScheduleService } from 'src/app/services/deployment-schedule-services/deployment-schedule.service';
import { getParseErrors } from '@angular/compiler';
import { AssignProfileModalComponent } from 'src/app/components/modals/assign-profile-modal/assign-profile-modal.component';
import { AssignmentProfileDto } from 'src/app/dtos/assignment-profile-dto.model';
import { NgForm } from '@angular/forms';
import {
  markAllAsTouched,
  updateTreeValidity,
  focusInvalidInput,
  generateGuid,
} from 'src/app/utility';

enum Action {
  New = 'new',
  Edit = 'edit',
  Details = 'details',
}

@Component({
  selector: 'app-deployment-schedule-details',
  templateUrl: './deployment-schedule-details.component.html',
  styleUrls: ['./deployment-schedule-details.component.scss'],
})
export class DeploymentScheduleDetailsComponent implements OnInit, OnDestroy {
  markAllAsTouched = markAllAsTouched;
  updateTreeValidity = updateTreeValidity;
  focusInvalidInput = focusInvalidInput;

  dto: DeploymentScheduleDetailsDto = new DeploymentScheduleDetailsDto();
  action: Action = Action.New;
  Action = Action;
  errorMessage: string | undefined = undefined;

  cronExpression: string | undefined;
  minOffset: number = 0;
  maxOffset: number = 27;

  triggerValue: string = 'On new version';

  @ViewChild('deploymentScheduleFormElement')
  deploymentScheduleFormElementRef: ElementRef<HTMLFormElement>;

  constructor(
    private readonly modalService: NgbModal,
    private readonly deploymentScheduleService: DeploymentScheduleService,
    private readonly location: Location,
    private route: ActivatedRoute
  ) {}

  ngOnDestroy(): void {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.dto.id = params['id'];

      if (this.dto.id) {
        this.deploymentScheduleService
          .getDeploymentScheduleById(this.dto.id)
          .subscribe(
            (deploymentSchedule) => {
              this.dto.name = deploymentSchedule.name;
              this.dto.phases = deploymentSchedule.phases;
              this.dto.phases.forEach((phase) => {
                phase.guid = generateGuid();
              });

              this.updateTrigger(
                TriggerDto.fromExpression(deploymentSchedule.cronTrigger)
              );
            },
            () => this.location.back()
          );
      }
    });

    this.route.url.subscribe((url) => {
      let action = url.pop()?.path;

      if (
        action == undefined ||
        !Object.values(Action).includes(action as unknown as Action)
      ) {
        action = url.pop()?.path;
      }

      const indexOfAction = Object.values(Action).indexOf(
        action as unknown as Action
      );
      const actionKey = Object.keys(Action)[
        indexOfAction
      ] as keyof typeof Action;

      this.action = Action[actionKey];
    });
  }

  onCancel() {
    this.location.back();
  }

  getPhaseMinOffset(index: number) {
    const previousIndex = index - 1;

    if (previousIndex < 0) {
      return this.minOffset;
    }

    return this.dto.phases[previousIndex].offsetDays + 1;
  }

  updateTrigger(dto: TriggerDto) {
    switch (+dto.mode) {
      case Mode.Update: {
        this.triggerValue = 'On new version';
        this.maxOffset = 27;
        break;
      }

      case Mode.Weekly: {
        this.triggerValue = `On each ${WeekDay[dto.weekDay!]}`;
        this.maxOffset = 6;
        break;
      }

      case Mode.Monthly: {
        this.triggerValue = `On the ${MonthWeek[
          dto.monthWeek!
        ].toLowerCase()} ${WeekDay[dto.weekDay!]} of each month`;
        this.maxOffset = 27;
        break;
      }
    }

    this.dto.cronTrigger = dto.expression;
  }

  setCronTrigger() {
    this.openCronTriggerModal().then((dto: TriggerDto | undefined) => {
      if (!dto) {
        return;
      }

      this.updateTrigger(dto);
    });
  }

  private openCronTriggerModal(): Promise<TriggerDto> {
    const modalRef = this.modalService.open(CronSelectorModalComponent, {
      size: 'md',
      scrollable: true,
    });

    modalRef.componentInstance.expression = this.dto.cronTrigger;

    return modalRef.result;
  }

  addPhase() {
    const phases = this.dto.phases;
    const phasesLength = phases.length;
    let offsetDays = 0;

    if (phasesLength > 0) {
      offsetDays = phases[phasesLength - 1].offsetDays + 1;
    }

    if (offsetDays > this.maxOffset) {
      const modalRef = this.modalService.open(InfoModalComponent);
      modalRef.componentInstance.title = 'Error';
      modalRef.componentInstance.content1 =
        'Cannot add more phases because the last offset reached the maximum value.';

      return;
    }

    phases.push({
      ...DeploymentSchedulePhaseDto.default,
      name: 'New Phase',
      offsetDays,
      guid: generateGuid(),
    });
  }

  removePhase(index: number) {
    this.dto.phases.splice(index, 1);
  }

  setAssignmentProfile(index: number) {
    this.openAssignProfileModal().then((assignmentProfile) => {
      if (!assignmentProfile) {
        return;
      }

      this.dto.phases[index].assignmentProfileId = assignmentProfile.id;
      this.dto.phases[index].assignmentProfileName = assignmentProfile.name;
    });
  }

  private openAssignProfileModal(): Promise<AssignmentProfileDto | undefined> {
    const modalRef = this.modalService.open(AssignProfileModalComponent, {
      size: 'lg',
      scrollable: true,
    });

    return modalRef.result;
  }

  validatePhases() {
    const phases = this.dto.phases;
    let errorIndex = undefined;
    let errorMessage = undefined;

    const generateError = () => {
      if (phases.length > this.maxOffset + 1) {
        errorMessage = `Too many phases. The maximum number of phases for the selected trigger mode is ${
          this.maxOffset + 1
        }`;

        return;
      }
    };

    generateError();

    if (!errorMessage) {
      return true;
    }

    const modalRef = this.modalService.open(InfoModalComponent);
    modalRef.componentInstance.title = 'Error';
    modalRef.componentInstance.content1 =
      errorIndex != undefined
        ? `Phase number ${errorIndex + 1} is invalid.`
        : '';
    modalRef.componentInstance.content2 = errorMessage;

    return false;
  }

  onSubmit() {
    const valid = this.validatePhases();
    if (!valid) {
      return;
    }

    let observable;

    if (this.dto.id) {
      observable = this.deploymentScheduleService.editDeploymentSchedule(
        this.dto.id,
        this.dto
      );
    } else {
      observable = this.deploymentScheduleService.addDeploymentSchedule(
        this.dto
      );
    }

    observable.subscribe((_) => {
      this.location.back();
    });
  }

  showError() {
    this.errorMessage = 'The deployment schedule name is required';
  }
}
