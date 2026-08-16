import { Injectable } from '@angular/core';

/**
 * DEMO ONLY: static AES-256 key shared with the backend's appsettings.Development.json
 * (Masking:DemoEncryptionKeyBase64). Keeps PII out of the wire and off server/proxy logs,
 * which is the point of this demo - it is NOT a boundary against this browser's own already-
 * authorised session (anyone with Sources/Console here can find it too). Production would
 * issue short-lived per-session keys via an authenticated exchange or KMS instead of baking
 * a shared secret into the SPA bundle.
 */
const DEMO_KEY_BASE64 = 'OaSt56jz74LncKaAJYN9JEcr3/H76tC3KLNUnaVQP+Y=';

function base64ToBytes(base64: string): Uint8Array<ArrayBuffer> {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

/** Decrypts "base64(nonce):base64(ciphertext+tag)" blobs produced by AesGcmFieldCipher. */
@Injectable({ providedIn: 'root' })
export class CryptoService {
  private readonly keyPromise = crypto.subtle.importKey(
    'raw',
    base64ToBytes(DEMO_KEY_BASE64),
    'AES-GCM',
    false,
    ['decrypt']
  );

  async decryptField(blob: string): Promise<string> {
    const [ivB64, dataB64] = blob.split(':');
    const key = await this.keyPromise;
    const plainBuf = await crypto.subtle.decrypt(
      { name: 'AES-GCM', iv: base64ToBytes(ivB64) },
      key,
      base64ToBytes(dataB64)
    );
    return new TextDecoder().decode(plainBuf);
  }
}
