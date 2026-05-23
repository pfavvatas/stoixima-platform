import { useState } from 'react';
import type { AggregatedMatch } from '../types/api';
import { ConsensusBars } from './ConsensusBars';
import { TipperList } from './TipperList';

interface Props {
  match: AggregatedMatch;
  highlight?: boolean;
}

function formatTime(isoDate: string): string {
  return new Date(isoDate).toLocaleTimeString('el-GR', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Europe/Athens',
  });
}

const LEAGUE_LABELS: Record<string, string> = {
  PL: 'Premier League',
  PD: 'La Liga',
  SA: 'Serie A',
  BL1: 'Bundesliga',
  FL1: 'Ligue 1',
};

export function MatchCard({ match, highlight }: Props) {
  const [open, setOpen] = useState(false);
  const isLive = match.status === 'live';

  return (
    <div
      className={`bg-white rounded-xl border shadow-sm transition-all duration-300 ${
        highlight ? 'ring-2 ring-blue-400 ring-offset-1' : ''
      }`}
    >
      <div className="p-4">
        {/* Header */}
        <div className="flex items-start justify-between mb-3">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              {isLive && (
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-700 animate-pulse">
                  LIVE
                </span>
              )}
              <span className="text-xs text-gray-500">
                {LEAGUE_LABELS[match.league] ?? match.league}
              </span>
            </div>
            <h3 className="text-base font-semibold text-gray-900">
              {match.homeTeam} vs {match.awayTeam}
            </h3>
          </div>
          <div className="text-right ml-4">
            <p className="text-lg font-bold text-gray-800">{formatTime(match.kickOff)}</p>
            <p className="text-xs text-gray-500">{match.totalTippers} tipsters</p>
          </div>
        </div>

        {/* Consensus bars */}
        <ConsensusBars consensus={match.consensus} />

        {/* Expand button */}
        <button
          onClick={() => setOpen(o => !o)}
          className="mt-3 w-full text-sm text-blue-600 hover:text-blue-800 flex items-center justify-center gap-1 transition-colors"
        >
          {open ? '▲ Απόκρυψη' : '▼ Δες αναλυτικά'}
        </button>
      </div>

      {/* Tipper detail drawer */}
      {open && (
        <div className="border-t border-gray-100 px-4 pb-4 pt-3 bg-gray-50 rounded-b-xl">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
            Αναλυτικά tips ({match.tips.length})
          </p>
          <TipperList tips={match.tips} />
        </div>
      )}
    </div>
  );
}
