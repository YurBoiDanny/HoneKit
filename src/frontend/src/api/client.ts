import type { BagDto, TeamDto } from './types';

// The POC has seeded members instead of accounts; this is the demo member.
export const DEMO_MEMBER_ID = 'danny';
export const DEMO_ROLE = 'camera';

async function get<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: { Accept: 'application/json' } });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText} from ${path}`);
  }

  return (await response.json()) as T;
}

// Same-origin prefixes; nginx proxies them in the published stack.
export const fetchTeam = (role: string) => get<TeamDto>(`/api/roster/teams/${role}`);

export const fetchBag = (memberId: string) => get<BagDto>(`/api/roster/members/${memberId}/bag`);
