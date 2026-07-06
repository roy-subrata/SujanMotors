import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';

// ── StatusPill ────────────────────────────────────────────────────────────────

class StatusPill extends StatelessWidget {
  const StatusPill({super.key, required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final cfg = _cfg(label, scheme);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: cfg.bg,
        borderRadius: BorderRadius.circular(99),
        border: cfg.border != null ? Border.all(color: cfg.border!) : null,
      ),
      child: Text(
        label,
        style: GoogleFonts.instrumentSans(
          fontSize: 10.5,
          fontWeight: FontWeight.w600,
          color: cfg.fg,
        ),
      ),
    );
  }

  static _PillConfig _cfg(String l, ColorScheme scheme) {
    switch (l.toUpperCase()) {
      case 'PAID':
      case 'COMPLETED':
        return const _PillConfig(AppColors.green, AppColors.greenBg, null);
      case 'DUE':
      case 'OVERDUE':
      case 'RETURN':
      case 'RETURNED':
      case 'OUT OF STOCK':
        return const _PillConfig(
            AppColors.red, AppColors.redBg, AppColors.redBorder);
      case 'PARTIAL':
      case 'PARTIALLY_PAID':
      case 'LOW STOCK':
      case 'PENDING':
        return const _PillConfig(
            AppColors.amber, AppColors.amberBg, AppColors.amberBorder);
      default:
        return _PillConfig(
          scheme.onSurface.withAlpha(160),
          scheme.outline.withAlpha(30),
          scheme.outline,
        );
    }
  }
}

class _PillConfig {
  const _PillConfig(this.fg, this.bg, this.border);
  final Color fg;
  final Color bg;
  final Color? border;
}

// ── FilterChipRow ─────────────────────────────────────────────────────────────

class FilterChipRow extends StatelessWidget {
  const FilterChipRow({
    super.key,
    required this.chips,
    required this.selected,
    required this.onSelect,
  });

  final List<FilterChipData> chips;
  final int selected;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Row(
        children: chips.asMap().entries.map((e) {
          final idx = e.key;
          final chip = e.value;
          final isActive = idx == selected;
          return Padding(
            padding: EdgeInsets.only(left: idx == 0 ? 0 : 8),
            child: GestureDetector(
              onTap: () => onSelect(idx),
              child: Container(
                padding: const EdgeInsets.symmetric(
                    horizontal: 14, vertical: 7),
                decoration: BoxDecoration(
                  color: isActive
                      ? (chip.activeBg ?? scheme.onSurface)
                      : (chip.inactiveBg ?? scheme.surface),
                  borderRadius: BorderRadius.circular(99),
                  border: Border.all(
                    color: isActive
                        ? (chip.activeBg ?? scheme.onSurface)
                        : (chip.inactiveBorder ?? scheme.outline),
                  ),
                ),
                child: Text(
                  chip.label,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: isActive
                        ? (chip.activeColor ?? scheme.onPrimary)
                        : (chip.inactiveColor ??
                            scheme.onSurface.withAlpha(160)),
                  ),
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}

class FilterChipData {
  const FilterChipData({
    required this.label,
    this.activeColor,
    this.activeBg,
    this.inactiveColor,
    this.inactiveBg,
    this.inactiveBorder,
  });

  final String label;
  final Color? activeColor;
  final Color? activeBg;
  final Color? inactiveColor;
  final Color? inactiveBg;
  final Color? inactiveBorder;
}

// ── SectionEyebrow ────────────────────────────────────────────────────────────

class SectionEyebrow extends StatelessWidget {
  const SectionEyebrow({super.key, required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Text(
      label.toUpperCase(),
      style: GoogleFonts.instrumentSans(
        fontSize: 11,
        fontWeight: FontWeight.w600,
        color: Theme.of(context).colorScheme.onSurface.withAlpha(120),
        letterSpacing: 0.88,
      ),
    );
  }
}

// ── CardSection ───────────────────────────────────────────────────────────────

class CardSection extends StatelessWidget {
  const CardSection({
    super.key,
    required this.child,
    this.padding,
  });

  final Widget child;
  final EdgeInsets? padding;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      width: double.infinity,
      padding: padding ?? const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: scheme.outline),
      ),
      child: child,
    );
  }
}

// ── StickyCtaBar ──────────────────────────────────────────────────────────────

class StickyCtaBar extends StatelessWidget {
  const StickyCtaBar({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final bg = Theme.of(context).scaffoldBackgroundColor;
    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            bg.withValues(alpha: 0),
            bg.withValues(alpha: 0.85),
            bg,
          ],
          stops: const [0.0, 0.3, 0.6],
        ),
      ),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      child: SafeArea(top: false, child: child),
    );
  }
}

// ── MethodGrid ────────────────────────────────────────────────────────────────

class MethodGrid extends StatelessWidget {
  const MethodGrid({
    super.key,
    required this.methods,
    required this.selected,
    required this.onSelect,
  });

  final List<String> methods;
  final int selected;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 4,
        mainAxisSpacing: 8,
        crossAxisSpacing: 8,
        childAspectRatio: 2.2,
      ),
      itemCount: methods.length,
      itemBuilder: (_, i) {
        final isSelected = i == selected;
        return GestureDetector(
          onTap: () => onSelect(i),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            decoration: BoxDecoration(
              color: isSelected ? scheme.onSurface : scheme.surface,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: isSelected ? scheme.onSurface : scheme.outline,
              ),
            ),
            alignment: Alignment.center,
            child: Text(
              methods[i],
              textAlign: TextAlign.center,
              style: GoogleFonts.instrumentSans(
                fontSize: 11.5,
                fontWeight: FontWeight.w600,
                color: isSelected
                    ? scheme.surface
                    : scheme.onSurface.withAlpha(160),
              ),
            ),
          ),
        );
      },
    );
  }
}

