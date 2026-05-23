import type { TipDetail } from '../types/api';

interface Props {
  tips: TipDetail[];
}

const TYPE_LABELS: Record<string, string> = {
  over_under: 'O/U',
  btts: 'BTTS',
  '1x2': '1X2',
  handicap: 'HC',
  correct_score: 'CS',
};

export function TipperList({ tips }: Props) {
  if (tips.length === 0) {
    return <p className="text-sm text-gray-400 italic">No tips yet.</p>;
  }

  return (
    <div className="divide-y divide-gray-100">
      {tips.map((tip, i) => (
        <div key={i} className="flex items-center justify-between py-2">
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-50 text-blue-700">
              {TYPE_LABELS[tip.tipType] ?? tip.tipType}
            </span>
            <span className="text-sm font-medium text-gray-800">{tip.channelTitle}</span>
          </div>
          <div className="flex items-center gap-2 text-right">
            <span className="text-sm text-gray-600">
              {tip.tipValue.replace(/_/g, ' ')}
            </span>
            {tip.odds && (
              <span className="text-sm font-semibold text-green-600">@{tip.odds.toFixed(2)}</span>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
