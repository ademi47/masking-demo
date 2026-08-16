/**
 * Single place that knows API paths. Readable names on the left, so components stay
 * legible; if you later adopt opaque route IDs (Tier 2), only the values change here.
 *
 * All paths are RELATIVE - the browser never learns a backend hostname.
 */
export const API = {
  me:            '/api/v1/members/me',
  meUnmaskedDemo:'/api/v1/members/me/unmasked-demo',
  export:        '/api/v1/members/me/export',
  updateProfile: '/api/v1/members/me/profile',
  updateUnsafe:  '/api/v1/members/me/profile-unsafe-demo'
} as const;