// ── BillCheckRow ──────────────────────────────────────────────────────────────

class BillCheckRow extends StatelessWidget {
  const BillCheckRow({
    super.key,
    required this.title,
    required this.sub,
    required this.amount,
    required this.checked,
    required this.onToggle,
  });

  final String title;
  final String sub;
  final String amount;
  final bool checked;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return GestureDetector(
      onTap: onToggle,
      behavior: HitTestBehavior.opaque,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 10),
        child: Row(
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              width: 19,
              height: 19,
              decoration: BoxDecoration(
                color: checked ? scheme.onSurface : Colors.transparent,
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: checked ? scheme.onSurface : scheme.outline,
                  width: 1.5,
                ),
              ),
              child: checked
                  ? Icon(Icons.check, color: scheme.surface, size: 13)
                  : null,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w500,
                      color: scheme.onSurface,
                    ),
                  ),
                  Text(
                    sub,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11.5,
                      color: scheme.onSurface.withAlpha(120),
                    ),
                  ),
                ],
              ),
            ),
            Text(
              amount,
              style: GoogleFonts.instrumentSans(
                fontSize: 13.5,
                fontWeight: FontWeight.w600,
                color: scheme.onSurface,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── SearchInput ───────────────────────────────────────────────────────────────

class SearchInput extends StatelessWidget {
  const SearchInput({
    super.key,
    this.controller,
    this.hintText = 'Search...',
    this.onChanged,
    this.onScan,
    this.autofocus = false,
  });

  final TextEditingController? controller;
  final String hintText;
  final ValueChanged<String>? onChanged;
  final VoidCallback? onScan;
  final bool autofocus;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: controller,
            autofocus: autofocus,
            onChanged: onChanged,
            textInputAction: TextInputAction.search,
            style: GoogleFonts.instrumentSans(
                fontSize: 14, color: scheme.onSurface),
            decoration: InputDecoration(
              hintText: hintText,
              prefixIcon: Icon(Icons.search,
                  size: 20, color: scheme.onSurface.withAlpha(120)),
            ),
          ),
        ),
        if (onScan != null) ...[
          const SizedBox(width: 8),
          GestureDetector(
            onTap: onScan,
            child: Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: scheme.onSurface,
                borderRadius: BorderRadius.circular(11),
              ),
              alignment: Alignment.center,
              child: Icon(Icons.qr_code_scanner,
                  color: scheme.surface, size: 20),
            ),
          ),
        ],
      ],
    );
  }
}

// ── InitialsAvatar ────────────────────────────────────────────────────────────

class InitialsAvatar extends StatelessWidget {
  const InitialsAvatar({
    super.key,
    required this.name,
    this.radius = 20,
    this.fontSize,
  });

  final String name;
  final double radius;
  final double? fontSize;

  String _initials() {
    final parts = name
        .trim()
        .split(RegExp(r'\s+'))
        .where((p) => p.isNotEmpty)
        .toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return CircleAvatar(
      radius: radius,
      backgroundColor: scheme.outline.withAlpha(50),
      child: Text(
        _initials(),
        style: GoogleFonts.instrumentSans(
          fontSize: fontSize ?? radius * 0.65,
          fontWeight: FontWeight.w600,
          color: scheme.onSurface.withAlpha(160),
        ),
      ),
    );
  }
}

// ── ListCard ──────────────────────────────────────────────────────────────────

class ListCard extends StatelessWidget {
  const ListCard({
    super.key,
    required this.child,
    this.onTap,
    this.margin = const EdgeInsets.only(bottom: 8),
  });

  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsets margin;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: margin,
      child: Material(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(13),
        child: InkWell(
          borderRadius: BorderRadius.circular(13),
          onTap: onTap,
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(13),
              border: Border.all(color: scheme.outline),
            ),
            padding: const EdgeInsets.symmetric(
                horizontal: 14, vertical: 12),
            child: child,
          ),
        ),
      ),
    );
  }
}

// ── PrimaryCtaBar ─────────────────────────────────────────────────────────────

class PrimaryCtaBar extends StatelessWidget {
  const PrimaryCtaBar({
    super.key,
    required this.label,
    required this.onTap,
    this.isLoading = false,
    this.backgroundColor,
    this.shadowColor,
  });

  final String label;
  final VoidCallback onTap;
  final bool isLoading;
  final Color? backgroundColor;
  final Color? shadowColor;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final bg = backgroundColor ?? scheme.primary;
    final fg = backgroundColor != null ? Colors.white : scheme.onPrimary;
    final shadow = shadowColor ?? bg.withAlpha(0x40);

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      color: Colors.transparent,
      child: SafeArea(
        top: false,
        child: SizedBox(
          width: double.infinity,
          height: 52,
          child: DecoratedBox(
            decoration: BoxDecoration(
              boxShadow: [
                BoxShadow(
                  color: shadow,
                  blurRadius: 24,
                  offset: const Offset(0, 8),
                ),
              ],
              borderRadius: BorderRadius.circular(14),
            ),
            child: FilledButton(
              onPressed: isLoading ? null : onTap,
              style: FilledButton.styleFrom(
                backgroundColor: bg,
                foregroundColor: fg,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14)),
                padding: const EdgeInsets.symmetric(vertical: 15),
                textStyle: GoogleFonts.instrumentSans(
                    fontSize: 15, fontWeight: FontWeight.w700),
              ),
              child: isLoading
                  ? SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                          strokeWidth: 2.5, color: fg),
                    )
                  : Text(label),
            ),
          ),
        ),
      ),
    );
  }
}
