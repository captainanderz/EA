import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, debounceTime, Subject, switchMap } from 'rxjs';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { RequestState } from 'src/app/dtos/request-state.model';
import { RequestDto } from 'src/app/dtos/request-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { RequestsService } from 'src/app/services/requests.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-requests',
  templateUrl: './requests.component.html',
  styleUrls: ['./requests.component.scss'],
})
export class RequestsComponent implements OnInit {
  // Fields
  RequestState = RequestState;
  searchTerm = '';

  requests: ReadonlyArray<RequestDto> = [];

  pageNumber = 1;
  pageSize = 10;
  allItemsCount = 0;
  stateFilter = RequestState.Pending;

  loggedInUser: UserDto;

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  // Constructor
  constructor(
    protected readonly requestsService: RequestsService,
    protected readonly router: Router,
    protected readonly route: ActivatedRoute,
    protected readonly modalService: NgbModal,
    protected readonly userStore: UserStore
  ) {}

  // Methods
  ngOnInit(): void {
    // Pagination logic
    this.route.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber'))
        this.pageNumber = params['pageNumber'];
      else this.pageNumber = 1;

      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';

      this.update();
    });

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          this.requestsService.getPaged(
            this.pageNumber,
            this.pageSize,
            this.searchTerm,
            this.stateFilter
          )
        )
      )
      .subscribe((requestsPaged: PagedResult<RequestDto>) => {
        this.allItemsCount = requestsPaged.allItemsCount;
        this.requests = requestsPaged.pageItems;
      });
  }

  // Apply the last changes
  update() {
    this.behaviorSubject$.next({});
  }

  // Checks if the request is in pending state so it is enabled to changes
  isEnabled(request: RequestDto) {
    return request.stateId == RequestState.Pending;
  }

  // Handles the logic for approving a request
  onApproveRequest(request: RequestDto) {
    this.requestsService.approve(request).subscribe(() => {
      this.update();
    });
  }

  // Handles the logic of rejecting a request
  onRejectRequest(request: RequestDto) {
    this.requestsService.reject(request).subscribe(() => {
      this.update();
    });
  }

  // Go to another page applying the search filter too when changing the pages
  pageChange() {
    const queryParams: Params = {
      pageNumber: this.pageNumber,
    };

    if (this.searchTerm.trim() !== '')
      queryParams['searchTerm'] = this.searchTerm;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
    });
  }
}
