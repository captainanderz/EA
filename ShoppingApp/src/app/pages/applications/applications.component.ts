import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { BehaviorSubject, debounceTime, switchMap } from 'rxjs';
import { AppRoutes } from 'src/app/constants/app-routes';
import { ApplicationDto } from 'src/app/dtos/application-dto';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { RequestState } from 'src/app/dtos/request-state.model';
import { ApplicationsService } from 'src/app/services/applications.service';

@Component({
  selector: 'app-applications',
  templateUrl: './applications.component.html',
  styleUrls: ['./applications.component.scss'],
})
export class ApplicationsComponent implements OnInit {
  // Fields
  readonly appRoutes = AppRoutes;
  applications: ReadonlyArray<ApplicationDto> = [];
  searchTerm = '';

  pageNumber = 1;
  pageSize = 6;
  allItemsCount = 0;
  stateFilter = RequestState.NotSet;

  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  // Constructor
  public constructor(
    protected readonly applicationsService: ApplicationsService,
    protected readonly route: ActivatedRoute,
    protected readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  // Methods
  public ngOnInit(): void {
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
          this.applicationsService.getPaged(
            this.pageNumber,
            this.pageSize,
            this.searchTerm,
            this.stateFilter
          )
        )
      )
      .subscribe((applicationsPaged: PagedResult<ApplicationDto>) => {
        this.allItemsCount = applicationsPaged.allItemsCount;
        this.applications = applicationsPaged.pageItems;
      });
  }

  // Search bar logic
  public onSearch() {
    const queryParams: Params = {};

    if (this.searchTerm.trim() !== '')
      queryParams['searchTerm'] = this.searchTerm;

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }

  public onSelectState() {
    this.update();
  }

  // Handles the request button action
  public onRequest(applicationDto: ApplicationDto) {
    this.applicationsService.request(applicationDto).subscribe(() => {
      this.update();
    });
  }

  // Search an application by it's publisher
  public onPublisherSearch(applicationPublisher: string) {
    const queryParams: Params = {};

    if (this.searchTerm.trim() !== '')
      queryParams['searchTerm'] = applicationPublisher;

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }

  // Apply the last changes
  update() {
    this.behaviorSubject$.next({});
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
