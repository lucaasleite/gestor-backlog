export interface ConnectionSettings {
  organizationUrl: string;
  project: string;
  team: string;
  areaPath?: string;
  personalAccessToken?: string;
}

export interface ConnectionSettingsResponse {
  organizationUrl: string;
  project: string;
  team: string;
  areaPath?: string;
  hasToken: boolean;
}

export interface Iteration {
  id: string;
  name: string;
  path: string;
  isCurrent: boolean;
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

export interface ParentUserStory {
  id: number;
  title: string;
  assignedTo: string | null;
}

export interface GenerateTasksFromParentRequest {
  userStoryIds: number[];
  iterationPaths: string[];
}
