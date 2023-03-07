import { Component } from '@angular/core';
import { UserStore } from 'src/app/services/user.store';
import { AssignmentProfileService } from 'src/app/services/assignment-profile.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { AssignmentProfileDto } from 'src/app/dtos/assignment-profile-dto.model';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { MultipleSelectDirective } from '../multiple-select.directive';
import { UserRole } from 'src/app/constants/user-role';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { BehaviorSubject, combineLatest, Observable, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { AppRoutes } from 'src/app/constants/app-routes';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-assignment-profiles',
  templateUrl: './assignment-profiles.component.html',
  providers: [
    {
      provide: ApplicationService,
      useClass: AssignmentProfileService,
    },
  ],
})
export class AssignmentProfilesComponent extends MultipleSelectDirective<
  number,
  AssignmentProfileDto,
  string
> {
  readonly appRoutes = AppRoutes;

  readonly userRole = UserRole;

  loggedInUser: UserDto;
  searchTerm = '';

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  constructor(
    protected readonly assignmentService: AssignmentProfileService,
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
            this.assignmentService.getAssignmentsPaged(
              this.pgNr,
              this.pageSize,
              this.searchTerm
            ),
            this.assignmentService.getAllAssignmentProfilesIds(),
          ])
        )
      )
      .subscribe(([assignmentsPaged, assignmentProfileIds]) => {
        this.allItemsCount = assignmentsPaged.allItemsCount;
        this.pagedItems = assignmentsPaged.pageItems;

        this.selectedItemIds = new Set(
          [...this.selectedItemIds].filter((itemId) =>
            assignmentProfileIds.includes(itemId)
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
    return this.assignmentService.getAllAssignmentProfilesIds();
  }

  startMultipleAssignmentProfileDelete() {
    this.deleteAssignments(Array.from(this.selectedItemIds));
  }

  startMultipleAssignmentProfileCopy() {
    this.copyAssignments(Array.from(this.selectedItemIds));
  }

  applyBulkActions() {
    if (this.selectedOption == 'Copy') {
      this.startMultipleAssignmentProfileCopy();
    }
    if (this.selectedOption == 'Delete') {
      this.startMultipleAssignmentProfileDelete();
    }
  }

  deselectDeletedItems(assignmentIds: Set<number>) {
    assignmentIds.forEach((assignmentId) => {
      if (this.isItemSelected(assignmentId))
        this.toggleSelectItem(assignmentId);
    });
  }

  copyAssignment(assignmentId: number) {
    this.assignmentService
      .copyAssignmentProfiles([assignmentId])
      .subscribe(() => {
        this.update();
      });
  }

  deleteAssignments(assignmentIds: Array<number>) {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 =
      'Clicking the Continue button will delete the selected assignment profiles.';
    modalRef.componentInstance.continue.subscribe(() => {
      this.assignmentService
        .deleteAssignmentProfiles(assignmentIds)
        .subscribe(() => {
          this.deselectItems(new Set(assignmentIds));
          this.update();
        });
    });
  }

  copyAssignments(assignmentIds: Array<number>) {
    this.assignmentService
      .copyAssignmentProfiles(assignmentIds)
      .subscribe(() => {
        this.update();
      });
  }
}
