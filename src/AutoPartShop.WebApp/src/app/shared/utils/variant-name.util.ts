/**
 * Compose a standalone display label for a product line that may be a variant.
 *
 * Variant identity is stored separately: the product carries the base name
 * ("Looking Glass") and the variant carries only a short distinguishing label
 * ("LH"). Wherever a line stands on its own (POS line, search row, receipt) the
 * two are joined as "Base - Label". Where the base name is already shown as a
 * heading, render the bare label instead — do not call this.
 *
 * The backend exposes the same composed value as `displayName` on flattened
 * product responses; prefer that when available and use this only when building
 * the label client-side.
 *
 * @param name        Base product name.
 * @param variantName Short variant label, if the line is a variant.
 */
export function composeVariantDisplayName(name: string, variantName?: string | null): string {
  const base = (name ?? '').trim();
  const variant = (variantName ?? '').trim();
  if (!variant) return base;
  if (!base) return variant;
  // Defensive: some legacy/seed variant labels already embed the full base name
  // ("Bosch Alternator 65A — Standard"). Avoid duplicating it.
  if (variant.toLowerCase().startsWith(base.toLowerCase())) return variant;
  return `${base} - ${variant}`;
}
