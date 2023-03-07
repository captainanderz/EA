import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import {
  AssignmentProfileGroupDto,
  GroupMode,
  TemporaryAssignmentProfileGroupDto,
} from 'src/app/dtos/assignment-profile-group-dto';
import { GroupDto } from 'src/app/dtos/group-dto.model';
import { NewAssignmentProfileDto } from 'src/app/dtos/new-assignment-profile-dto.model';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';
import { SelectGroupsModalComponent } from '../../components/modals/select-groups-modal/select-groups-modal.component';

enum AssignmentType {
  Required = 1,
  Available = 2,
  Uninstall = 3,
}

enum Action {
  New = 'new',
  Edit = 'edit',
  Details = 'details',
}

@Component({
  selector: 'app-assignment-profile',
  templateUrl: './assignment-profiles-details.component.html',
  styleUrls: ['./assignment-profiles-details.component.scss'],
  providers: [
    {
      provide: ApplicationService,
      useClass: AssignmentProfileService,
    },
  ],
})
export class AssignmentProfileDetailsComponent implements OnInit, OnDestroy {
  id: number | undefined;
  action: Action = Action.New;
  assignmentProfileName: string | undefined;
  name: string;
  requiredGroups: TemporaryAssignmentProfileGroupDto[] = [];
  availableGroups: TemporaryAssignmentProfileGroupDto[] = [];
  uninstallGroups: TemporaryAssignmentProfileGroupDto[] = [];
  assignmentTypes: AssignmentType[] = [
    AssignmentType.Required,
    AssignmentType.Available,
    AssignmentType.Uninstall,
  ];
  errorMessage: string | undefined = undefined;

  public AssignmentType = AssignmentType;
  public GroupMode = GroupMode;
  public Action = Action;

  constructor(
    private readonly modalService: NgbModal,
    private readonly assignmentProfileService: AssignmentProfileService,
    private location: Location,
    private route: ActivatedRoute
  ) {}

