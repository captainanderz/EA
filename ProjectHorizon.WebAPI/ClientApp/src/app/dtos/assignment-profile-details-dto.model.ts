import { AssignmentProfileGroupDetailsDto } from './assignment-profile-group-dto';

export class AssignmentProfileDetailsDto {
  id: number;
  name: string;
  groups: AssignmentProfileGroupDetailsDto[];
}
