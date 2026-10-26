import type {
  AiStatusDto,
  AttemptResultDto,
  BagDto,
  BagWallDto,
  CardDto,
  DeckSummaryDto,
  NextCardDto,
  ParseFailureDto,
  CreateCardRequest,
  CreateDeckRequest,
  LessonDto,
  OverrideWarningDto,
  PlanDto,
  PlanSummaryDto,
  RecommendationDto,
  RequirementSetDto,
  RequirementSummaryDto,
  SessionDto,
  SkillDto,
  SessionResultDto,
  TeamBagDto,
  TeamDto,
  UpdateRequirementSetRequest,
  ManualRequirementSetRequest,
} from './types';

// The POC has seeded members instead of accounts; this is the demo member.
export const DEMO_MEMBER_ID = 'danny';
export const DEMO_ROLE = 'camera';

export class ApiError extends Error {
  readonly details: string[];

  constructor(message: string, details: string[] = []) {
    super(message);
    this.name = 'ApiError';
    this.details = details;
  }
}

export interface DeckDetail {
  id: string;
  role: string;
  name: string;
  skillId: string;
  cards: CardDto[];
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const isForm = init?.body instanceof FormData;

  const response = await fetch(path, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body && !isForm ? { 'Content-Type': 'application/json' } : {}),
    },
  });

  if (!response.ok) {
    let detail = `${response.status} ${response.statusText}`;
    let details: string[] = [];

    try {
      const body = (await response.json()) as
        | string
        | { title?: string; detail?: string; errors?: string[] };
      if (typeof body === 'string') {
        detail = body;
      } else {
        details = body.errors ?? [];
        detail = body.detail ?? body.title ?? (details.join(' ') || detail);
      }
    } catch {
      // keep the status line
    }

    throw new ApiError(detail, details);
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

export const fetchBagWall = (role: string) =>
  request<BagWallDto>(`/api/roster/teams/${role}/members`);

export const fetchTeamBag = (role: string) =>
  request<TeamBagDto>(`/api/roster/teams/${role}/bag`);

export const updateBagStyle = (memberId: string, colour: string, style: string) =>
  request<void>(`/api/roster/members/${memberId}/bag`, {
    method: 'PATCH',
    body: JSON.stringify({ colour, style }),
  });

export const acknowledgeTool = (memberId: string, skillId: string) =>
  request<void>(`/api/roster/members/${memberId}/bag/acknowledge`, {
    method: 'POST',
    body: JSON.stringify({ skillId }),
  });

export const fetchSkills = (role: string) =>
  request<SkillDto[]>(`/api/content/roles/${role}/skills`);

export const fetchAiStatus = (probe = false) =>
  request<AiStatusDto>(`/api/requirements/ai-status${probe ? '?probe=true' : ''}`);

export const fetchRequirementSets = () => request<RequirementSummaryDto[]>('/api/requirements');

export const fetchRequirementSet = (id: string) =>
  request<RequirementSetDto>(`/api/requirements/${id}`);

export const updateRequirementSet = (id: string, body: UpdateRequirementSetRequest) =>
  request<RequirementSetDto>(`/api/requirements/${id}`, {
    method: 'PUT',
    body: JSON.stringify(body),
  });

export const confirmRequirementSet = (id: string) =>
  post<RequirementSetDto>(`/api/requirements/${id}/confirm`, {});

export const createManualRequirementSet = (body: ManualRequirementSetRequest) =>
  post<RequirementSetDto>('/api/requirements/manual', body);

export type BriefParseResult =
  | { ok: true; set: RequirementSetDto }
  | { ok: false; failure: ParseFailureDto };

/** Multipart upload; returns the failure body intact so the manual form can keep the text. */
export async function parseBrief(file: File | null, text: string): Promise<BriefParseResult> {
  const form = new FormData();
  if (file) {
    form.append('file', file);
  } else {
    form.append('text', text);
  }

  const response = await fetch('/api/requirements', {
    method: 'POST',
    body: form,
    headers: { Accept: 'application/json' },
  });

  if (response.ok) {
    return { ok: true, set: (await response.json()) as RequirementSetDto };
  }

  const fallback: ParseFailureDto = {
    detail: `${response.status} ${response.statusText}`,
    sourceName: file?.name ?? 'Typed brief',
    sourceText: text,
    errors: [],
  };

  try {
    const body = (await response.json()) as Partial<ParseFailureDto> & { detail?: string };
    return { ok: false, failure: { ...fallback, ...body } };
  } catch {
    return { ok: false, failure: fallback };
  }
}

export const fetchLessons = (role: string) =>
  request<LessonDto[]>(`/api/content/roles/${role}/lessons`);

export const createDeck = (body: CreateDeckRequest) =>
  post<DeckSummaryDto>('/api/content/decks', body);

export const addCard = (deckId: string, body: CreateCardRequest) =>
  post<CardDto>(`/api/content/decks/${deckId}/cards`, body);

export const fetchRecommendations = (requirementId: string, strategy: string) =>
  post<RecommendationDto>('/api/recommendations', { requirementId, strategy });

export const checkOverrides = (requirementId: string, memberIds: string[]) =>
  post<OverrideWarningDto[]>('/api/recommendations/check', { requirementId, memberIds });

export const savePlan = (requirementId: string, strategy: string, memberIds: string[]) =>
  post<PlanDto>('/api/plans', { requirementId, strategy, memberIds });

export const fetchPlans = () => request<PlanSummaryDto[]>('/api/plans');

export const fetchPlan = (id: string) => request<PlanDto>(`/api/plans/${id}`);

export const assignDeck = (deckId: string, memberIds: string[]) =>
  request<void>(`/api/content/decks/${deckId}/assign`, {
    method: 'POST',
    body: JSON.stringify({ memberIds }),
  });

export const setShowAnswerOnWrong = (memberId: string, showAnswerOnWrong: boolean) =>
  request<void>(`/api/roster/members/${memberId}/preferences`, {
    method: 'PATCH',
    body: JSON.stringify({ showAnswerOnWrong }),
  });
