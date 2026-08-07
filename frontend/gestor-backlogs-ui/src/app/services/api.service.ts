import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ConnectionSettings,
  ConnectionSettingsResponse,
  GenerateTasksFromParentRequest,
  GenerateTasksItemResult,
  GenerateTasksRequest,
  GenerateTasksResult,
  Iteration,
  ParentUserStory,
  RegenerateTasksResult,
  SprintDashboard,
  TaskOverride,
  WorkItemPreview,
  WorkItemTask,
} from '../models/api-models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  getConnectionSettings(): Observable<ConnectionSettingsResponse> {
    return this.http.get<ConnectionSettingsResponse>(`${this.baseUrl}/config`);
  }

  saveConnectionSettings(settings: ConnectionSettings): Observable<ConnectionSettingsResponse> {
    return this.http.post<ConnectionSettingsResponse>(`${this.baseUrl}/config`, settings);
  }

  testConnection(): Observable<{ success: boolean; message?: string }> {
    return this.http.post<{ success: boolean; message?: string }>(`${this.baseUrl}/config/test-connection`, {});
  }

  getSprints(team: string): Observable<Iteration[]> {
    return this.http.get<Iteration[]>(`${this.baseUrl}/sprints`, { params: { team } });
  }

  getWorkItems(iterationPath: string, areaPath: string | null): Observable<WorkItemPreview[]> {
    const params: Record<string, string> = { iterationPath };
    if (areaPath) {
      params['areaPath'] = areaPath;
    }
    return this.http.get<WorkItemPreview[]>(`${this.baseUrl}/workitems`, { params });
  }

  generateTasks(request: GenerateTasksRequest): Observable<GenerateTasksResult> {
    return this.http.post<GenerateTasksResult>(`${this.baseUrl}/workitems/generate-tasks`, request);
  }

  getParentUserStories(parentId: number): Observable<ParentUserStory[]> {
    return this.http.get<ParentUserStory[]>(`${this.baseUrl}/parent/${parentId}/user-stories`);
  }

  generateTasksFromParent(request: GenerateTasksFromParentRequest): Observable<GenerateTasksResult> {
    return this.http.post<GenerateTasksResult>(`${this.baseUrl}/parent/generate-tasks`, request);
  }

  getChildTasks(parentId: number): Observable<WorkItemTask[]> {
    return this.http.get<WorkItemTask[]>(`${this.baseUrl}/workitems/${parentId}/tasks`);
  }

  regenerateTasks(workItemId: number): Observable<RegenerateTasksResult> {
    return this.http.post<RegenerateTasksResult>(`${this.baseUrl}/workitems/${workItemId}/regenerate-tasks`, {});
  }

  getWorkItemPreview(id: number): Observable<WorkItemPreview> {
    return this.http.get<WorkItemPreview>(`${this.baseUrl}/workitems/${id}/preview`);
  }

  generateTaskForItem(id: number, tasks?: TaskOverride[]): Observable<GenerateTasksItemResult> {
    return this.http.post<GenerateTasksItemResult>(`${this.baseUrl}/workitems/${id}/generate-tasks`, { tasks });
  }

  closeWorkItem(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/workitems/${id}/close`, {});
  }

  getSprintDashboard(team: string, areaPath: string | null, iterationPath: string): Observable<SprintDashboard> {
    const params: Record<string, string> = { team, iterationPath };
    if (areaPath) {
      params['areaPath'] = areaPath;
    }
    return this.http.get<SprintDashboard>(`${this.baseUrl}/dashboard`, { params });
  }
}
