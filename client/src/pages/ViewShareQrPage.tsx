import { useMemo, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ApiError } from '../services/httpClient';
import { decodeQrFromImageFile, guestSharePlanPath, tryParseQrPayload } from '../utils/qrShare';

export function ViewShareQrPage() {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement | null>(null);

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const canSubmit = useMemo(() => !busy, [busy]);

  async function onPickFile(file: File) {
    setError(null);
    setInfo(null);
    setBusy(true);
    try {
      const url = URL.createObjectURL(file);
      setPreviewUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return url;
      });

      const raw = await decodeQrFromImageFile(file);
      const payload = tryParseQrPayload(raw);
      if (!payload) {
        throw new Error(
          'QR kod nije u podržanom formatu. Koristi QR koji je generisan u aplikaciji (pregled ili uređivanje).'
        );
      }

      if (payload.p === 'edit') {
        setInfo(
          'Ovaj QR kod je za uređivanje plana. Da biste ga učitali i menjali sadržaj, prvo se prijavite na nalog koji ima to pravo.'
        );
        return;
      }

      navigate(guestSharePlanPath(payload.pid, payload.t));
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
    <div className="auth-page">
      <div className="auth-page-bg" aria-hidden />
      <div className="page narrow auth-panel">
        <p className="auth-kicker">Planiranje putovanja</p>
        <h1 className="auth-title">Učitaj QR</h1>
        <p className="muted auth-lead">
          Učitaj sliku QR koda za pregled plana — prijava nije potrebna ako je QR izdat samo za gledanje.
        </p>

        <div className="card glass-panel auth-form" style={{ marginTop: 20 }}>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
            <button
              type="button"
              className="btn btn-glow primary btn-lg"
              disabled={!canSubmit}
              onClick={() => inputRef.current?.click()}
            >
              Učitaj QR
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

          {info ? (
            <div className="card" style={{ marginTop: 16, borderColor: 'rgba(234, 179, 8, 0.35)' }}>
              <p style={{ margin: 0 }}>{info}</p>
              <p style={{ marginTop: 12, marginBottom: 0 }}>
                <Link to="/login" className="btn primary">
                  Prijavi se
                </Link>
              </p>
            </div>
          ) : null}

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
                style={{ width: 'min(420px, 100%)', borderRadius: 16, border: '1px solid rgba(15, 23, 42, 0.12)' }}
              />
            </div>
          ) : null}
        </div>

      </div>
    </div>
  );
}
