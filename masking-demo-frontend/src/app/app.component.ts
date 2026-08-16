import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MemberService } from './member.service';
import { CryptoService } from './crypto.service';
import { MASKED_FIELDS, MemberDto } from './models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  styles: [`
    .wrap { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem 4rem; }
    h1 { font-size: 1.35rem; margin: 0 0 0.25rem; }
    .sub { color: var(--muted); font-size: 0.9rem; margin: 0 0 1.75rem; }
    .hint {
      background: #1b2130; border: 1px solid #2b3648; border-left: 3px solid var(--accent);
      border-radius: 6px; padding: 0.75rem 1rem; font-size: 0.85rem; margin-bottom: 1.5rem;
    }
    .actions { display: flex; flex-wrap: wrap; gap: 0.6rem; margin-bottom: 1.5rem; }
    .grid { display: grid; grid-template-columns: 1fr; gap: 1rem; }
    @media (min-width: 900px) { .grid { grid-template-columns: 1fr 1fr; } }
    .card { background: var(--panel); border: 1px solid var(--border); border-radius: 10px; padding: 1rem; }
    .card h2 { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.06em;
               color: var(--muted); margin: 0 0 0.75rem; font-weight: 600; }
    .field { display: flex; justify-content: space-between; gap: 1rem;
             padding: 0.4rem 0; border-bottom: 1px dashed var(--border); font-size: 0.88rem; }
    .field:last-child { border-bottom: 0; }
    .field .k { color: var(--muted); }
    .field .v { font-family: ui-monospace, "Cascadia Code", monospace; }
    .masked { color: var(--ok); }
    .exposed { color: var(--danger); }
    .status { font-size: 0.85rem; color: var(--muted); min-height: 1.4rem; margin-bottom: 0.75rem; }
    .err { color: var(--danger); }
  `],
  template: `
    <div class="wrap">
      <h1>Field-Level Masking Demo</h1>
      <p class="sub">.NET 8 microservice + Angular &mdash; NRIC, account number, DOB, email and mobile
         are masked by the serialiser before the payload leaves the API.</p>

      <div class="hint">
        Press <strong>F12</strong> &rarr; <strong>Network</strong> &rarr; filter <strong>Fetch/XHR</strong>,
        then click the buttons below. Compare the <em>Preview</em> tab of each call.
        Note every request goes to <code>localhost:4200/api/...</code> &mdash; the backend host
        never appears in the browser.
      </div>

      <div class="actions">
        <button class="primary" (click)="loadMasked()">GET /me (masked)</button>
        <button (click)="loadOfficer()">GET /me as officer (unmasked + audited)</button>
        <button (click)="loadEncrypted()">GET /me encrypted (ciphertext on wire, decrypted in DOM)</button>
        <button (click)="loadRaw()">GET /unmasked-demo (the "before" payload)</button>
        <button class="danger" (click)="sendMaskedBack()">PUT masked NRIC back &rarr; expect 400</button>
        <button (click)="downloadCsv()">Download CSV export</button>
      </div>

      <p class="status" [class.err]="isError()">{{ status() }}</p>

      <div class="grid">
        <div class="card">
          <h2>Rendered view</h2>
          @if (member(); as m) {
            <div class="field"><span class="k">Member ID</span><span class="v">{{ m.memberId }}</span></div>
            <div class="field"><span class="k">Name</span><span class="v">{{ m.name }}</span></div>
            <div class="field"><span class="k">NRIC</span>
              <span class="v" [class.masked]="hasMask(m.nric)" [class.exposed]="!hasMask(m.nric)">{{ m.nric }}</span></div>
            <div class="field"><span class="k">Date of birth</span>
              <span class="v" [class.masked]="m.dateOfBirth.length === 4" [class.exposed]="m.dateOfBirth.length > 4">{{ m.dateOfBirth }}</span></div>
            <div class="field"><span class="k">Account number</span>
              <span class="v" [class.masked]="hasMask(m.accountNumber)" [class.exposed]="!hasMask(m.accountNumber)">{{ m.accountNumber }}</span></div>
            <div class="field"><span class="k">Email</span>
              <span class="v" [class.masked]="hasMask(m.email)" [class.exposed]="!hasMask(m.email)">{{ m.email }}</span></div>
            <div class="field"><span class="k">Mobile</span>
              <span class="v" [class.masked]="hasMask(m.mobileNumber)" [class.exposed]="!hasMask(m.mobileNumber)">{{ m.mobileNumber }}</span></div>
            <div class="field"><span class="k">Contributions</span><span class="v">{{ m.contributions.length }} months</span></div>
          } @else {
            <p class="sub">Nothing loaded yet.</p>
          }
        </div>

        <div class="card">
          <h2>Raw response body (same bytes as the Network tab)</h2>
          <pre>{{ raw() || 'Nothing loaded yet.' }}</pre>
        </div>
      </div>
    </div>
  `
})
export class AppComponent {
  private readonly members = inject(MemberService);
  private readonly crypto = inject(CryptoService);

  readonly member = signal<any | null>(null);
  readonly raw = signal<string>('');
  readonly status = signal<string>('');
  readonly isError = signal<boolean>(false);

  hasMask(value: string): boolean {
    return value?.includes('*') ?? false;
  }

  loadMasked(): void {
    this.set('Calling GET /api/v1/members/me ...');
    this.members.getMe().subscribe({
      next: r => this.apply(r, 'Masked response. NRIC, account, DOB, email and mobile are redacted server-side.'),
      error: e => this.fail(e)
    });
  }

  loadOfficer(): void {
    this.set('Calling GET /api/v1/members/me with X-Demo-Role: officer ...');
    this.members.getMeAsOfficer().subscribe({
      next: r => this.apply(r, 'Unmasked - privileged role. Check the API console for the UNMASKED PII ACCESS audit line.'),
      error: e => this.fail(e)
    });
  }

  loadEncrypted(): void {
    this.set('Calling GET /api/v1/members/me with X-Demo-Role: officer + X-Demo-Reveal: encrypted ...');
    this.members.getMeEncrypted().subscribe({
      next: async r => {
        this.raw.set(JSON.stringify(r, null, 2));

        const data = r.data;
        if (!data) {
          this.set('No data returned.', true);
          return;
        }

        try {
          const decrypted: MemberDto = { ...data };
          await Promise.all(MASKED_FIELDS.map(async field => {
            (decrypted as any)[field] = await this.crypto.decryptField(data[field]);
          }));

          this.member.set(decrypted);
          this.set('Ciphertext on the wire (see raw response below) - decrypted client-side via ' +
            'Web Crypto before rendering. Network tab shows the blobs above; Elements shows the real values.');
        } catch (e) {
          this.set(`Client-side decryption failed: ${e}`, true);
        }
      },
      error: e => this.fail(e)
    });
  }

  loadRaw(): void {
    this.set('Calling GET /api/v1/members/me/unmasked-demo ...');
    this.members.getRawEntity().subscribe({
      next: r => this.apply(r, 'This is what the payload looked like BEFORE masking. Demo endpoint only.'),
      error: e => this.fail(e)
    });
  }

  sendMaskedBack(): void {
    this.set('Sending nric "S****567D" back to the server ...');
    this.members.updateWithMaskedNric('S****567D').subscribe({
      next: r => {
        this.raw.set(JSON.stringify(r, null, 2));
        this.set('Unexpected 200 - the write-path guard is not wired up.', true);
      },
      error: (e: HttpErrorResponse) => {
        this.raw.set(JSON.stringify(e.error, null, 2));
        this.set(`Blocked with HTTP ${e.status} - asterisks never reach the database.`);
      }
    });
  }

  downloadCsv(): void {
    window.open('/api/v1/members/me/export', '_blank');
    this.set('CSV opened in a new tab - masked, because the export path calls Mask.Apply directly.');
  }

  private apply(response: any, message: string): void {
    this.member.set(response?.data ?? null);
    this.raw.set(JSON.stringify(response, null, 2));
    this.set(message);
  }

  private fail(error: HttpErrorResponse): void {
    this.raw.set(JSON.stringify(error.error ?? error.message, null, 2));
    this.set(`Request failed: HTTP ${error.status}. Is the API running on port 5219?`, true);
  }

  private set(message: string, isError = false): void {
    this.status.set(message);
    this.isError.set(isError);
  }
}
