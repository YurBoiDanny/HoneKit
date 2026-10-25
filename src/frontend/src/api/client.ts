import type {
  AttemptResultDto,
  BagDto,
  CardDto,
  DeckSummaryDto,
  NextCardDto,
  SessionDto,
  SessionResultDto,
  TeamDto,
} from './types';

// The POC has seeded members instead of accounts; this is the demo member.
export const DEMO_MEMBER_ID = 'danny';
export const DEMO_ROLE = 'camera';

export interface DeckDetail {
  id: string;
  role: string;
  name: string;
  skillId: string;
  cards: CardDto[];
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { Accept: 'application/json', ...(init?.body ? { 'Content-Type': 'application/json' } : {}) },
  });

  if (!response.ok) {
    let detail = `${response.status} ${response.statusText}`;
    try {
      const problem = (await response.json()) as { title?: string; detail?: string };
      detail = problem.detail ?? problem.title ?? detail;
    } catch {
      // keep the status line
    }
    throw new Error(detail);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

const post = <T>(path: string, body: unknown) =>
  request<T>(path, { method: 'POST', body: JSON.stringify(body) });

// Same-origin prefixes; nginx proxies them in the published stack.
export const fetchTeam = (role: string) => request<TeamDto>(`/api/roster/teams/${role}`);

export const fetchBag = (memberId: string) => request<BagDto>(`/api/roster/members/${memberId}/bag`);

export const fetchDecks = (role: string) => request<DeckSummaryDto[]>(`/api/content/decks?role=${role}`);

export const fetchDeck = (deckId: string) => request<DeckDetail>(`/api/content/decks/${deckId}`);

export const startSession = (memberId: string, deckId: string) =>
  post<SessionDto>('/api/progress/sessions', { memberId, deckId });

export const fetchNextCard = (sessionId: string) =>
  request<NextCardDto>(`/api/progress/sessions/${sessionId}/next`);

export const submitAttempt = (sessionId: string, cardId: string, answer: string) =>
  post<AttemptResultDto>(`/api/progress/sessions/${sessionId}/attempts`, { cardId, answer });

export const finishSession = (sessionId: string, exitedEarly: boolean) =>
  post<SessionResultDto>(`/api/progress/sessions/${sessionId}/finish`, { exitedEarly });

export const setShowAnswerOnWrong = (memberId: string, showAnswerOnWrong: boolean) =>
  request<void>(`/api/roster/members/${memberId}/preferences`, {
    method: 'PATCH',
    body: JSON.stringify({ showAnswerOnWrong }),
  });
