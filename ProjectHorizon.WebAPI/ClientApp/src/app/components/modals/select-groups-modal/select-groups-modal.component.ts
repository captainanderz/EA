import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  switchMap,
  tap,
} from 'rxjs/operators';
import { GroupDto } from 'src/app/dtos/group-dto.model';
import { GraphConfigService } from 'src/app/services/graph-config.service';
import { GroupService } from 'src/app/services/group.service';

@Component({
  selector: 'app-select-groups-modal',
  templateUrl: './select-groups-modal.component.html',
  styleUrls: ['./select-groups-modal.component.scss'],
})
export class SelectGroupsModalComponent implements OnInit, OnDestroy {
  selectedGroupId: string | undefined = undefined;
  selectedGroupName: string | undefined = undefined;
  selectedGroupIndex: number | undefined = undefined;
  filteredGroups: ReadonlyArray<GroupDto> = [];

  errorMessage: string | undefined = undefined;

  selectedGroups: Array<GroupDto> = [];
  nextPageLink: string | undefined = '';

  private readonly groupNameTyped = new BehaviorSubject<
    [name: string, nextPageLink: string | undefined]
  >(['', '']);
  private readonly dueTime = 300;

  constructor(
    private readonly graphConfigService: GraphConfigService,
    private readonly groupService: GroupService,
    private readonly activeModal: NgbActiveModal
  ) {}

  ngOnInit(): void {
    this.graphConfigService.hasGraphConfig().subscribe(
      (hasGraphConfig) => {
        if (hasGraphConfig) {
          this.groupNameTyped
            .pipe(
              // wait 300ms after each keystroke before considering the term
              debounceTime(this.dueTime),

              tap(([groupName, nextPageLink]) => {
                if (this.selectedGroupName != groupName) {
                  this.filteredGroups = [];
                  this.selectedGroupName = groupName;
                  this.nextPageLink = '';
                }
              }),

              // switch to new search observable each time the term changes
              switchMap(([groupName, nextPageLink]) =>
                this.groupService.filterGroupsByName(groupName, nextPageLink)
              )
            )
            .subscribe((pagedResult) => {
              this.filteredGroups = this.filteredGroups.concat(
                pagedResult.pageItems
              );

              this.nextPageLink = pagedResult.nextPageLink;
            });
        } else {
          this.showError();
        }
      },
      (_) => this.showError()
    );
  }

  ngOnDestroy(): void {
    this.clearSelectedGroup();
  }

  showError() {
    this.errorMessage =
      'Error while trying to load the groups. Make sure the integration with Microsoft Endpoint Manager has been configured properly.';
  }

  isSelected(group: GroupDto) {
    return this.selectedGroups.some(
      (selectedGroup) => selectedGroup.id === group.id
    );
  }

  onInput(name: string) {
    this.groupNameTyped.next([name, undefined]);

    this.selectedGroupIndex = undefined;
    this.selectedGroupId = undefined;
  }

  onKeyDown(event: KeyboardEvent) {
    const selectedIndex = this.selectedGroupIndex;
    const length = this.filteredGroups.length;

    switch (event.key) {
      case 'ArrowUp':
        this.selectedGroupIndex =
          selectedIndex === undefined || selectedIndex! <= 0
            ? length - 1
            : selectedIndex - 1;
        break;
      case 'ArrowDown':
        this.selectedGroupIndex =
          length === undefined || selectedIndex! >= length - 1
            ? 0
            : selectedIndex! + 1;
        break;
      case 'Enter':
        event.preventDefault();
        if (selectedIndex !== undefined) this.selectGroup(selectedIndex);
        break;
      case 'Escape':
        event.preventDefault();
        this.clearSelectedGroup();
        break;
    }
  }

  selectGroup(index: number) {
    const selectedGroup = this.filteredGroups[index];

    if (this.selectedGroups.some((group) => group.id === selectedGroup.id)) {
      this.selectedGroups = this.selectedGroups.filter(
        (group) => group.id !== selectedGroup.id
      );
    } else {
      this.selectedGroups.push(selectedGroup);
    }
  }

  removeGroup(id: string) {
    const selectedGroup = this.selectedGroups.find((group) => group.id === id);

    if (!selectedGroup) return;

    this.selectedGroups = this.selectedGroups.filter(
      (group) => group.id !== id
    );
  }

  clearSelectedGroup() {
    this.selectedGroupName = undefined;
  }

  onScroll() {
    if (!this.nextPageLink) {
      return;
    }

    this.groupNameTyped.next([this.selectedGroupName || '', this.nextPageLink]);
  }

  passBack() {
    this.clearSelectedGroup();
    this.activeModal.close(this.selectedGroups);
  }

  close() {
    this.clearSelectedGroup();
    this.activeModal.close();
  }
}
