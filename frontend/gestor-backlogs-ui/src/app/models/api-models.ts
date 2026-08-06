export interface TeamConfig {
  name: string;
  areaPath: string;
}

export interface ConnectionSettings {
  organizationUrl: string;
  project: string;
  teams: TeamConfig[];
  personalAccessToken?: string;
}

export interface ConnectionSettingsResponse {
  organizationUrl: string;
  project: string;
  teams: TeamConfig[];
  hasToken: boolean;
}

export interface Iteration {
  id: string;
  name: string;
  path: string;
  isCurrent: boolean;
  finishDate: string | null;
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
  plannedTaskTitles: string[];
  state: string | null;
}

export interface WorkItemTask {
  id: number;
  title: string;
  assignedTo: string | null;
  originalEstimate: number | null;
  remainingWork: number | null;
  completedWork: number | null;
  state: string | null;
}

export interface GenerateTasksRequest {
  iterationPath: string;
  workItemIds: number[];
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
