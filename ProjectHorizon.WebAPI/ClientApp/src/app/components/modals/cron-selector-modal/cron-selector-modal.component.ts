import { WeekDay } from '@angular/common';
import { Component, Input, OnInit, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import {
  CronBuilder,
  DAY_OF_THE_MONTH,
  DAY_OF_THE_WEEK,
  HOUR,
  MINUTE,
} from 'cron-builder-ts';

import * as Cron from 'cron-converter';

export enum Mode {
  Update,
  Weekly,
  Monthly,
}

export enum MonthWeek {
  First,
  Second,
  Third,
  Fourth,
}

export class TriggerDto {
  nextStartTime: string | undefined;
  expression: string | undefined;
  mode: Mode = Mode.Update;
  weekDay?: WeekDay;
  monthWeek?: MonthWeek;

  static fromExpression(expression: string | undefined) {
    const dto = new TriggerDto();
    dto.mode = Mode.Update;

    if (!expression) {
      return dto;
    }

    dto.mode = Mode.Weekly;
    const builder = new CronBuilder(expression);

    dto.weekDay = builder.get(DAY_OF_THE_WEEK) as unknown as WeekDay;
    const dayOfTheMonth = builder.get(DAY_OF_THE_MONTH);

    if (dayOfTheMonth != '*') {
      dto.mode = Mode.Monthly;

      let startDay = dayOfTheMonth.split('-')[0] as unknown as number;
      startDay -= 1;
      startDay /= 7;

      dto.monthWeek = startDay as MonthWeek;
    }

    const cronInstance = new Cron();
    cronInstance.fromString(expression);
    dto.nextStartTime = cronInstance.schedule().next().format();
    dto.expression = expression;

    return dto;
  }
}

@Component({
  selector: 'app-cron-selector-modal',
  templateUrl: './cron-selector-modal.component.html',
  styleUrls: ['./cron-selector-modal.component.scss'],
})
export class CronSelectorModalComponent implements OnInit {
  Mode = Mode;
  WeekDay = WeekDay;
  MonthWeek = MonthWeek;

  public weekDays: WeekDay[] = [
    WeekDay.Monday,
    WeekDay.Tuesday,
    WeekDay.Wednesday,
    WeekDay.Thursday,
    WeekDay.Friday,
    WeekDay.Saturday,
    WeekDay.Sunday,
  ];

  public monthWeeks: MonthWeek[] = [
    MonthWeek.First,
    MonthWeek.Second,
    MonthWeek.Third,
    MonthWeek.Fourth,
  ];

  @Input()
  expression: string | undefined;

  dto: TriggerDto = new TriggerDto();

  constructor(private readonly activeModal: NgbActiveModal) {}

  ngOnInit(): void {
    if (this.expression) {
      this.dto = TriggerDto.fromExpression(this.expression);
      console.log(this.dto);
    }
  }
  
  update() {
    const builder = new CronBuilder();
    builder.set(HOUR, ['0']);
    builder.set(MINUTE, ['0']);

    this.dto.expression = undefined;

    switch (+this.dto.mode) {
      case Mode.Weekly:
        builder.set(DAY_OF_THE_WEEK, [(this.dto.weekDay as number).toString()]);

        this.dto.expression = builder.build();
        break;

      case Mode.Monthly:
        const startDay = this.dto.monthWeek! * 7 + 1;
        const endDay = startDay + 7;

        builder.set(DAY_OF_THE_WEEK, [(this.dto.weekDay as number).toString()]);
        builder.set(DAY_OF_THE_MONTH, [`${startDay}-${endDay}`]);

        this.dto.expression = builder.build();
        break;
    }

    if (!this.dto.expression) {
      return;
    }

    var cronInstance = new Cron();
    cronInstance.fromString(this.dto.expression);
    this.dto.nextStartTime = cronInstance.schedule().next().format();
  }

  onModeChange(event: Event) {
    const value = (event.target as HTMLInputElement).value as unknown as Mode;

    this.dto.mode = value;

    // the + casts the value into a number, see https://stackoverflow.com/questions/27747437/typescript-enum-switch-not-working
    switch (+value) {
      case Mode.Update: {
        this.dto.weekDay = undefined;
        this.dto.monthWeek = undefined;
        break;
      }

      case Mode.Weekly: {
        this.dto.weekDay = WeekDay.Monday;
        this.dto.monthWeek = undefined;

        break;
      }

      case Mode.Monthly: {
        this.dto.weekDay = WeekDay.Monday;
        this.dto.monthWeek = MonthWeek.First;
        break;
      }
    }

    this.update();
  }

  passBack() {
    this.update();

    this.activeModal.close(this.dto);
  }

  close() {
    this.activeModal.close();
  }
}
