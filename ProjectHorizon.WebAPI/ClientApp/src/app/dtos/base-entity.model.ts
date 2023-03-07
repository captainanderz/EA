export class BaseEntity {
  static default: BaseEntity = {
    createdOn: new Date(),
    modifiedOn: new Date(),
  };

  createdOn?: Date;
  modifiedOn?: Date;
}
