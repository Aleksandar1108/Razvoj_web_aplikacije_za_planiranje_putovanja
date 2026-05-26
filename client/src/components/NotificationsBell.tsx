import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { UserNotification } from '../models/notification';
import { notificationsService } from '../services/notificationsService';
import { ApiError } from '../services/httpClient';

function formatWhen(iso: string): string {
  try {
    return new Date(iso).toLocaleString('sr-Latn', {
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

function BellIcon() {
  return (
    <svg
      className="notifications-bell-icon"
      viewBox="0 0 24 24"
      width="22"
      height="22"
      aria-hidden="true"
      focusable="false"
    >
      <path
        fill="currentColor"
        d="M12 2a6 6 0 0 0-6 6v2.1c0 .7-.3 1.4-.8 1.9L3.3 14.8A1 1 0 0 0 4 16.5h16a1 1 0 0 0 .7-1.7l-1.9-2.8c-.5-.5-.8-1.2-.8-1.9V8a6 6 0 0 0-6-6zm0 20a2.5 2.5 0 0 0 2.45-2h-4.9A2.5 2.5 0 0 0 12 22z"
      />
    </svg>
  );
}

export function NotificationsBell() {
  const { accessToken } = useAuth();
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<UserNotification[]>([]);
  const [unread, setUnread] = useState(0);
  const [loading, setLoading] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  const refresh = useCallback(async () => {
    if (!accessToken) {
      setItems([]);
      setUnread(0);
      return;
    }
    try {
      const [list, countRes] = await Promise.all([
        notificationsService.list(accessToken),
        notificationsService.unreadCount(accessToken),
      ]);
      setItems(list);
      setUnread(countRes.count);
    } catch {
    }
  }, [accessToken]);

  useEffect(() => {
    void refresh();
    const id = window.setInterval(() => void refresh(), 45000);
    return () => window.clearInterval(id);
  }, [refresh]);

  useEffect(() => {
    function onDocClick(ev: MouseEvent) {
      if (!wrapRef.current?.contains(ev.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, []);

  const toggle = async () => {
    const next = !open;
    setOpen(next);
    if (next && accessToken) {
      setLoading(true);
      try {
        await refresh();
      } finally {
        setLoading(false);
      }
    }
  };

  const markRead = async (n: UserNotification) => {
    if (!accessToken || n.isRead) return;
    try {
      await notificationsService.markRead(accessToken, n.id);
      setItems((prev) => prev.map((x) => (x.id === n.id ? { ...x, isRead: true } : x)));
      setUnread((c) => Math.max(0, c - 1));
    } catch {
    }
  };

  const markAll = async () => {
    if (!accessToken) return;
    try {
      await notificationsService.markAllRead(accessToken);
      setItems((prev) => prev.map((x) => ({ ...x, isRead: true })));
      setUnread(0);
    } catch (e) {
      alert(e instanceof ApiError ? e.message : 'Greška.');
    }
  };

  if (!accessToken) return null;

  return (
    <div className="notifications-wrap" ref={wrapRef}>
      <button
        type="button"
        className="btn ghost notifications-trigger notifications-trigger-icon"
        onClick={() => void toggle()}
        aria-expanded={open}
        aria-label={unread > 0 ? `Obaveštenja, ${unread} nepročitanih` : 'Obaveštenja'}
        title="Obaveštenja"
      >
        <BellIcon />
        {unread > 0 ? <span className="notifications-badge">{unread > 99 ? '99+' : unread}</span> : null}
      </button>

      {open ? (
        <div className="notifications-panel glass-panel">
          <div className="notifications-panel-head">
            <strong>Obaveštenja</strong>
            {unread > 0 ? (
              <button type="button" className="btn ghost btn-sm" onClick={() => void markAll()}>
                Označi sve pročitano
              </button>
            ) : null}
          </div>

          {loading ? (
            <p className="muted notifications-empty">Učitavanje…</p>
          ) : items.length === 0 ? (
            <p className="muted notifications-empty">Nema obaveštenja.</p>
          ) : (
            <ul className="notifications-list">
              {items.map((n) => (
                <li
                  key={n.id}
                  className={n.isRead ? 'notifications-item read' : 'notifications-item unread'}
                  onClick={() => void markRead(n)}
                >
                  <div className="notifications-item-title">{n.title}</div>
                  <p className="notifications-item-msg">{n.message}</p>
                  <div className="notifications-item-meta">
                    <span className="muted">{formatWhen(n.createdAtUtc)}</span>
                    {n.travelPlanId ? (
                      <Link
                        to={`/plans/${n.travelPlanId}`}
                        className="notifications-plan-link"
                        onClick={(e) => e.stopPropagation()}
                      >
                        Otvori plan
                      </Link>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
