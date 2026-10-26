type QuietMember = {
  worn: boolean;
  lastActiveAt: string | null;
};

function daysSince(iso: string): number {
  return Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
}

/** How long a member has been quiet, for the roster note. Null when they are not worn. */
export function quietFor(member: QuietMember): string | null {
  if (!member.worn) {
    return null;
  }

  if (!member.lastActiveAt) {
    return 'quiet for a while';
  }

  const days = daysSince(member.lastActiveAt);
  return days >= 1 ? `quiet ${days} days` : 'quiet today';
}

/** The same fact as a full sentence, for a bag view. */
export function quietSentence(member: QuietMember): string | null {
  if (!member.worn) {
    return null;
  }

  if (!member.lastActiveAt) {
    return 'Has been quiet for a while.';
  }

  const days = daysSince(member.lastActiveAt);
  return days >= 1 ? `Has been quiet for ${days} days.` : 'Has been quiet today.';
}
