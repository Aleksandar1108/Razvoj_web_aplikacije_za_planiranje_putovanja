import jsQR from 'jsqr';
import type { QrPayloadV1 } from '../models/share';

export function tryParseQrPayload(text: string): QrPayloadV1 | null {
  const trimmed = text.trim();
  if (!trimmed.startsWith('{')) return null;
  try {
    const obj = JSON.parse(trimmed) as unknown;
    if (!obj || typeof obj !== 'object') return null;
    const o = obj as Record<string, unknown>;
    if (o.v !== 1) return null;
    if (typeof o.pid !== 'string' || typeof o.p !== 'string' || typeof o.t !== 'string') return null;
    if (o.p !== 'view' && o.p !== 'edit') return null;
    return { v: 1, pid: o.pid, p: o.p, t: o.t };
  } catch {
    return null;
  }
}

export async function decodeQrFromImageFile(file: File): Promise<string> {
  const bitmap = await createImageBitmap(file);
  const canvas = document.createElement('canvas');
  canvas.width = bitmap.width;
  canvas.height = bitmap.height;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('Canvas nije dostupan.');
  ctx.drawImage(bitmap, 0, 0);
  const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
  const qr = jsQR(imageData.data, imageData.width, imageData.height);
  if (!qr?.data) {
    throw new Error('QR kod nije prepoznat. Probaj jaču svjetlost / veću rezoluciju slike.');
  }
  return qr.data;
}

export function guestSharePlanPath(planId: string, token: string): string {
  const params = new URLSearchParams({ t: token, p: 'view' });
  return `/share/plans/${encodeURIComponent(planId)}?${params.toString()}`;
}
