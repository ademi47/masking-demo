import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api-routes';
import { ApiResponse, MemberDto, UpdateProfileRequest } from './models';

@Injectable({ providedIn: 'root' })
export class MemberService {
  private readonly http = inject(HttpClient);

  /** Normal call. No masking logic here - the server decides what leaves the perimeter. */
  getMe(): Observable<ApiResponse<MemberDto>> {
    return this.http.get<ApiResponse<MemberDto>>(API.me);
  }

  /** DEMO: privileged role sees unmasked values and triggers an audit log entry. */
  getMeAsOfficer(): Observable<ApiResponse<MemberDto>> {
    return this.http.get<ApiResponse<MemberDto>>(API.me, {
      headers: new HttpHeaders({ 'X-Demo-Role': 'officer' })
    });
  }

  /** DEMO: the "before" payload, straight off the entity. */
  getRawEntity(): Observable<ApiResponse<MemberDto>> {
    return this.http.get<ApiResponse<MemberDto>>(API.meUnmaskedDemo);
  }

  /** DEMO: privileged role, but every masked field arrives as an AES-GCM ciphertext blob. */
  getMeEncrypted(): Observable<ApiResponse<MemberDto>> {
    return this.http.get<ApiResponse<MemberDto>>(API.me, {
      headers: new HttpHeaders({ 'X-Demo-Role': 'officer', 'X-Demo-Reveal': 'encrypted' })
    });
  }

  updateProfile(request: UpdateProfileRequest): Observable<ApiResponse<MemberDto>> {
    return this.http.put<ApiResponse<MemberDto>>(API.updateProfile, request);
  }

  /** DEMO: round-trips a masked NRIC back to the server to show the 400. */
  updateWithMaskedNric(nric: string): Observable<unknown> {
    return this.http.put(API.updateUnsafe, {
      nric,
      email: 'weiming.tan@example.com',
      mobileNumber: '91234567',
      mailingAddress: 'Blk 123 Ang Mo Kio Ave 6'
    });
  }
}
