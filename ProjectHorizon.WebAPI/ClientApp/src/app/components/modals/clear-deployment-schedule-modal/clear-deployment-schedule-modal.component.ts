import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { DeploymentScheduleClearDto } from 'src/app/dtos/deployment-schedule-clear-dto.model';

@Component({
  selector: 'app-delete-patchapp-modal',
  templateUrl: './clear-deployment-schedule-modal.component.html',
  styleUrls: ['./clear-deployment-schedule-modal.component.scss'],
})
export class ClearDeploymentScheduleModalComponent implements OnInit {
  @Input() content1: string;
  public dto: DeploymentScheduleClearDto = new DeploymentScheduleClearDto();

  @Output() continue: EventEmitter<DeploymentScheduleClearDto> =
    new EventEmitter<DeploymentScheduleClearDto>();

  constructor(public activeModal: NgbActiveModal) {}

  ngOnInit(): void {
    this.dto.shouldRemovePatchApp = true;
  }

  doContinue() {
    this.continue.emit(this.dto);
    this.activeModal.close();
  }
}
