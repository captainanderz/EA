import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApplicationDetailsDto } from 'src/app/dtos/application-details-dto.model';
import { ApplicationDto } from 'src/app/dtos/application-dto';
import { ApplicationsService } from 'src/app/services/applications.service';
import { RequestState } from 'src/app/dtos/request-state.model';

@Component({
  selector: 'app-application-details',
  templateUrl: './application-details.component.html',
  styleUrls: ['./application-details.component.scss'],
})
export class ApplicationDetailsComponent implements OnInit {
  // Fields
  RequestState = RequestState;
  application: ApplicationDetailsDto | undefined;

  @Output()
  onRequest: EventEmitter<ApplicationDto> = new EventEmitter();

  // Constructor
  constructor(
    private route: ActivatedRoute,
    private service: ApplicationsService,
    private readonly router: Router
  ) {}

  // Methods
  // Get the application details so it can render them in the details page
  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      const id = params['id'];
      const isPrivate = params['type'] == 'private';

      if (id) {
        this.service.getDetails(id, isPrivate).subscribe((application) => {
          this.application = application;
        });
      }
    });
  }

  // Handles the request button action
  request(): void {
    this.onRequest.emit(this.application);
  }

  // Handles the cancel button logic, it closes the modal and redirects the user to the last page he was before opening the modal
  onCancel() {
    this.router.navigate(['../..'], { relativeTo: this.route });
  }
}
