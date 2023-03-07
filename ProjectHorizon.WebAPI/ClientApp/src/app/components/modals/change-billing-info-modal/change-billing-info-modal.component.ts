import { Component, Input } from '@angular/core';
import { NgForm } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { CountryList } from 'src/app/constants/country-list';
import { Patterns } from 'src/app/constants/patterns';
import { BillingInfoDto } from 'src/app/dtos/billing-info-dto.model';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { isCompanyNameValid } from 'src/app/utility';

@Component({
  selector: 'app-change-billing-info-modal',
  templateUrl: './change-billing-info-modal.component.html',
})
export class ChangeBillingInfoModalComponent {
  @Input() billingInfoDto: BillingInfoDto;

  readonly patterns = Patterns;
  isCompanyNameValid = isCompanyNameValid;
  readonly countrySelectionTag = 'Select a country';
  countryList = CountryList;

  constructor(
    private readonly activeModal: NgbActiveModal,
    private readonly subscriptionService: SubscriptionService
  ) {}

  submit(form: NgForm): void {
    if (
      form.value.country === this.countrySelectionTag ||
      !isCompanyNameValid(this.billingInfoDto.companyName)
    )
      return;

    this.subscriptionService
      .updateBillingInfo(this.billingInfoDto)
      .subscribe(() => {
        this.close();
        window.location.reload();
      });
  }

  close() {
    this.activeModal.close();
  }
}
