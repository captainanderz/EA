import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppRoutes } from './constants/app-routes';
import { ApplicationsComponent } from './pages/applications/applications.component';
import { LoginComponent } from './pages/login/login.component';
import { ApplicationDetailsComponent } from './pages/application-details/application-details.component';
import { RequestsComponent } from './pages/requests/requests.component';
import { AuthGuard } from './guards/auth.guard';
import { SubscriptionCheckComponent } from './pages/subscription-check/subscription-check.component';

const routes: Routes = [
  {
    path: AppRoutes.root,
    redirectTo: AppRoutes.applications,
    pathMatch: 'full',
  },
  {
    path: AppRoutes.applications,
    canActivate: [AuthGuard],
    component: ApplicationsComponent,
    children: [],
  },
  {
    path: AppRoutes.applications + '/:type' + '/:id',
    canActivate: [AuthGuard],
    component: ApplicationDetailsComponent,
  },
  {
    path: AppRoutes.requests,
    canActivate: [AuthGuard],
    component: RequestsComponent,
  },
  {
    path: AppRoutes.login,
    component: LoginComponent,
  },
  {
    path: AppRoutes.subscriptionCheck,
    canActivate: [],
    component: SubscriptionCheckComponent,
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
