import { useCallback, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { feedApi } from '../lib/api';
import { useFeedWebSocket } from '../hooks/useFeedWebSocket';
import { MatchCard } from '../components/MatchCard';
import { MatchCardSkeleton } from '../components/MatchCardSkeleton';
import type { AggregatedMatch } from '../types/api';

const LEAGUES = ['', 'PL', 'PD', 'SA', 'BL1', 'FL1'];
const LEAGUE_NAMES: Record<string, string> = {
  '': 'All Leagues',
  PL: 'Premier League',
  PD: 'La Liga',
  SA: 'Serie A',
  BL1: 'Bundesliga',
  FL1: 'Ligue 1',
};

function todayIso(): string {
  return new Date().toISOString().split('T')[0];
}

export function FeedPage() {
  const [date, setDate]     = useState(todayIso());
  const [league, setLeague] = useState('');
  const [minTippers, setMinTippers] = useState(1);
  const [highlighted, setHighlighted] = useState<number | null>(null);

  const queryClient = useQueryClient();

  const { data: feed = [], isLoading, isError } = useQuery({
    queryKey: ['feed', date, league, minTippers],
    queryFn: () => feedApi.getFeed(date, league || undefined, minTippers),
    refetchInterval: 60_000,
  });

  const handleMatchUpdate = useCallback((updated: AggregatedMatch) => {
    queryClient.setQueryData<AggregatedMatch[]>(
      ['feed', date, league, minTippers],
      old => old?.map(m => m.matchId === updated.matchId ? updated : m) ?? [updated]
    );
    setHighlighted(updated.matchId);
    setTimeout(() => setHighlighted(null), 3000);
  }, [date, league, minTippers, queryClient]);

  useFeedWebSocket(handleMatchUpdate);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 sticky top-0 z-10">
        <div className="max-w-3xl mx-auto px-4 py-3 flex flex-wrap items-center gap-3">
          <h1 className="text-xl font-bold text-blue-700 mr-auto">⚽ Stoixima</h1>

          <input
            type="date"
            value={date}
            onChange={e => setDate(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          />

          <select
            value={league}
            onChange={e => setLeague(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            {LEAGUES.map(l => (
              <option key={l} value={l}>{LEAGUE_NAMES[l]}</option>
            ))}
          </select>

          <select
            value={minTippers}
            onChange={e => setMinTippers(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            {[1, 2, 3, 5].map(n => (
              <option key={n} value={n}>Min {n} tipper{n > 1 ? 's' : ''}</option>
            ))}
          </select>
        </div>
      </header>

      {/* Content */}
      <main className="max-w-3xl mx-auto px-4 py-6">
        {isLoading && (
          <div className="space-y-4">
            {[1, 2, 3].map(i => <MatchCardSkeleton key={i} />)}
          </div>
        )}

        {isError && (
          <div className="text-center py-16">
            <p className="text-red-500 text-lg">Αποτυχία φόρτωσης δεδομένων.</p>
            <p className="text-gray-400 text-sm mt-1">Έλεγξε ότι το API Gateway τρέχει.</p>
          </div>
        )}

        {!isLoading && !isError && feed.length === 0 && (
          <div className="text-center py-16">
            <p className="text-4xl mb-4">📭</p>
            <p className="text-gray-500 text-lg">Δεν υπάρχουν προβλέψεις για σήμερα.</p>
            <p className="text-gray-400 text-sm mt-1">
              Ελέγξε αν τα Telegram κανάλια είναι ενεργά.
            </p>
          </div>
        )}

        {!isLoading && !isError && feed.length > 0 && (
          <div className="space-y-4">
            <p className="text-sm text-gray-500">{feed.length} αγώνας/ες με tips</p>
            {feed.map(match => (
              <MatchCard
                key={match.matchId}
                match={match}
                highlight={highlighted === match.matchId}
              />
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
