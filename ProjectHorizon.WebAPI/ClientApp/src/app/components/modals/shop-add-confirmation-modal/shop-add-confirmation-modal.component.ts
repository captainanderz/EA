import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { ShopAddDto } from 'src/app/dtos/shop-add-dto.model';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { SubscriptionService } from 'src/app/services/subscription.service';

@Component({
  selector: 'app-shop-add-confirmation-modal',
  templateUrl: './shop-add-confirmation-modal.component.html',
  styleUrls: ['./shop-add-confirmation-modal.component.scss'],
})
export class ShopAddConfirmationModalComponent implements OnInit {
  public dto: ShopAddDto = new ShopAddDto();
  public appDto: ApplicationDto = new ApplicationDto();
  public subDto: SubscriptionDto = new SubscriptionDto();

  @Output() continue: EventEmitter<ShopAddDto> = new EventEmitter<ShopAddDto>();

  constructor(
    public activeModal: NgbActiveModal,
    public subscriptionService: SubscriptionService
  ) {}

  ngOnInit(): void {
    this.subscriptionService.getShopGroupPrefix().subscribe((dto) => {
      this.subDto.shopGroupPrefix = dto.prefix;
      debugger;
      this.dto.groupName = `${this.subDto.shopGroupPrefix}_${this.appDto.publisher}_${this.appDto.name}_${this.appDto.architecture}`;
    });
  }

  doContinue() {
    this.continue.emit(this.dto);
    this.activeModal.close();
  }
}
