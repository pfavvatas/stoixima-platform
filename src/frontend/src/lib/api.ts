import axios from 'axios';
import type { AggregatedMatch, AdminChannel, AdminStats, Channel, Match, ServiceStatus } from '../types/api';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
});

export const feedApi = {
  getFeed: (date?: string, league?: string, minTippers = 1) =>
    api.get<AggregatedMatch[]>('/api/feed', {
      params: { date, league, minTippers },
    }).then(r => r.data),

  getMatch: (matchId: number) =>
    api.get<AggregatedMatch>(`/api/feed/match/${matchId}`).then(r => r.data),

  getMatches: (date?: string) =>
    api.get<Match[]>('/api/matches', { params: { date } }).then(r => r.data),

  getChannels: () =>
    api.get<Channel[]>('/api/channels').then(r => r.data),
};

export const adminApi = {
  getServices: () =>
    api.get<ServiceStatus[]>('/admin/services').then(r => r.data),

  getStats: () =>
    api.get<AdminStats>('/admin/stats').then(r => r.data),

  getChannels: () =>
    api.get<AdminChannel[]>('/admin/channels').then(r => r.data),

  createChannel: (chatId: string, title: string, username?: string) =>
    api.post<AdminChannel>('/admin/channels', { chatId: Number(chatId), title, username }).then(r => r.data),

  toggleChannel: (id: number, active: boolean) =>
    api.patch(`/admin/channels/${id}/active`, { active }).then(r => r.data),

  deleteChannel: (id: number) =>
    api.delete(`/admin/channels/${id}`).then(r => r.data),

  fetchMatches: () =>
    api.post('/admin/actions/fetch-matches').then(r => r.data),

  reAggregate: () =>
    api.post('/admin/actions/re-aggregate').then(r => r.data),
};
