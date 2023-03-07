import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Settings } from 'src/app/constants/settings';
import { AssignmentProfileDto } from 'src/app/dtos/assignment-profile-dto.model';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';

@Component({
  selector: 'app-assign-profile-modal',
  templateUrl: './assign-profile-modal.component.html',
  styleUrls: ['./assign-profile-modal.component.scss'],
})
export class AssignProfileModalComponent implements OnInit {
  Message: string | undefined = undefined;
  filteredAssignmentProfiles: ReadonlyArray<AssignmentProfileDto> = [];
  selectedAssignmentProfile: AssignmentProfileDto | undefined;
  assignmentProfileNameTyped = new BehaviorSubject<string>('');
  selectedAssignmentProfileId: string | undefined = undefined;
  selectedAssignmentProfileName: string | undefined = undefined;
  selectedAssignmentProfileIndex: number | undefined = undefined;

  constructor(
    private readonly assignmentProfileService: AssignmentProfileService,
    private readonly activeModal: NgbActiveModal
  ) {}

  ngOnInit(): void {
    this.assignmentProfileNameTyped
      .pipe(
        debounceTime(Settings.debounceTimeMs),
        // ignore new term if same as previous term
        distinctUntilChanged(),

        // switch to new search observable each time the term changes
        switchMap((name) =>
          this.assignmentProfileService.filterAssignmentsByName(name)
        )
      )
      .subscribe(
        (assignmentProfiles) => {
          this.filteredAssignmentProfiles = assignmentProfiles;
        },
        (_) => this.showMessage()
      );
  }

  ngOnDestroy(): void {
    this.clearSelectedAssignmentProfiles();
  }

  clearSelectedAssignmentProfiles() {
    this.onInput('');
    this.selectedAssignmentProfileName = undefined;
  }

  onInput(name: string) {
    this.assignmentProfileNameTyped.next(name);

    this.selectedAssignmentProfileIndex = undefined;
    this.selectedAssignmentProfileId = undefined;
  }

  selectAssignmentProfile(index: number) {
    this.selectedAssignmentProfile = this.filteredAssignmentProfiles[index];
  }

  isSelected(assignmentProfile: AssignmentProfileDto) {
    return (
      this.selectedAssignmentProfile &&
      assignmentProfile.id == this.selectedAssignmentProfile.id
    );
  }

  passBack() {
    this.activeModal.close(this.selectedAssignmentProfile);
  }

  filterItem(value: string) {}

  onChangeofOptions(newOption: string) {
    console.log(newOption);
  }

  showMessage() {
    this.Message = 'No assignment profiles';
  }

  close() {
    this.activeModal.close();
  }
}
