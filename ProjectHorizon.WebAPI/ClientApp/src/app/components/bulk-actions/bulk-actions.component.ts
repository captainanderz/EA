import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-bulk-actions',
  templateUrl: './bulk-actions.component.html',
  styleUrls: ['./bulk-actions.component.scss'],
})
export class BulkActionsComponent implements OnInit {
  @Input()
  enabled = true;
  @Output()
  apply = new EventEmitter();

  @Input()
  selectedOption!: any | undefined;
  @Output()
  selectedOptionChange = new EventEmitter<any>();

  constructor() {}

  ngOnInit(): void {}

  onChange(option: any) {
    this.selectedOption = option;
    this.selectedOptionChange.emit(this.selectedOption);
  }

  applyBulkActions() {
    this.apply.emit();
  }
}
