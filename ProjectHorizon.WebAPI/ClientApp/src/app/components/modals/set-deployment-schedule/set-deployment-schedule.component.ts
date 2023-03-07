import { Component, Input, OnInit } from '@angular/core';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { DeploymentStatus } from 'src/app/constants/deployment-status';
import { Settings } from 'src/app/constants/settings';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { DeploymentScheduleDto } from 'src/app/dtos/deployment-schedule-dto.model';
import { PublicApplicationDto } from 'src/app/dtos/public-application-dto.model';
import { DeploymentScheduleService } from 'src/app/services/deployment-schedule-services/deployment-schedule.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-set-deployment-schedule',
  templateUrl: './set-deployment-schedule.component.html',
  styleUrls: ['./set-deployment-schedule.component.scss'],
})
export class SetDeploymentScheduleComponent implements OnInit {
  @Input()
  enableCheckbox: boolean = false;

  message: string | undefined = undefined;
  selectedDeploymentSchedule: DeploymentScheduleDto | undefined;
  filteredDeploymentSchedules: ReadonlyArray<DeploymentScheduleDto> = [];
  deploymentScheduleNameTyped = new BehaviorSubject<string>('');
  autoUpdate: boolean = false;

  constructor(
    private readonly deploymentScheduleService: DeploymentScheduleService,
    private readonly activeModal: NgbActiveModal,
    private readonly modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.deploymentScheduleNameTyped
      .pipe(
        debounceTime(Settings.debounceTimeMs),
        // ignore new term if same as previous term
        distinctUntilChanged(),

        // switch to new search observable each time the term changes
        switchMap((name) =>
          this.deploymentScheduleService.filterDeploymentSchedulesByName(name)
        )
      )
      .subscribe(
        (deploymentSchedules) => {
          this.filteredDeploymentSchedules = deploymentSchedules;
        },
        (_) => this.showMessage()
      );
  }

  ngOnDestroy(): void {
    this.clearSelectedDeploymentSchedules();
  }

  clearSelectedDeploymentSchedules() {
    this.onInput('');
  }

  onInput(name: string) {
    this.deploymentScheduleNameTyped.next(name);
  }

  selectDeploymentSchedule(index: number) {
    this.selectedDeploymentSchedule = this.filteredDeploymentSchedules[index];
  }

  isSelected(deploymentSchedule: DeploymentScheduleDto) {
    return (
      this.selectedDeploymentSchedule &&
      deploymentSchedule.id == this.selectedDeploymentSchedule.id
    );
  }

  passBack() {
    if (!this.selectedDeploymentSchedule) {
      return;
    }

    this.activeModal.close([this.selectedDeploymentSchedule, this.autoUpdate]);
  }

  showMessage() {
    this.message = 'No deployment schedules';
  }

  close() {
    this.activeModal.close([undefined, undefined]);
  }
}
