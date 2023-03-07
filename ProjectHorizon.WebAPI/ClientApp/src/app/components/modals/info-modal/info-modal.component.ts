import { Component, Input, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-info-modal',
  templateUrl: './info-modal.component.html',
})
export class InfoModalComponent implements OnInit {
  @Input() title: string;
  @Input() content1: string;
  @Input() content2: string;

  constructor(public activeModal: NgbActiveModal) {}

  ngOnInit(): void {}
}
