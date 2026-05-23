import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '../lib/api';
import type { AdminChannel, ServiceStatus } from '../types/api';

// ─── Service health grid ──────────────────────────────────────────────────────

function statusColor(status: ServiceStatus['status']) {
  if (status === 'healthy') return 'bg-green-500';
  if (status === 'unhealthy') return 'bg-yellow-500';
  return 'bg-red-500';
}

function ServicesPanel() {
  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['admin-services'],
    queryFn: adminApi.getServices,
    refetchInterval: 30_000,
  });

  return (
    <section>
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-lg font-semibold text-white">Service Health</h2>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="text-xs px-3 py-1 rounded bg-slate-700 text-slate-300 hover:bg-slate-600 disabled:opacity-50"
        >
          {isFetching ? 'Checking…' : 'Refresh'}
        </button>
      </div>
      {isLoading ? (
        <p className="text-slate-400 text-sm">Checking services…</p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {(data ?? []).map(s => (
            <div key={s.name} className="bg-slate-800 rounded-lg p-4 flex items-center gap-3">
              <span className={`w-3 h-3 rounded-full flex-shrink-0 ${statusColor(s.status)}`} />
              <div className="min-w-0">
                <p className="text-white text-sm font-medium truncate">{s.name}</p>
                <p className="text-slate-400 text-xs capitalize">{s.status} · {s.latencyMs} ms</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

// ─── Actions panel ────────────────────────────────────────────────────────────

function ActionsPanel() {
  const { data: stats } = useQuery({
    queryKey: ['admin-stats'],
    queryFn: adminApi.getStats,
    refetchInterval: 60_000,
  });

  const [actionLog, setActionLog] = useState<string[]>([]);

  function log(msg: string) {
    setActionLog(prev => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev].slice(0, 20));
  }

  const fetchMutation = useMutation({
    mutationFn: adminApi.fetchMatches,
    onSuccess: (data: { fetched?: { league: string; count: number; status: string }[] }) => {
      const total = (data?.fetched ?? []).reduce((s: number, r: { count: number }) => s + r.count, 0);
      log(`Fetch Matches: ${total} matches fetched across ${data?.fetched?.length ?? 0} leagues`);
    },
    onError: (e: Error) => log(`Fetch Matches failed: ${e.message}`),
  });

  const reAggMutation = useMutation({
    mutationFn: adminApi.reAggregate,
    onSuccess: (data: { total?: number }) => log(`Re-aggregate: ${data?.total ?? 0} matches re-aggregated`),
    onError: (e: Error) => log(`Re-aggregate failed: ${e.message}`),
  });

  const actions = [
    {
      label: 'Fetch Matches',
      description: 'Pull today\'s fixtures from football-data.org',
      fn: () => fetchMutation.mutate(),
      loading: fetchMutation.isPending,
    },
    {
      label: 'Re-aggregate',
      description: 'Recalculate consensus for all today\'s matches',
      fn: () => reAggMutation.mutate(),
      loading: reAggMutation.isPending,
    },
  ];

  return (
    <section>
      <h2 className="text-lg font-semibold text-white mb-3">Developer Actions</h2>

      {stats && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
          {[
            { label: 'Active channels', value: stats.activeChannels },
            { label: 'Matches today', value: stats.matchesToday },
            { label: 'Tips today', value: stats.tipsToday },
            { label: 'Active tippers', value: stats.activesTippersToday },
          ].map(s => (
            <div key={s.label} className="bg-slate-800 rounded-lg p-4">
              <p className="text-2xl font-bold text-white">{s.value}</p>
              <p className="text-slate-400 text-xs mt-1">{s.label}</p>
            </div>
          ))}
        </div>
      )}

      <div className="flex flex-wrap gap-3 mb-4">
        {actions.map(a => (
          <button
            key={a.label}
            onClick={a.fn}
            disabled={a.loading}
            title={a.description}
            className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-sm font-medium transition-colors"
          >
            {a.loading ? `${a.label}…` : a.label}
          </button>
        ))}
      </div>

      {actionLog.length > 0 && (
        <div className="bg-slate-900 rounded-lg p-3 font-mono text-xs text-slate-300 space-y-1 max-h-48 overflow-y-auto">
          {actionLog.map((line, i) => (
            <p key={i}>{line}</p>
          ))}
        </div>
      )}
    </section>
  );
}

// ─── Channels panel ───────────────────────────────────────────────────────────

function ChannelRow({ channel, onToggle, onDelete }: {
  channel: AdminChannel;
  onToggle: (id: number, active: boolean) => void;
  onDelete: (id: number) => void;
}) {
  return (
    <tr className="border-b border-slate-700">
      <td className="py-2 px-3 text-slate-200 text-sm">{channel.title}</td>
      <td className="py-2 px-3 text-slate-400 text-sm font-mono">{channel.id}</td>
      <td className="py-2 px-3 text-slate-400 text-sm">{channel.username ?? '—'}</td>
      <td className="py-2 px-3 text-slate-400 text-sm text-right">{channel.tipCount}</td>
      <td className="py-2 px-3 text-center">
        <button
          onClick={() => onToggle(channel.id, !channel.active)}
          className={`px-2 py-0.5 rounded text-xs font-medium transition-colors ${
            channel.active
              ? 'bg-green-600 hover:bg-green-500 text-white'
              : 'bg-slate-600 hover:bg-slate-500 text-slate-300'
          }`}
        >
          {channel.active ? 'Active' : 'Paused'}
        </button>
      </td>
      <td className="py-2 px-3 text-center">
        <button
          onClick={() => onDelete(channel.id)}
          className="text-red-500 hover:text-red-400 text-xs"
        >
          Delete
        </button>
      </td>
    </tr>
  );
}

function ChannelsPanel() {
  const qc = useQueryClient();

  const { data: channels, isLoading } = useQuery({
    queryKey: ['admin-channels'],
    queryFn: adminApi.getChannels,
  });

  const toggleMut = useMutation({
    mutationFn: ({ id, active }: { id: number; active: boolean }) =>
      adminApi.toggleChannel(id, active),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-channels'] }),
  });

  const deleteMut = useMutation({
    mutationFn: (id: number) => adminApi.deleteChannel(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-channels'] }),
  });

  const createMut = useMutation({
    mutationFn: ({ chatId, title, username }: { chatId: string; title: string; username: string }) =>
      adminApi.createChannel(chatId, title, username || undefined),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-channels'] });
      setForm({ chatId: '', title: '', username: '' });
    },
  });

  const [form, setForm] = useState({ chatId: '', title: '', username: '' });

  function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    if (!form.chatId || !form.title) return;
    createMut.mutate(form);
  }

  return (
    <section>
      <h2 className="text-lg font-semibold text-white mb-3">Telegram Channels</h2>

      <form onSubmit={handleAdd} className="flex flex-wrap gap-2 mb-4">
        <input
          value={form.chatId}
          onChange={e => setForm(f => ({ ...f, chatId: e.target.value }))}
          placeholder="Chat ID (e.g. -100123456789)"
          className="flex-1 min-w-40 bg-slate-800 text-white text-sm rounded px-3 py-2 border border-slate-600 focus:outline-none focus:border-indigo-500"
          required
        />
        <input
          value={form.title}
          onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
          placeholder="Channel name"
          className="flex-1 min-w-40 bg-slate-800 text-white text-sm rounded px-3 py-2 border border-slate-600 focus:outline-none focus:border-indigo-500"
          required
        />
        <input
          value={form.username}
          onChange={e => setForm(f => ({ ...f, username: e.target.value }))}
          placeholder="@username (optional)"
          className="flex-1 min-w-32 bg-slate-800 text-white text-sm rounded px-3 py-2 border border-slate-600 focus:outline-none focus:border-indigo-500"
        />
        <button
          type="submit"
          disabled={createMut.isPending}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-sm rounded font-medium"
        >
          {createMut.isPending ? 'Adding…' : 'Add Channel'}
        </button>
      </form>

      {isLoading ? (
        <p className="text-slate-400 text-sm">Loading channels…</p>
      ) : (channels ?? []).length === 0 ? (
        <p className="text-slate-400 text-sm">No channels configured yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-slate-700 text-left text-xs text-slate-400">
                <th className="pb-2 px-3">Name</th>
                <th className="pb-2 px-3">Chat ID</th>
                <th className="pb-2 px-3">Username</th>
                <th className="pb-2 px-3 text-right">Tips</th>
                <th className="pb-2 px-3 text-center">Status</th>
                <th className="pb-2 px-3 text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              {(channels ?? []).map(ch => (
                <ChannelRow
                  key={ch.id}
                  channel={ch}
                  onToggle={(id, active) => toggleMut.mutate({ id, active })}
                  onDelete={id => deleteMut.mutate(id)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

// ─── Tab navigation + page ────────────────────────────────────────────────────

type Tab = 'services' | 'channels' | 'actions';

export function AdminPage() {
  const [tab, setTab] = useState<Tab>('services');

  const tabs: { id: Tab; label: string }[] = [
    { id: 'services', label: 'Services' },
    { id: 'channels', label: 'Channels' },
    { id: 'actions', label: 'Actions' },
  ];

  return (
    <div className="min-h-screen bg-slate-950 text-white">
      <header className="bg-slate-900 border-b border-slate-800 px-4 py-3 flex items-center gap-6">
        <span className="font-bold text-indigo-400 text-lg">Stoixima Admin</span>
        <nav className="flex gap-1">
          {tabs.map(t => (
            <button
              key={t.id}
              onClick={() => setTab(t.id)}
              className={`px-3 py-1.5 rounded text-sm font-medium transition-colors ${
                tab === t.id
                  ? 'bg-indigo-600 text-white'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800'
              }`}
            >
              {t.label}
            </button>
          ))}
        </nav>
        <a href="/" className="ml-auto text-xs text-slate-400 hover:text-white">← Feed</a>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-8 space-y-10">
        {tab === 'services' && <ServicesPanel />}
        {tab === 'channels' && <ChannelsPanel />}
        {tab === 'actions' && <ActionsPanel />}
      </main>
    </div>
  );
}
