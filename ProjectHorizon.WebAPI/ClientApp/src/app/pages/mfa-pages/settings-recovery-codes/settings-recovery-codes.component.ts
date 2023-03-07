import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-settings-recovery-codes',
  templateUrl: './settings-recovery-codes.component.html',
})
export class SettingsRecoveryCodesComponent implements OnInit {
  constructor(private readonly activatedRoute: ActivatedRoute) {}

  showActivated: boolean;

  ngOnInit(): void {
    const showActivated = this.activatedRoute.snapshot.queryParamMap.get(
      'showActivated'
    );
    this.showActivated = showActivated === 'true';
  }
}
