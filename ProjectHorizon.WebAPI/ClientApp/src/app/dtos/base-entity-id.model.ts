import { BaseEntity } from './base-entity.model';

export class BaseEntityId<T> extends BaseEntity {
  id: T;
}
