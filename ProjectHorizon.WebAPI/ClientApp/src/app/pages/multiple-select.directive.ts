import { Directive, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseEntityId } from '../dtos/base-entity-id.model';
import { UserStore } from '../services/user.store';

export enum SelectionState {
  None,
  Partial,
  All,
}

@Directive({
  selector: '[appMultipleSelect]',
})
export class MultipleSelectDirective<TId, T extends BaseEntityId<TId>, TOption>
  implements OnInit
{
  constructor(protected userStore: UserStore) {}

  ngOnInit(): void {
    this.userStore
      .getLoggedInUser()
      .subscribe(() => this.selectedItemIds.clear());
  }

  pgNr = 1;
  pageSize = 20;
  allItemsCount = 0;

  pagedItems: ReadonlyArray<T> = [];
  selectedOption: TOption | undefined = undefined;

  SelectionState = SelectionState;

  protected selectedItemIds = new Set<TId>();

  toggleSelectItem(itemId: TId) {
    if (this.isItemSelected(itemId)) {
      this.selectedItemIds.delete(itemId);
    } else {
      this.selectedItemIds.add(itemId);
    }
  }

  isItemSelected(itemId: TId): boolean {
    return this.selectedItemIds.has(itemId);
  }

  getSelectionState(): SelectionState {
    const selectedItemsCount = this.selectedItemIds.size;

    if (selectedItemsCount == 0) {
      return SelectionState.None;
    } else if (selectedItemsCount == this.allItemsCount) {
      return SelectionState.All;
    } else {
      return SelectionState.Partial;
    }
  }

  getAllItemIds?(): Observable<TId[]>;

  selectAllItems() {
    if (this.isAnyItemSelected()) {
      this.selectedItemIds.clear();
    } else {
      if (this.getAllItemIds) {
        this.getAllItemIds().subscribe((itemIds) => {
          this.selectedItemIds = new Set(itemIds);
        });
      }
    }
  }

  deselectItems(itemIds: Set<TId>) {
    itemIds.forEach((itemId) => {
      this.selectedItemIds.delete(itemId);
    });
  }

  canApplyBulkAction(): boolean {
    return this.selectedOption != undefined && this.isAnyItemSelected();
  }

  isAnyItemSelected(): boolean {
    return this.selectedItemIds.size > 0;
  }
}
