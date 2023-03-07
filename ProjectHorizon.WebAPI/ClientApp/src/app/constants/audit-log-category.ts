export const AuditLogCategories = [
  'All Categories',
  'Public Repository',
  'Private Repository',
  'Approvals',
  'User Management',
  'Notification Settings',
  'Integrations',
  'Single Sign-On',
  'Subscription',
  'Assignment Profiles',
  'Shop Requests',
  'Deployment Schedules',
] as const;
export type AuditLogCategory = typeof AuditLogCategories[number];
