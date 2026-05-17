# Frontend Specification

## Stack

- **React 18 + TypeScript**
- **Vite** (build tool)
- **TailwindCSS** (styling)
- **@microsoft/signalr** (WebSocket)
- **React Query** (data fetching + caching)

### Γιατί React (και όχι Angular)

- Μικρότερο bundle size για αυτό το use case
- Πιο γρήγορο για να φτιάξεις ένα feed UI
- Μεγαλύτερη κοινότητα για real-time UI components
- Εύκολη ενσωμάτωση SignalR

---

## Σελίδες

### / (Feed Page — κύρια σελίδα)

Real-time feed με match cards.

**Layout:**
```
┌─────────────────────────────────────────┐
│  [Logo] Stoixima  [Date Picker] [Filter]│
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  🏟 Arsenal vs Chelsea  15:00   │   │
│  │  Premier League                 │   │
│  │  ─────────────────────────────  │   │
│  │  6 tipsters                     │   │
│  │                                 │   │
│  │  Over 2.5:  ████████░░  80%    │   │
│  │  BTTS Yes:  ██████████  100%   │   │
│  │  Home Win:  █████░░░░░  50%    │   │
│  │                                 │   │
│  │  [Δες αναλυτικά ▼]              │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  🏟 Real Madrid vs Barcelona    │   │
│  │  ...                            │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

**Live badge**: αν ο αγώνας είναι live, εμφάνιση κόκκινου "LIVE" indicator.

---

### Match Detail Drawer/Modal

Ανοίγει όταν πατάς "Δες αναλυτικά" σε match card.

```
Arsenal vs Chelsea
─────────────────────────────────────────
Tipsters (6):

  📢 Football Tips VIP
     BTTS Yes @ 1.85

  📢 Soccer Predictions
     Over 2.5 @ 1.70
     Home Win @ 2.10

  📢 Sure Bets Channel
     BTTS Yes @ 1.85
     Over 2.5 @ 1.72
  ...
```

---

## Components

### `<MatchCard />`
Props:
- `match: AggregatedMatch`
- `onExpand: () => void`

Εμφανίζει: teams, kick-off time, consensus bars, tipper count.

### `<ConsensusBars />`
Props:
- `consensus: ConsensusData`

Progress bars για κάθε tip type με percentages.

### `<TipperList />`
Props:
- `tips: TipDetail[]`

Λίστα ανά channel με tip details.

### `<FeedPage />`
- Κάνει fetch από `GET /api/feed?date=today`
- Συνδέεται στο SignalR hub
- On `MatchUpdated` event: ανανεώνει το αντίστοιχο match card

---

## Real-Time Connection

```typescript
// hooks/useFeedWebSocket.ts
export function useFeedWebSocket(onMatchUpdate: (match: AggregatedMatch) => void) {
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/feed")
      .withAutomaticReconnect()
      .build();

    connection.on("MatchUpdated", onMatchUpdate);
    connection.start();

    return () => { connection.stop(); };
  }, []);
}
```

---

## API Types (TypeScript)

```typescript
interface AggregatedMatch {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  league: string;
  kickOff: string;
  totalTippers: number;
  consensus: {
    over_under?: Record<string, number>;
    btts?: Record<string, number>;
    "1x2"?: Record<string, number>;
  };
  tips: TipDetail[];
  updatedAt: string;
}

interface TipDetail {
  channelId: number;
  channelTitle: string;
  tipType: string;
  tipValue: string;
  odds: number;
}
```

---

## Deployment

- Build: `npm run build` → static files
- Serve: Nginx container ή Vercel
- API proxy: Nginx reverse proxies `/api` και `/hubs` στο API Gateway
- Environment: `VITE_API_URL` = API Gateway URL