  ngOnDestroy(): void {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.id = params['id'];

      if (this.id) {
        this.assignmentProfileService
          .getAssignmentProfileById(this.id)
          .subscribe(
            (assignmentProfile) => {
              this.assignmentProfileName = assignmentProfile.name;

              this.requiredGroups = assignmentProfile.groups
                .filter(
                  (group) => group.assignmentTypeId == AssignmentType.Required
                )
                .map((group) => new TemporaryAssignmentProfileGroupDto(group));

              this.availableGroups = assignmentProfile.groups
                .filter(
                  (group) => group.assignmentTypeId == AssignmentType.Available
                )
                .map((group) => new TemporaryAssignmentProfileGroupDto(group));

              this.uninstallGroups = assignmentProfile.groups
                .filter(
                  (group) => group.assignmentTypeId == AssignmentType.Uninstall
                )
                .map((group) => new TemporaryAssignmentProfileGroupDto(group));
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

  assignmentTypeToString(assignmentType: AssignmentType): string {
    return AssignmentType[assignmentType];
  }

  onSubmit() {
    if (
      this.assignmentProfileName == '' ||
      this.assignmentProfileName == null
    ) {
      this.showError();
      return;
    }

    const temporaryGroups = this.getAllGroups();

    const groups = temporaryGroups.map(
      (group) => new AssignmentProfileGroupDto(group)
    );

    const assignmentProfile: NewAssignmentProfileDto = {
      name: this.assignmentProfileName,
      groups: groups,
    };

    let observable =
      this.assignmentProfileService.addAssignmentProfile(assignmentProfile);

    if (this.id) {
      observable = this.assignmentProfileService.editAssignmentProfile(
        this.id,
        assignmentProfile
      );
    }

    observable.subscribe((_) => {
      this.location.back();
    });
  }

  onCancel() {
    this.location.back();
  }

  onGroupModeChange(
    azureGroupId: string,
    assignmentType: AssignmentType,
    newValue: GroupMode
  ) {
    const groups = this.getAllGroups();

    const selectedGroup = groups.find(
      (group) =>
        group.azureGroupId == azureGroupId &&
        group.assignmentTypeId == assignmentType
    );

    if (!selectedGroup) {
      return;
    }

    if (newValue == GroupMode.Included) {
      groups
        .filter((group) => group.azureGroupId == azureGroupId)
        .forEach((group) => (group.groupModeId = GroupMode.Excluded));
    }

    selectedGroup.groupModeId = newValue;
  }

  showError() {
    this.errorMessage = 'The assignment profile name is required';
  }

  getGroups(assignmentType: AssignmentType) {
    switch (assignmentType) {
      case AssignmentType.Required: {
        return this.requiredGroups;
      }
      case AssignmentType.Available: {
        return this.availableGroups;
      }
      case AssignmentType.Uninstall: {
        return this.uninstallGroups;
      }
    }
  }

  isChecked(azureGroupId: string, assignmentType: AssignmentType): boolean {
    const groups = this.getAllGroups();

    const selectedGroup = groups.find(
      (group) =>
        group.azureGroupId == azureGroupId &&
        group.assignmentTypeId == assignmentType
    );

    return selectedGroup?.groupModeId == GroupMode.Included;
  }

  removeGroup(category: AssignmentType, id: string) {
    switch (category) {
      case AssignmentType.Required: {
        const selectedGroup = this.requiredGroups.find(
          (group) => group.azureGroupId === id
        );

        if (!selectedGroup) return;

        this.requiredGroups = this.requiredGroups.filter(
          (group) => group.azureGroupId !== id
        );
        return this.requiredGroups;
      }
      case AssignmentType.Available: {
        const selectedGroup = this.availableGroups.find(
          (group) => group.azureGroupId === id
        );

        if (!selectedGroup) return;

        this.availableGroups = this.availableGroups.filter(
          (group) => group.azureGroupId !== id
        );
        return this.availableGroups;
      }
      case AssignmentType.Uninstall: {
        const selectedGroup = this.uninstallGroups.find(
          (group) => group.azureGroupId === id
        );

        if (!selectedGroup) return;

        this.uninstallGroups = this.uninstallGroups.filter(
          (group) => group.azureGroupId !== id
        );
        return this.uninstallGroups;
      }
    }
  }

  removeGroupByGroupMode(assignmentType: AssignmentType, groupModeId: string) {
    const groups = this.getGroups(assignmentType);

    groups.forEach((group, index) => {
      if (group.groupModeId == groupModeId) {
        groups.splice(index, 1);
      }
    });
  }

  // Compare the 2 arrays : one that the user creates by adding groups, and the one on the popup with all the groups
  // already selected, than add on the popup only those groups that were not added already. No duplicate groups will be
  // present on the Create new Assignment Profile popup.
  concatSet(
    set: TemporaryAssignmentProfileGroupDto[],
    items: TemporaryAssignmentProfileGroupDto[]
  ) {
    const newElements = items.filter(
      (group) =>
        !set.some((setGroup) => setGroup.azureGroupId === group.azureGroupId)
    );

    return set
      .concat(newElements)
      .sort((group1, group2) =>
        group1.displayName < group2.displayName ? -1 : 1
      );
  }

  getAllGroups() {
    return this.requiredGroups
      .concat(this.availableGroups)
      .concat(this.uninstallGroups);
  }

  checkGroupModeExists(groupModeId: GroupMode): boolean {
    const temporaryGroups = this.getAllGroups();

    return temporaryGroups.some((group) => group.groupModeId == groupModeId);
  }

  checkGroupIdExists(azureGroupId: string): boolean {
    const temporaryGroups = this.getAllGroups();

    return temporaryGroups.some((group) => group.azureGroupId == azureGroupId);
  }

  addAllUsers(assignmentType: AssignmentType) {
    if (this.checkGroupModeExists(GroupMode.AllUsers)) {
      return;
    }

    const groups = (() => {
      switch (assignmentType) {
        case AssignmentType.Required: {
          return this.requiredGroups;
        }
        case AssignmentType.Available: {
          return this.availableGroups;
        }
        case AssignmentType.Uninstall: {
          return this.uninstallGroups;
        }
      }
    })();

    groups.push({
      azureGroupId: null,
      displayName: 'AllUsers',
      assignmentTypeId: assignmentType,
      endUserNotificationId: '1',
      deliveryOptimizationPriorityId: '1',
      groupModeId: GroupMode.AllUsers,
    });
  }

  addAllDevices(assignmentType: AssignmentType) {
    if (this.checkGroupModeExists(GroupMode.AllDevices)) {
      return;
    }

    const groups = (() => {
      switch (assignmentType) {
        case AssignmentType.Required: {
          return this.requiredGroups;
        }
        case AssignmentType.Available: {
          return this.availableGroups;
        }
        case AssignmentType.Uninstall: {
          return this.uninstallGroups;
        }
      }
    })();

    groups.push({
      azureGroupId: null,
      displayName: 'AllDevices',
      assignmentTypeId: assignmentType,
      endUserNotificationId: '1',
      deliveryOptimizationPriorityId: '1',
      groupModeId: GroupMode.AllDevices,
    });
  }

  openAddGroupModal(category: AssignmentType) {
    const modalRef = this.modalService.open(SelectGroupsModalComponent, {
      size: 'lg',
      backdrop: 'static',
      keyboard: true,
    });

    modalRef.result.then((arrayGroupThatUserSelected) => {
      if (arrayGroupThatUserSelected) {
        // Map the array of GroupDto in AssignmentProfileGroupDto, and selects as default value the first option for EndUserNotifications
        // and DeliveryOptimizationPriority
        const mappedSelectedGroups: TemporaryAssignmentProfileGroupDto[] =
          arrayGroupThatUserSelected.map((group: GroupDto) => {
            const assignmentProfileGroup: TemporaryAssignmentProfileGroupDto = {
              azureGroupId: group.id,
              displayName: group.displayName,
              assignmentTypeId: 0,
              endUserNotificationId: '1',
              deliveryOptimizationPriorityId: '1',
              groupModeId: this.checkGroupIdExists(group.id)
                ? GroupMode.Excluded
                : GroupMode.Included,
            };

            return assignmentProfileGroup;
          });

        switch (category) {
          case AssignmentType.Required: {
            mappedSelectedGroups.forEach(
              (group) => (group.assignmentTypeId = AssignmentType.Required)
            );

            this.requiredGroups = this.concatSet(
              this.requiredGroups,
              mappedSelectedGroups
            );

            break;
          }
          case AssignmentType.Available: {
            mappedSelectedGroups.forEach(
              (group) => (group.assignmentTypeId = AssignmentType.Available)
            );

            this.availableGroups = this.concatSet(
              this.availableGroups,
              mappedSelectedGroups
            );
            break;
          }
          case AssignmentType.Uninstall: {
            mappedSelectedGroups.forEach(
              (group) => (group.assignmentTypeId = AssignmentType.Uninstall)
            );

            this.uninstallGroups = this.concatSet(
              this.uninstallGroups,
              mappedSelectedGroups
            );
            break;
          }
        }
      }
    });
  }
}
