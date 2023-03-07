import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  Event,
  NavigationEnd,
  Params,
  Router,
} from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { UserRole } from 'src/app/constants/user-role';
import { displayArchitecture } from 'src/app/dtos/application-dto.model';
import { ApprovalDto } from 'src/app/dtos/approval-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { ApprovalService } from 'src/app/services/approval.service';
import { UserStore } from 'src/app/services/user.store';
import { MultipleSelectDirective } from '../multiple-select.directive';

@Component({
  selector: 'app-approvals',
  templateUrl: './approvals.component.html',
})
export class ApprovalsComponent
  extends MultipleSelectDirective<number, ApprovalDto, string>
  implements OnInit, OnDestroy
{
  readonly displayArchitecture = displayArchitecture;
  readonly userRole = UserRole;

  loggedInUser: UserDto;

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  constructor(
    protected readonly approvalsService: ApprovalService,
    protected readonly userStore: UserStore,
    protected readonly router: Router,
    protected readonly activatedRoute: ActivatedRoute,
    protected readonly modalService: NgbModal
  ) {
    super(userStore);
  }

  ngOnInit() {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber')) this.pgNr = params['pageNumber'];
      else this.pgNr = 1;

      this.update();
    });

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          this.approvalsService.getApprovalsPaged(this.pgNr, this.pageSize)
        )
      )
      .subscribe((approvalsPaged: PagedResult<ApprovalDto>) => {
        this.allItemsCount = approvalsPaged.allItemsCount;
        this.pagedItems = approvalsPaged.pageItems;
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
  }

  update() {
    this.behaviorSubject$.next({});
  }

  pageChange() {
    const queryParams: Params = {
      pageNumber: this.pgNr,
    };

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }

  getAllItemIds(): Observable<number[]> {
    return this.approvalsService.getAllApprovalIds();
  }

  applyBulkActions() {
    if (this.selectedOption === 'Approve')
      this.approvalsService
        .approveItems(Array.from(this.selectedItemIds))
        .subscribe(() => {
          this.deselectItems(this.selectedItemIds);
          this.update();
        });
    else if (this.selectedOption === 'Reject') {
      const modalRef = this.modalService.open(ConfirmationModalComponent);
      modalRef.componentInstance.content1 = `Clicking the Continue button will delete the selected approval requests.`;
      modalRef.componentInstance.continue.subscribe(() => {
        this.approvalsService
          .rejectItems(Array.from(this.selectedItemIds))
          .subscribe(() => {
            this.deselectItems(this.selectedItemIds);
            this.update();
          });
      });
    }
  }

  approveDeploy(approvalId: number) {
    this.approvalsService.approveItems([approvalId]).subscribe((_) => {
      this.deselectItems(new Set([approvalId]));
      this.update();
    });
  }

  rejectDeploy(approvalId: number) {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `Clicking the Continue button will delete the approval request.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.approvalsService.rejectItems([approvalId]).subscribe((_) => {
        this.deselectItems(new Set([approvalId]));
        this.update();
      });
    });
  }
}
