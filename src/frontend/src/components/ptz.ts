export type PtzVerb = 'preset' | 'trace';

export type PtzAction = {
  camera: number;
  verb: PtzVerb;
  number: number;
  raw: string;
};

export function formatPtzAction(camera: number, verb: PtzVerb, number: number): string {
  return `cam${camera}:${verb}:${number}`;
}

export function readPtzAction(raw: string): PtzAction | null {
  const match = /^cam([1-6]):(preset|trace):(\d{1,2})$/.exec(raw.trim());

  if (!match) {
    return null;
  }

  return {
    camera: Number(match[1]),
    verb: match[2] as PtzVerb,
    number: Number(match[3]),
    raw: raw.trim(),
  };
}

/** Plain-language label for the action log and the answer reveal. */
export function describePtzAction(raw: string): string {
  const action = readPtzAction(raw);

  if (!action) {
    return raw;
  }

  return `Camera ${action.camera}, ${action.verb} ${action.number}`;
}

/** Reads a JSON array of strings; anything else is treated as a plain-text answer. */
export function parseStringList(value: string): string[] | null {
  try {
    const parsed: unknown = JSON.parse(value);

    if (Array.isArray(parsed) && parsed.every((item) => typeof item === 'string')) {
      return parsed;
    }
  } catch {
    // Not JSON; the caller shows it as text.
  }

  return null;
}
