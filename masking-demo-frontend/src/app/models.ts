export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
  traceId: string;
}

export interface ContributionDto {
  month: string;
  employer: string;
  amount: number;
  ordinaryAccount: number;
  specialAccount: number;
}

export interface MemberDto {
  memberId: number;
  nric: string;
  name: string;
  dateOfBirth: string;
  accountNumber: string;
  email: string;
  mobileNumber: string;
  mailingAddress: string;
  contributions: ContributionDto[];
}

/**
 * Every MemberDto string field the backend can serve encrypted (mirrors [Mask] on
 * MemberDto.cs). If a new field gains [Mask(...)] on the backend, add it here too -
 * this is the frontend's one place that has to know.
 */
export const MASKED_FIELDS = ['nric', 'dateOfBirth', 'accountNumber', 'email', 'mobileNumber'] as const;

export interface UpdateProfileRequest {
  email: string;
  mobileNumber: string;
  mailingAddress: string;
}
