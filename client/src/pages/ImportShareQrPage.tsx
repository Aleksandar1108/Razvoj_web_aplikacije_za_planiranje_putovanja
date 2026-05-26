import { useMemo, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { claimShareLink } from '../services/sharingService';
import { ApiError } from '../services/httpClient';
import { decodeQrFromImageFile, guestSharePlanPath, tryParseQrPayload } from '../utils/qrShare';

export function ImportShareQrPage() {
  const navigate = useNavigate();
  const { accessToken } = useAuth();
  const inputRef = useRef<HTMLInputElement | null>(null);

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const canSubmit = useMemo(() => Boolean(accessToken) && !busy, [accessToken, busy]);

  async function onPickFile(file: File) {
    setError(null);
    setBusy(true);
    try {
      const url = URL.createObjectURL(file);
      setPreviewUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return url;
      });

      const raw = await decodeQrFromImageFile(file);
      const payload = tryParseQrPayload(raw);
      if (payload) {
        if (payload.p === 'view') {
          navigate(guestSharePlanPath(payload.pid, payload.t));
          return;
        }
        await claimShareLink(payload.t, accessToken);
        navigate(`/plans/${encodeURIComponent(payload.pid)}?p=${encodeURIComponent(payload.p)}`);
        return;
      }

      const token = raw.trim();
      if (token.length < 10) throw new Error('QR sadržaj nije validan.');

      const claimed = await claimShareLink(token, accessToken);
      navigate(`/plans/${encodeURIComponent(claimed.travelPlanId)}?p=${encodeURIComponent(claimed.permission)}`);
    } catch (e) {
      const msg =
        e instanceof ApiError
          ? e.message
          : e instanceof Error
            ? e.message
            : 'Učitavanje QR koda nije uspjelo.';
      setError(msg);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="page">
      <div className="card glass-panel" style={{ maxWidth: 920, margin: '0 auto' }}>
        <h1 style={{ marginTop: 0 }}>Učitaj QR</h1>
        <p className="muted" style={{ marginTop: 8 }}>
          Odaberi PNG/JPG snimak QR koda. Aplikacija će ga dekodirati, povezati plan sa tvojim nalogom (claim) i otvoriti
          detalje plana.
        </p>

        {!accessToken ? <p className="error">Moraš biti ulogovan/na da učitaš QR.</p> : null}

        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginTop: 16, alignItems: 'center' }}>
          <button
            type="button"
            className="btn btn-glow primary"
            disabled={!canSubmit}
            onClick={() => inputRef.current?.click()}
          >
            Odaberi sliku
          </button>
          <input
            ref={inputRef}
            type="file"
            accept="image/*"
            style={{ display: 'none' }}
            onChange={(e) => {
              const f = e.target.files?.[0];
              e.target.value = '';
              if (!f) return;
              void onPickFile(f);
            }}
          />
          {busy ? <span className="muted">Obrada…</span> : null}
        </div>

        {error ? (
          <p className="error" style={{ marginTop: 14 }}>
            {error}
          </p>
        ) : null}

        {previewUrl ? (
          <div style={{ marginTop: 18 }}>
            <p className="muted" style={{ marginBottom: 10 }}>
              Pregled slike
            </p>
            <img
              src={previewUrl}
              alt="Pregled učitane slike QR koda"
              style={{ width: 'min(520px, 100%)', borderRadius: 16, border: '1px solid rgba(15, 23, 42, 0.12)' }}
            />
          </div>
        ) : null}

      </div>
    </div>
  );
}
