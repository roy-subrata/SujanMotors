/** Derive a 1-2 letter avatar label from a user's full name (e.g. "Roy Subrata" -> "RS"). */
export function getUserInitials(fullName: string | null | undefined): string {
  const trimmed = (fullName ?? '').trim();
  if (!trimmed) return '?';
  const names = trimmed.split(/\s+/);
  if (names.length >= 2) {
    return `${names[0][0]}${names[names.length - 1][0]}`.toUpperCase();
  }
  return names[0].substring(0, 2).toUpperCase();
}
