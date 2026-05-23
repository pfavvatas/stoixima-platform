import axios from 'axios';
import type { AggregatedMatch, Channel, Match } from '../types/api';

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
