export function MatchCardSkeleton() {
  return (
    <div className="bg-white rounded-xl border shadow-sm p-4 animate-pulse">
      <div className="flex items-start justify-between mb-3">
        <div className="flex-1">
          <div className="h-3 bg-gray-200 rounded w-24 mb-2" />
          <div className="h-5 bg-gray-200 rounded w-48" />
        </div>
        <div className="ml-4 text-right">
          <div className="h-6 bg-gray-200 rounded w-12 mb-1" />
          <div className="h-3 bg-gray-200 rounded w-16" />
        </div>
      </div>
      <div className="space-y-3">
        {[1, 2].map(i => (
          <div key={i}>
            <div className="h-3 bg-gray-200 rounded w-20 mb-1" />
            <div className="h-2 bg-gray-200 rounded-full w-full" />
          </div>
        ))}
      </div>
    </div>
  );
}
