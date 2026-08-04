import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ConnectionSettings,
  ConnectionSettingsResponse,
  GenerateTasksFromParentRequest,
  GenerateTasksRequest,
  GenerateTasksResult,
  Iteration,
  ParentUserStory,
  WorkItemPreview,
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

  getSprints(): Observable<Iteration[]> {
    return this.http.get<Iteration[]>(`${this.baseUrl}/sprints`);
  }

  getWorkItems(iterationPath: string): Observable<WorkItemPreview[]> {
    return this.http.get<WorkItemPreview[]>(`${this.baseUrl}/workitems`, {
      params: { iterationPath },
    });
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
}
