import type { AggregatedMatch } from '../types/api';

interface Props {
  consensus: AggregatedMatch['consensus'];
}

const TYPE_LABELS: Record<string, string> = {
  over_under: 'Over/Under',
  btts: 'BTTS',
  '1x2': '1X2',
  handicap: 'Handicap',
  correct_score: 'Correct Score',
};

const VALUE_LABELS: Record<string, string> = {
  over_2_5: 'Over 2.5',
  under_2_5: 'Under 2.5',
  over_1_5: 'Over 1.5',
  under_1_5: 'Under 1.5',
  yes: 'Yes',
  no: 'No',
  home: 'Home',
  draw: 'Draw',
  away: 'Away',
};

function formatValue(value: string): string {
  return VALUE_LABELS[value] ?? value.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
}

export function ConsensusBars({ consensus }: Props) {
  const types = Object.entries(consensus);
  if (types.length === 0) return null;

  return (
    <div className="space-y-3">
      {types.map(([tipType, values]) => (
        <div key={tipType}>
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">
            {TYPE_LABELS[tipType] ?? tipType}
          </p>
          <div className="space-y-1">
            {Object.entries(values)
              .sort(([, a], [, b]) => b - a)
              .map(([value, pct]) => (
                <div key={value}>
                  <div className="flex justify-between text-xs text-gray-700 mb-0.5">
                    <span>{formatValue(value)}</span>
                    <span className="font-medium">{Math.round(pct * 100)}%</span>
                  </div>
                  <div className="w-full bg-gray-100 rounded-full h-2">
                    <div
                      className="h-2 rounded-full transition-all duration-500"
                      style={{
                        width: `${pct * 100}%`,
                        backgroundColor: pct >= 0.7 ? '#16a34a' : pct >= 0.5 ? '#2563eb' : '#6b7280',
                      }}
                    />
                  </div>
                </div>
              ))}
          </div>
        </div>
      ))}
    </div>
  );
}
