import { Component, EventEmitter, OnDestroy, Output } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpEventType,
} from '@angular/common/http';
import { Subscription } from 'rxjs';
import { UploadState } from '../../../constants/upload-state';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { Version } from '@angular/compiler';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import { $ } from 'protractor';
import { compare } from 'compare-versions';

@Component({
  selector: 'app-application-upload-modal',
  templateUrl: './application-upload-modal.component.html',
})
export class ApplicationUploadModalComponent<
  TApplication extends ApplicationDto
> implements OnDestroy
{
  @Output() applicationAddStarted = new EventEmitter<undefined>();

  readonly uploadState = UploadState;
  protected readonly uploadFileSizeLimit = 8589934592 /*8 Gigabytes*/;
  readonly sizeUnit = 'MB';
  invalidFileErrorMessage = '';

  step = 1;
  currentUploadState = UploadState.NotStarted;

  progress = 0;
  loaded = 0;
  total = 0;

  uploadAreaLayer = 0;

  fileName: string;
  applicationInfo: TApplication | null = null;
  subscription: Subscription | null = null;

  constructor(
    protected readonly applicationService: ApplicationService<TApplication>,
    public readonly activeModal: NgbActiveModal,
    protected readonly modalService: NgbModal
  ) {}

  ngOnDestroy() {
    this.clear();
  }

  allowDrop(event: DragEvent) {
    event.preventDefault();
  }

  onUploadAreaEnter(event: DragEvent) {
    if (!(event.target instanceof HTMLElement)) return;

    const targetedHtmlElement = event.target;
    this.uploadAreaLayer++;
    if (targetedHtmlElement.className.includes('upload-area'))
      targetedHtmlElement.style.border = '2px dashed blue';
  }

  onUploadAreaLeave(event: DragEvent) {
    if (!(event.target instanceof HTMLElement)) return;

    const targetedHtmlElement = event.target;
    this.uploadAreaLayer--;
    if (
      targetedHtmlElement.className.includes('upload-area') &&
      this.uploadAreaLayer == 0
    )
      targetedHtmlElement.style.border = '';
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    if (!(event.target instanceof HTMLElement)) return;

    const targetedHtmlElement = event.target;
    if (
      targetedHtmlElement.className.includes('upload-area') ||
      this.uploadAreaLayer > 0
    ) {
      targetedHtmlElement.style.border = '';

      const fileDropped = event.dataTransfer?.files.item(0);
      this.startUpload(fileDropped);
    }
  }

  toggleStep(step: 1 | 2) {
    this.step = step;
  }

  onUploadFile(event: Event) {
    if (!(event.target instanceof HTMLInputElement)) return;

    const applicationToUpload = event.target.files?.item(0);
    this.startUpload(applicationToUpload);
  }

  private getNrMegaBytes(nrBytes: number): number {
    return Math.floor(nrBytes / 1024 / 1024);
  }

  private startUpload(application: File | null | undefined) {
    this.invalidFileErrorMessage = '';

    if (!this.validateApplication(application)) return;

    const applicationToUpload = application as File;
    this.currentUploadState = UploadState.Started;
    this.fileName = applicationToUpload.name;
    this.total = this.getNrMegaBytes(applicationToUpload.size);

    this.subscription = this.applicationService
      .uploadApplication(applicationToUpload)
      .subscribe(
        (event: HttpEvent<ApplicationDto>) => {
          switch (event.type) {
            case HttpEventType.UploadProgress:
              this.progress = Math.floor(
                (99 * event.loaded) / (event.total ?? applicationToUpload.size)
              );
              this.loaded = this.getNrMegaBytes(event.loaded);
              break;
            case HttpEventType.Response:
              this.progress = 100;
              this.currentUploadState = UploadState.Finished;
              this.applicationInfo = event.body as TApplication;
              break;
          }
        },
        (error: HttpErrorResponse) => {
          switch (error.status) {
            case 400:
              this.invalidFileErrorMessage = error.error;
              break;

            case 500:
              this.invalidFileErrorMessage =
                'Upload failed. Invalid upload file!';
              break;

            default:
              this.invalidFileErrorMessage =
                'Unknown Error, please check your internet connection or if the service is available!';
          }

          this.clear();
        }
      );
  }

  private validateApplication(application: File | null | undefined): boolean {
    if (application == null) {
      this.invalidFileErrorMessage = 'No file received!';
      return false;
    }

    const validZipMimeTypes = [
      'application/x-zip-compressed',
      'application/zip',
      'application/octet-stream',
      'multipart/x-zip',
    ];

    if (!validZipMimeTypes.includes(application.type)) {
      this.invalidFileErrorMessage =
        'Wrong file type. Only ZIP files are allowed.';
      return false;
    }

    if (application.size > this.uploadFileSizeLimit) {
      this.invalidFileErrorMessage =
        'File too large. Only files under 8 Gigabytes are allowed.';
      return false;
    }

    return true;
  }

  addApplication() {
    if (!this.applicationInfo!.existingVersion) {
      this._addApplication();

      return;
    }

    const version = this.applicationInfo!.version;
    const existingVersion = this.applicationInfo!.existingVersion;

    if (compare(version, existingVersion, '>=')) {
      this._addApplication();
      return;
    }

    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 =
      'You are about to upload an older version of an application, this version will not be deployed to Microsoft Endpoint Manager.';
    modalRef.componentInstance.content2 = `If you want an older version deployed, you will have to delete this application and add it again with an older version.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this._addApplication();
    });
  }

  _addApplication() {
    this.applicationService.addApplication(this.applicationInfo!).subscribe();

    this.applicationAddStarted.emit();
    this.activeModal.close();
  }

  clear() {
    this.subscription?.unsubscribe();
    this.applicationInfo = null;
    this.subscription = null;

    this.currentUploadState = UploadState.NotStarted;
    this.step = 1;
    this.fileName = '';

    this.progress = 0;
    this.loaded = 0;
    this.total = 0;
  }
}
