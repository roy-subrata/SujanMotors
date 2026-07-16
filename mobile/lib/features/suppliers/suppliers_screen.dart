import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../core/i18n/strings.dart';
import '../../shared/format.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import 'suppliers_repository.dart';

/// Supplier list — square initials avatar, payable amount right-aligned,
/// "We owe" filter chip. Tapping a row opens the pay/statement actions.
class SuppliersScreen extends ConsumerStatefulWidget {
  const SuppliersScreen({super.key});

  @override
  ConsumerState<SuppliersScreen> createState() => _SuppliersScreenState();
}

class _SuppliersScreenState extends ConsumerState<SuppliersScreen> {
  final _searchCtrl = TextEditingController();
  Timer? _debounce;
  String _query = '';
  int _filterIndex = 0;

  @override
  void dispose() {
    _debounce?.cancel();
    _searchCtrl.dispose();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 380), () {
      if (mounted) setState(() => _query = value.trim());
    });
  }

  void _openActions(Supplier supplier) {
    showModalBottomSheet<void>(
      context: context,
      useSafeArea: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 4),
              child: Row(
                children: [
                  _SupplierAvatar(name: supplier.name, size: 40),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          supplier.name,
                          style: GoogleFonts.instrumentSans(
                              fontSize: 15, fontWeight: FontWeight.w700),
                        ),
                        Text(
                          supplier.hasPayable
                              ? 'We owe ${formatCurrency(supplier.currentBalance)}'
                              : 'No outstanding payable',
                          style: GoogleFonts.instrumentSans(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: supplier.hasPayable
                                ? context.colors.amber
                                : context.colors.green,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 4),
            ListTile(
              leading: const Icon(Icons.payments_outlined),
              title: Text('Pay supplier',
                  style: GoogleFonts.instrumentSans(fontSize: 14)),
              onTap: () {
                Navigator.of(sheetContext).pop();
                context.push('/suppliers/${supplier.id}/pay');
              },
            ),
            ListTile(
              leading: const Icon(Icons.receipt_long_outlined),
              title: Text('Statement',
                  style: GoogleFonts.instrumentSans(fontSize: 14)),
              onTap: () {
                Navigator.of(sheetContext).pop();
                context.push('/suppliers/${supplier.id}/statement');
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: S.of(context).suppliers,
      showNotificationBell: true,
      body: Column(
        children: [
          // Search
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: 'Search supplier...',
              onChanged: _onSearchChanged,
            ),
          ),
          const SizedBox(height: 10),

          // Filter chips
          FilterChipRow(
            selected: _filterIndex,
            onSelect: (i) => setState(() => _filterIndex = i),
            chips: [
              FilterChipData(label: 'All'),
              FilterChipData(
                label: 'We owe',
                inactiveColor: context.colors.amber,
                inactiveBg: context.colors.amberBg,
                inactiveBorder: context.colors.amberBorder,
              ),
            ],
          ),
          const SizedBox(height: 8),

          Expanded(
            child: PagedListView<Supplier>(
              resetKey: '$_query|$_filterIndex',
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
              fetch: (page) async {
                final res = await ref
                    .read(suppliersRepositoryProvider)
                    .list(search: _query, page: page, pageSize: 20);
                final items = _filterIndex == 1
                    ? res.items.where((s) => s.hasPayable).toList()
                    : res.items;
                return PagedChunk<Supplier>(
                  items: items,
                  totalCount: items.length,
                  hasMore: res.hasMore,
                );
              },
              emptyBuilder: (context) => const EmptyView(
                message: 'No suppliers found.',
                icon: Icons.store_outlined,
              ),
              itemBuilder: (context, supplier) => _SupplierCard(
                supplier: supplier,
                onTap: () => _openActions(supplier),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Supplier card ─────────────────────────────────────────────────────────────

class _SupplierCard extends StatelessWidget {
  const _SupplierCard({required this.supplier, required this.onTap});

  final Supplier supplier;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final subtitle = [
      if ((supplier.phone ?? '').isNotEmpty) supplier.phone!,
      if ((supplier.contactPerson ?? '').isNotEmpty) supplier.contactPerson!,
    ].join(' · ');

    return ListCard(
      onTap: onTap,
      child: Row(
        children: [
          _SupplierAvatar(name: supplier.name, size: 40),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  supplier.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.instrumentSans(
                      fontSize: 13.5, fontWeight: FontWeight.w500),
                ),
                if (subtitle.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 11.5, color: context.colors.muted),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                formatCurrency(supplier.currentBalance),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: supplier.hasPayable
                      ? context.colors.amber
                      : context.colors.green,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                supplier.hasPayable ? 'we owe' : 'clear',
                style: GoogleFonts.instrumentSans(
                    fontSize: 10.5, color: context.colors.muted),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Square (radius 11) initials avatar — the supplier variant of the circular
/// customer avatar, per the design.
class _SupplierAvatar extends StatelessWidget {
  const _SupplierAvatar({required this.name, required this.size});

  final String name;
  final double size;

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
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: Theme.of(context).scaffoldBackgroundColor,
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: scheme.outline),
      ),
      alignment: Alignment.center,
      child: Text(
        _initials(),
        style: GoogleFonts.instrumentSans(
          fontSize: size * 0.3,
          fontWeight: FontWeight.w600,
          color: context.colors.secondary,
        ),
      ),
    );
  }
}
