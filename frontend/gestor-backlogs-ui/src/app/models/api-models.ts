export interface TeamConfig {
  name: string;
  areaPath: string;
}

export type AuthMode = 'Pat' | 'Sso';

export interface ConnectionSettings {
  organizationUrl: string;
  project: string;
  teams: TeamConfig[];
  authMode: AuthMode;
  personalAccessToken?: string;
}

export interface ConnectionSettingsResponse {
  organizationUrl: string;
  project: string;
  teams: TeamConfig[];
  authMode: AuthMode;
  hasToken: boolean;
}

export interface EntraDeviceCodeInfo {
  verificationUri: string;
  userCode: string;
  expiresInSeconds: number;
}

export interface EntraLoginStatus {
  status: 'pending' | 'success' | 'error';
  message?: string;
}

export interface Iteration {
  id: string;
  name: string;
  path: string;
  isCurrent: boolean;
  finishDate: string | null;
}

export interface PlannedTask {
  title: string;
  hours: number;
}

export interface WorkItemPreview {
  id: number;
  title: string;
  workItemType: string;
  sizeLabel: string | null;
  effortHours: number | null;
  assignedTo: string | null;
  alreadyHasTasks: boolean;
  sizeRecognized: boolean;
  plannedTasks: PlannedTask[];
  state: string | null;
  hasOpenTaskInDifferentSprint: boolean;
}

export interface WorkItemTask {
  id: number;
  title: string;
  assignedTo: string | null;
  originalEstimate: number | null;
  remainingWork: number | null;
  completedWork: number | null;
  state: string | null;
  iterationPath: string;
}

export interface TaskOverride {
  title: string;
  hours: number;
}

export interface GenerateTasksRequest {
  iterationPath: string;
  workItemIds: number[];
  overrides?: Record<number, TaskOverride[]>;
}

export interface CreatedTaskInfo {
  id: number;
  title: string;
  hoursEstimate: number;
}

export interface GenerateTasksItemResult {
  parentId: number;
  parentTitle: string;
  createdTasks: CreatedTaskInfo[];
}

export interface SkippedItemResult {
  parentId: number;
  parentTitle: string;
  reason: string;
}

export interface GenerateTasksResult {
  created: GenerateTasksItemResult[];
  skipped: SkippedItemResult[];
}

export interface RegenerateTasksResult {
  parentId: number;
  parentTitle: string;
  closedTasks: CreatedTaskInfo[];
  createdTasks: CreatedTaskInfo[];
}

export interface ParentUserStory {
  id: number;
  title: string;
  assignedTo: string | null;
  sizeLabel: string | null;
  effortHours: number | null;
  alreadyHasTasks: boolean;
}

export interface GenerateTasksFromParentRequest {
  userStoryIds: number[];
  iterationPaths: string[];
}

export interface SprintTrendPoint {
  label: string;
  isCurrent: boolean;
  plannedItems: number;
  outOfSprintItems: number;
  plannedHours: number;
  outOfSprintHours: number;
}

export interface AnalystBreakdown {
  name: string;
  plannedItems: number;
  outOfSprintItems: number;
  plannedHours: number;
  outOfSprintHours: number;
  doneItems: number;
  totalItems: number;
}

export interface OutOfSprintItem {
  id: number;
  title: string;
  workItemType: string;
  assignedTo: string | null;
  effortHours: number | null;
  state: string | null;
}

export interface SprintDashboard {
  plannedItems: number;
  outOfSprintItems: number;
  plannedHours: number;
  outOfSprintHours: number;
  plannedDoneItems: number;
  outOfSprintDoneItems: number;
  plannedDoneHours: number;
  outOfSprintDoneHours: number;
  trend: SprintTrendPoint[];
  analysts: AnalystBreakdown[];
  outOfSprintDetail: OutOfSprintItem[];
}
