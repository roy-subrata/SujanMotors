/**
 * Deterministic zone -> accent color mapping for Warehouse Location chips and
 * printed labels. There is no color field on the backend (`WarehouseLocation`
 * entity/DTO) — the tab/chip color is derived purely client-side from the
 * `zone` string so the same zone letter always renders the same color across
 * the list page and the printed label.
 *
 * Palette matches the design handoff's fixed brand accents (same 4 colors used
 * elsewhere in the label system): red, teal, blue, slate.
 */
export const ZONE_ACCENT_PALETTE = ['#B0392E', '#0F766E', '#1E40AF', '#3F3F46'] as const;

/**
 * Pick an accent color for a zone code. Anchored on the zone's first
 * character so single-letter zones (the common case, e.g. "A", "B", "C")
 * land on the exact same rotation the design handoff hard-codes (A -> red,
 * B -> teal, C -> blue, D -> slate, then repeats), while any other zone
 * string still gets a stable color via the same basis.
 */
export function getZoneColor(zone: string | null | undefined): string {
    const z = (zone ?? '').trim().toUpperCase();
    if (!z) return ZONE_ACCENT_PALETTE[ZONE_ACCENT_PALETTE.length - 1];

    const code = z.charCodeAt(0);
    const index = code >= 65 && code <= 90 ? code - 65 : code;
    return ZONE_ACCENT_PALETTE[index % ZONE_ACCENT_PALETTE.length];
}
