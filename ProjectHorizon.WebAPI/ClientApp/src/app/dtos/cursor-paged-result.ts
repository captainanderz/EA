import { PagedResult } from './paged-result.model';

export class CursorPagedResult<T> extends PagedResult<T> {
  nextPageLink: string;
}
