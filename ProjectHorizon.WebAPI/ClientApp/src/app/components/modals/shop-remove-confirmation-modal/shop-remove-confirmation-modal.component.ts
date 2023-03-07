import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ShopAddDto } from 'src/app/dtos/shop-add-dto.model';
import { ShopRemoveDto } from 'src/app/dtos/shop-remove-dto';

@Component({
  selector: 'app-shop-remove-confirmation-modal',
  templateUrl: './shop-remove-confirmation-modal.component.html',
  styleUrls: ['./shop-remove-confirmation-modal.component.scss'],
})
export class ShopRemoveConfirmationModalComponent implements OnInit {
  public dto: ShopRemoveDto = new ShopRemoveDto();

  @Output() continue: EventEmitter<ShopRemoveDto> =
    new EventEmitter<ShopRemoveDto>();

  constructor(public activeModal: NgbActiveModal) {}

  ngOnInit(): void {}

  doContinue() {
    this.continue.emit(this.dto);
    this.activeModal.close();
  }
}
