import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { useEffect, useRef } from 'react';
import type { AggregatedMatch } from '../types/api';

export function useFeedWebSocket(
  onMatchUpdate: (match: AggregatedMatch) => void
) {
  const callbackRef = useRef(onMatchUpdate);
  callbackRef.current = onMatchUpdate;

  useEffect(() => {
    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
    const conn = new HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/feed`)
      .withAutomaticReconnect()
      .build();

    conn.on('MatchUpdated', (match: AggregatedMatch) => {
      callbackRef.current(match);
    });

    conn.start().catch(err => console.error('SignalR connection error:', err));

    return () => {
      if (conn.state !== HubConnectionState.Disconnected) {
        conn.stop();
      }
    };
  }, []);
}
