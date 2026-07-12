import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/widgets/app_scaffold.dart';

class SalesScreen extends StatelessWidget {
  const SalesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return AppScaffold(
      title: 'Sales',
      showBottomNav: true,
      showNotificationBell: true,
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.go('/quick-sale'),
        backgroundColor: AppColors.ink,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add),
      ),
      body: Column(
        children: [
          // ── Search ─────────────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: TextField(
              style: GoogleFonts.instrumentSans(fontSize: 14, color: AppColors.ink),
              decoration: InputDecoration(
                hintText: 'Search invoices...',
                hintStyle: GoogleFonts.instrumentSans(color: AppColors.muted, fontSize: 14),
                prefixIcon: const Icon(Icons.search, size: 20, color: AppColors.muted),
                contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(11),
                  borderSide: BorderSide(color: scheme.outline),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(11),
                  borderSide: BorderSide(color: scheme.outline),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(11),
                  borderSide: const BorderSide(color: Color(0xFF4F46E5), width: 1.5),
                ),
                filled: true,
                fillColor: scheme.surface,
              ),
            ),
          ),
          const SizedBox(height: 10),

          // ── Filter chips ───────────────────────────────────────────────────
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                _FilterChip(label: 'All', active: true),
                const SizedBox(width: 8),
                _FilterChip(label: 'Paid', color: AppColors.green, bg: AppColors.greenBg),
                const SizedBox(width: 8),
                _FilterChip(label: 'Due', color: AppColors.red, bg: AppColors.redBg, border: AppColors.redBorder),
                const SizedBox(width: 8),
                _FilterChip(label: 'Returns'),
              ],
            ),
          ),
          const SizedBox(height: 12),

          // ── Empty state ────────────────────────────────────────────────────
          Expanded(
            child: Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.receipt_long_outlined, size: 56, color: AppColors.disabled),
                  const SizedBox(height: 14),
                  Text(
                    'No sales yet',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Tap + to start a new quick sale',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      color: AppColors.muted,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    this.active = false,
    this.color,
    this.bg,
    this.border,
  });

  final String label;
  final bool active;
  final Color? color;
  final Color? bg;
  final Color? border;

  @override
  Widget build(BuildContext context) {
    final fgColor = active ? Colors.white : (color ?? AppColors.secondary);
    final bgColor = active ? AppColors.ink : (bg ?? Theme.of(context).colorScheme.surface);
    final borderColor = active ? AppColors.ink : (border ?? Theme.of(context).colorScheme.outline);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(99),
        border: Border.all(color: borderColor),
      ),
      child: Text(
        label,
        style: GoogleFonts.instrumentSans(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          color: fgColor,
        ),
      ),
    );
  }
}
