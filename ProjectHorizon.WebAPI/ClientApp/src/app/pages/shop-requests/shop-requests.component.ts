import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, switchMap, takeUntil, filter } from 'rxjs/operators';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { UserRole } from 'src/app/constants/user-role';
import { AssignmentProfileDto } from 'src/app/dtos/assignment-profile-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { RequestState } from 'src/app/dtos/request-state.model';
import { ShopRequestDto } from 'src/app/dtos/shop-request-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { PrivateShoppingService } from 'src/app/services/shopping-services/private-shopping.service';
import { ShoppingRequestsService } from 'src/app/services/shopping-services/shopping-requests.service';
import { ShoppingService } from 'src/app/services/shopping-services/shopping.service';
import { UserStore } from 'src/app/services/user.store';
import { MultipleSelectDirective } from '../multiple-select.directive';

@Component({
  selector: 'app-shop-requests',
  templateUrl: './shop-requests.component.html',
  styleUrls: ['./shop-requests.component.scss'],
})
export class ShopRequestsComponent implements OnInit {
  RequestState = RequestState;
  stateFilter = RequestState.Pending;

  readonly userRole = UserRole;
  loggedInUser: UserDto;
  searchTerm = '';

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});
  allItemsCount: number;
  pagedItems: ShopRequestDto[];
  pgNr: number = 1;
  pageSize: number = 20;

  constructor(
    protected readonly shopRequestsService: ShoppingRequestsService,
    protected readonly router: Router,
    protected readonly activatedRoute: ActivatedRoute,
    protected readonly modalService: NgbModal,
    protected readonly userStore: UserStore
  ) {}

  ngOnInit(): void {
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
          this.shopRequestsService.getRequestsPaged(
            this.pgNr,
            this.pageSize,
            this.searchTerm,
            this.stateFilter
          )
        )
      )
      .subscribe((itemsPaged: PagedResult<ShopRequestDto>) => {
        this.allItemsCount = itemsPaged.allItemsCount;
        this.pagedItems = itemsPaged.pageItems;
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

  update() {
    this.behaviorSubject$.next({});
  }

  onSelectState() {
    this.update();
  }

  isEnabled(request: ShopRequestDto) {
    return request.stateId == RequestState.Pending;
  }

  getState(request: ShopRequestDto) {
    return RequestState[request.stateId];
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

  onApproveRequest(request: ShopRequestDto) {
    this.shopRequestsService.approveRequest(request).subscribe(() => {
      this.update();
    });
  }

  onRejectRequest(request: ShopRequestDto) {
    this.shopRequestsService.rejectRequest(request).subscribe(() => {
      this.update();
    });
  }
}
