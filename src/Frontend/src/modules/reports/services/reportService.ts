import { request } from '../../../shared/api/apiClient';

export interface ReportPayload {
  offerId: number;
  reason: string;
  comments?: string;
}

export interface ReportResponse {
  success: boolean;
  message: string;
  reportId?: number;
}

export const reportService = {
  async submitReport(payload: ReportPayload): Promise<ReportResponse> {
    return request<ReportResponse>('/reports', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  }
};
