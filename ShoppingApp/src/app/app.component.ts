import {
  ChangeDetectorRef,
  Component,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AppRoutes } from './constants/app-routes';
import { LoaderService } from './services/loader.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  // Fields
  title = 'ShoppingApp';
  templateRefExp: TemplateRef<any>;
  overlayVisible = false;

  @ViewChild('loggedIn') loggedInTemplate: TemplateRef<any>;
  @ViewChild('notLoggedIn') notLoggedInTemplate: TemplateRef<any>;

  private readonly notLoggedInPages = [
    AppRoutes.login,
    AppRoutes.subscriptionCheck,
  ];

  // Constructor
  constructor(
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly loaderService: LoaderService
  ) {}

  // Methods
  ngOnInit(): void {
    this.loaderService.httpProgress().subscribe((isLoadingInProgress) => {
      this.overlayVisible = isLoadingInProgress;
      this.changeDetectorRef.detectChanges();
    });

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        const firstSegment = this.activatedRoute.firstChild?.snapshot.url ?? [];

        const match = (boundData: string[]) =>
          boundData.some((p) => firstSegment.some((s) => s.toString() == p));

        const yes = match(this.notLoggedInPages);
        this.templateRefExp = match(this.notLoggedInPages)
          ? this.notLoggedInTemplate
          : this.loggedInTemplate;
      });
  }
}
