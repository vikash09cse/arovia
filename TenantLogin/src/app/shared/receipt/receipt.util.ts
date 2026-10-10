import { inject } from '@angular/core';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';
import { firstValueFrom } from 'rxjs';

interface ReceiptHtmlData {
  paymentId: string;
  receiptNumber: string;
  html: string;
}

export async function printPaymentReceipt(api: ApiService, paymentId: string): Promise<void> {
  const res = await firstValueFrom(api.get<ApiResult<ReceiptHtmlData>>(`/payments/${paymentId}/receipt`));
  const html = res.data?.html;
  if (!html) throw new Error(res.message || 'Receipt HTML missing.');

  // Print via a hidden iframe so browsers do not treat this as a pop-up
  // (window.open after await is blocked by Chrome).
  const iframe = document.createElement('iframe');
  iframe.setAttribute('aria-hidden', 'true');
  iframe.setAttribute('title', 'Print receipt');
  Object.assign(iframe.style, {
    position: 'fixed',
    right: '0',
    bottom: '0',
    width: '0',
    height: '0',
    border: '0',
    opacity: '0',
    pointerEvents: 'none'
  });
  document.body.appendChild(iframe);

  const doc = iframe.contentDocument ?? iframe.contentWindow?.document;
  const win = iframe.contentWindow;
  if (!doc || !win) {
    iframe.remove();
    throw new Error('Unable to prepare print view.');
  }

  doc.open();
  doc.write(html);
  doc.close();

  await waitForPrintReady(doc);

  const cleanup = () => {
    try { iframe.remove(); } catch { /* ignore */ }
  };

  win.addEventListener('afterprint', cleanup, { once: true });
  setTimeout(cleanup, 60_000);

  win.focus();
  win.print();
}

function waitForPrintReady(doc: Document): Promise<void> {
  return new Promise((resolve) => {
    const finish = () => setTimeout(resolve, 150);
    const images = Array.from(doc.images ?? []);
    if (images.length === 0) {
      finish();
      return;
    }

    let remaining = images.length;
    const onDone = () => {
      remaining -= 1;
      if (remaining <= 0) finish();
    };

    for (const img of images) {
      if (img.complete) onDone();
      else {
        img.addEventListener('load', onDone, { once: true });
        img.addEventListener('error', onDone, { once: true });
      }
    }

    // Safety timeout if an image never settles
    setTimeout(resolve, 2000);
  });
}

export async function downloadPaymentReceiptPdf(api: ApiService, paymentId: string): Promise<void> {
  const response = await firstValueFrom(api.getBlob(`/payments/${paymentId}/receipt.pdf`));
  const blob = response.body;
  if (!blob) throw new Error('PDF download failed.');

  if (blob.type.includes('application/json')) {
    const text = await blob.text();
    try {
      const parsed = JSON.parse(text) as { message?: string };
      throw new Error(parsed.message || 'Unable to download PDF.');
    } catch (e) {
      if (e instanceof SyntaxError) throw new Error('Unable to download PDF.');
      throw e;
    }
  }

  const disposition = response.headers.get('content-disposition') ?? '';
  const match = /filename="?([^"]+)"?/i.exec(disposition);
  const fileName = match?.[1] || `receipt-${paymentId}.pdf`;

  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

export async function downloadAdmissionFinalInvoicePdf(api: ApiService, admissionId: string): Promise<void> {
  let response;
  try {
    response = await firstValueFrom(api.getBlob(`/admissions/${admissionId}/final-invoice.pdf`));
  } catch (err: unknown) {
    throw new Error(await readBlobErrorMessage(err, 'Unable to download invoice.'));
  }

  const blob = response.body;
  if (!blob) throw new Error('PDF download failed.');

  if (blob.type.includes('application/json') || !response.ok) {
    const text = await blob.text();
    try {
      const parsed = JSON.parse(text) as { message?: string };
      throw new Error(parsed.message || 'Unable to download invoice.');
    } catch (e) {
      if (e instanceof Error && !(e instanceof SyntaxError)) throw e;
      throw new Error(text?.trim() || 'Unable to download invoice.');
    }
  }

  const disposition = response.headers.get('content-disposition') ?? '';
  const match = /filename\*?=(?:UTF-8''|")?([^\";]+)/i.exec(disposition);
  const fromHeader = match?.[1] ? decodeURIComponent(match[1].replace(/"/g, '')) : '';
  const fileName = fromHeader || `invoice-${admissionId}.pdf`;

  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

async function readBlobErrorMessage(err: unknown, fallback: string): Promise<string> {
  const httpErr = err as { error?: Blob | { message?: string }; message?: string };
  if (httpErr?.error instanceof Blob) {
    try {
      const text = await httpErr.error.text();
      const parsed = JSON.parse(text) as { message?: string };
      if (parsed.message) return parsed.message;
      if (text.trim()) return text.trim();
    } catch {
      /* ignore parse failures */
    }
  } else if (httpErr?.error && typeof httpErr.error === 'object' && 'message' in httpErr.error) {
    const msg = (httpErr.error as { message?: string }).message;
    if (msg) return msg;
  } else if (typeof httpErr?.message === 'string' && httpErr.message) {
    return httpErr.message;
  }
  return fallback;
}

/** Convenience injector-friendly helpers for components */
export function createReceiptActions() {
  const api = inject(ApiService);
  return {
    print: (paymentId: string) => printPaymentReceipt(api, paymentId),
    downloadPdf: (paymentId: string) => downloadPaymentReceiptPdf(api, paymentId)
  };
}
