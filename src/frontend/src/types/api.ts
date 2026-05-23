export interface TipDetail {
  channelId: number;
  channelTitle: string;
  tipType: string;
  tipValue: string;
  odds: number | null;
}

export interface AggregatedMatch {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  league: string;
  kickOff: string;
  totalTippers: number;
  status: 'scheduled' | 'live' | 'finished' | 'postponed';
  consensus: Record<string, Record<string, number>>;
  tips: TipDetail[];
  updatedAt: string;
}

export interface Match {
  id: number;
  homeTeam: string;
  awayTeam: string;
  league: string;
  kickOff: string;
  status: string;
  homeScore: number | null;
  awayScore: number | null;
}

export interface Channel {
  id: number;
  title: string;
  source: string;
  active: boolean;
  tipCount: number;
}
