import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  NavigationEnd,
  Params,
  Router,
  Event,
} from '@angular/router';
import { NgbCalendar, NgbDate } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import {
  AuditLogCategories,
  AuditLogCategory,
} from 'src/app/constants/audit-log-category';
import { AuditLogDto } from 'src/app/dtos/audit-log-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { AuditLogService } from 'src/app/services/audit-log.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-audit-log',
  templateUrl: './audit-log.component.html',
  styleUrls: ['./audit-log.component.scss'],
})
export class AuditLogComponent implements OnInit, OnDestroy {
  readonly pageSize = 20;
  readonly allCategories = AuditLogCategories;

  pgNr = 1;
  allItemsCount = 0;
  category: AuditLogCategory = 'All Categories';
  searchTerm = '';

  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = null;
  toDate: NgbDate | null = null;
  auditLogs: ReadonlyArray<AuditLogDto> = [];

  private readonly unsubscribe$ = new Subject<void>();
  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});

  constructor(
    private readonly calendar: NgbCalendar,
    private readonly auditLogService: AuditLogService,
    private readonly userStore: UserStore,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  ngOnInit() {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber')) this.pgNr = params['pageNumber'];
      else this.pgNr = 1;

      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';

      this.update();
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
      .subscribe((_) => this.update());

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          this.auditLogService.getPaged(
            this.pgNr,
            this.pageSize,
            this.searchTerm,
            this.fromDate,
            this.toDate,
            this.category
          )
        )
      )
      .subscribe((auditLogsPaged: PagedResult<AuditLogDto>) => {
        this.auditLogs = auditLogsPaged.pageItems;
        this.allItemsCount = auditLogsPaged.allItemsCount;
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
    this.behaviorSubject$.complete();
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

  onDateSelection(date: NgbDate) {
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (
      this.fromDate &&
      !this.toDate &&
      date &&
      date.after(this.fromDate)
    ) {
      this.toDate = date;
    } else if (this.fromDate?.equals(date) && !this.toDate) {
      this.fromDate = date;
      this.toDate = date;
    } else if (this.fromDate?.equals(date) && this.toDate?.equals(date)) {
      this.fromDate = null;
      this.toDate = null;
    } else {
      this.fromDate = date;
      this.toDate = null;
    }

    this.update();
  }

  isHovered(date: NgbDate) {
    return (
      this.fromDate &&
      !this.toDate &&
      this.hoveredDate &&
      date.after(this.fromDate) &&
      date.before(this.hoveredDate)
    );
  }

  isInside(date: NgbDate) {
    return this.toDate && date.after(this.fromDate) && date.before(this.toDate);
  }

  isRange(date: NgbDate) {
    return (
      date.equals(this.fromDate) ||
      (this.toDate && date.equals(this.toDate)) ||
      this.isInside(date) ||
      this.isHovered(date)
    );
  }

  tryUpdateFromDate(input: string) {
    const result = this.validateInput(this.fromDate, input);
    if (result !== this.fromDate) {
      this.fromDate = result;
      this.update();
    }
  }

  tryUpdateToDate(input: string) {
    const result = this.validateInput(this.toDate, input);
    if (result !== this.toDate) {
      this.toDate = result;
      this.update();
    }
  }

  format(date: NgbDate | null): string | null {
    if (!date) return null;
    return `${date.day}/${date.month}/${date.year}`;
  }

  downloadCsv() {
    this.auditLogService
      .getCsv(this.fromDate, this.toDate, this.searchTerm, this.category)
      .subscribe((blob) => {
        const dataUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = dataUrl;
        link.download = 'audit-log.csv';
        link.click();
        window.URL.revokeObjectURL(dataUrl);
      });
  }

  private validateInput(
    currentValue: NgbDate | null,
    input: string | null
  ): NgbDate | null {
    if (!input || input === '') return null;

    const parseRegex = /^(\d{0,2})\/(\d{0,2})\/(\d{4})/;
    const match = input.match(parseRegex);

    if (!match) return currentValue;

    const day = Number(match[1]);
    const month = Number(match[2]);
    const year = Number(match[3]);

    if (
      0 < day &&
      day <= 31 &&
      0 < month &&
      month <= 12 &&
      2000 < year &&
      year <= 3000
    ) {
      const date = new NgbDate(year, month, day);

      if (this.calendar.isValid(date)) return date;
    }

    return currentValue;
  }
}
