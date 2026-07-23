import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../core/i18n/strings.dart';
import '../../shared/format.dart';
import '../../shared/models/till_session.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'till_session_repository.dart';

class TillSessionScreen extends ConsumerStatefulWidget {
  const TillSessionScreen({super.key});

  @override
  ConsumerState<TillSessionScreen> createState() => _TillSessionScreenState();
}

class _TillSessionScreenState extends ConsumerState<TillSessionScreen> {
  // The `current` session query returns null once a session is CLOSED, so the
  // just-closed session (with its frozen reconciliation figures) is held here
  // rather than re-derived from the provider.
  TillSession? _justClosed;

  // Remembered across the "Open New Till" transition so the next Open Till
  // form can suggest this session's counted amount as the opening float,
  // without a round trip — we already have it right here.
  TillSession? _lastClosedForSuggestion;

  Future<void> _openCashDropSheet(TillSession session) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _CashDropSheet(sessionId: session.id),
    );
    if (saved == true) ref.invalidate(currentTillSessionProvider);
  }

  Future<void> _openCloseTillSheet(TillSession session) async {
    final closed = await showModalBottomSheet<TillSession>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _CloseTillSheet(session: session),
    );
    if (closed != null && mounted) {
      setState(() => _justClosed = closed);
      ref.invalidate(currentTillSessionProvider);
    }
  }

  void _startNewSession() {
    setState(() {
      _lastClosedForSuggestion = _justClosed;
      _justClosed = null;
    });
    ref.invalidate(currentTillSessionProvider);
  }

  @override
  Widget build(BuildContext context) {
    if (_justClosed != null) {
      return AppScaffold(
        title: S.of(context).tillSession,
        body: _ClosedSummaryBody(
          session: _justClosed!,
          onDone: _startNewSession,
        ),
      );
    }

    final currentAsync = ref.watch(currentTillSessionProvider);

    return AppScaffold(
      title: S.of(context).tillSession,
      floatingActionButton: currentAsync.asData?.value != null
          ? FloatingActionButton.extended(
              onPressed: () => _openCashDropSheet(currentAsync.asData!.value!),
              backgroundColor: context.colors.ink,
              foregroundColor: context.colors.onInk,
              icon: const Icon(Icons.payments_outlined),
              label: Text(S.of(context).cashDrop),
            )
          : null,
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(currentTillSessionProvider),
        child: currentAsync.when(
          loading: () => const LoadingView(),
          error: (e, _) => ListView(children: [
            const SizedBox(height: 120),
            ErrorView(
              message: e is AppException
                  ? e.message
                  : S.of(context).failedToLoadTillSession,
              onRetry: () => ref.invalidate(currentTillSessionProvider),
            ),
          ]),
          data: (session) => session == null
              ? _OpenTillFormCard(
                  onOpened: () => ref.invalidate(currentTillSessionProvider),
                  previousClosedSession: _lastClosedForSuggestion,
                )
              : _OpenSessionBody(
                  session: session,
                  onCloseTill: () => _openCloseTillSheet(session),
                ),
        ),
      ),
    );
  }
}

// ── No open session: "Open Till" form ───────────────────────────────────────

class _OpenTillFormCard extends ConsumerStatefulWidget {
  const _OpenTillFormCard({required this.onOpened, this.previousClosedSession});

  final VoidCallback onOpened;

  /// The session just closed in this same screen visit, if any — used only
  /// to default the Terminal field (reopening the same counter is the common
  /// case). The actual opening-float suggestion always comes from a fresh,
  /// terminal-scoped lookup (see [_OpenTillFormCardState._loadSuggestions]),
  /// not from this session directly — the cash physically stays in the
  /// drawer regardless of who counts it next, so it's the drawer's own
  /// history that matters, not this cashier's.
  final TillSession? previousClosedSession;

  @override
  ConsumerState<_OpenTillFormCard> createState() => _OpenTillFormCardState();
}

class _OpenTillFormCardState extends ConsumerState<_OpenTillFormCard> {
  late final _terminalCtrl = TextEditingController(
      text: widget.previousClosedSession?.terminalLabel ?? 'Mobile POS');
  final _terminalFocus = FocusNode();
  final _floatCtrl = TextEditingController();
  final _shiftCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _saving = false;
  String? _suggestedFloatFromCashier;
  String? _suggestedShiftHours;
  List<String> _terminalLabelOptions = const [];

  @override
  void initState() {
    super.initState();
    // The terminal field already has a default value (either the just-closed
    // session's terminal, or 'Mobile POS'), so we can look up its suggestion
    // right away rather than waiting for the cashier to touch the field.
    _loadSuggestions(_terminalCtrl.text.trim());
    _terminalFocus.addListener(() {
      if (!_terminalFocus.hasFocus) _loadSuggestions(_terminalCtrl.text.trim());
    });
    _loadTerminalLabelOptions();
  }

  Future<void> _loadTerminalLabelOptions() async {
    try {
      final labels =
          await ref.read(tillSessionRepositoryProvider).getTerminalLabels();
      if (mounted) setState(() => _terminalLabelOptions = labels);
    } on AppException {
      // Best-effort suggestion list only — an empty list just means plain free typing.
    }
  }

  /// Opening float is scoped to [terminalLabel]'s own last closed session
  /// (whoever ran it) — never carried over from a different cashier's
  /// history. Shift label is always resolved from the current cashier.
  Future<void> _loadSuggestions(String terminalLabel) async {
    try {
      final result = await ref
          .read(tillSessionRepositoryProvider)
          .getSuggestedOpeningFloat(terminalLabel: terminalLabel);
      if (!mounted) return;
      setState(() {
        if (result.suggestedOpeningFloat != null) {
          _floatCtrl.text = result.suggestedOpeningFloat!.toStringAsFixed(2);
          _suggestedFloatFromCashier = result.suggestedOpeningFloatFromCashier;
        } else {
          _suggestedFloatFromCashier = null;
        }
        if (result.suggestedShiftLabel != null) {
          _shiftCtrl.text = result.suggestedShiftLabel!;
          _suggestedShiftHours = result.suggestedShiftHours;
        }
      });
    } on AppException {
      // Best-effort UI hints only — silently skip if it fails, fields stay blank.
    }
  }

  @override
  void dispose() {
    _terminalCtrl.dispose();
    _terminalFocus.dispose();
    _floatCtrl.dispose();
    _shiftCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _open() async {
    final s = S.of(context);
    final terminal = _terminalCtrl.text.trim();
    final openingFloat = double.tryParse(_floatCtrl.text.trim()) ?? -1;
    String? problem;
    if (terminal.isEmpty) problem = s.enterTerminalLabel;
    if (openingFloat < 0) problem = s.enterValidOpeningFloat;
    if (problem != null) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(problem)));
      return;
    }

    setState(() => _saving = true);
    try {
      await ref.read(tillSessionRepositoryProvider).open(
            terminalLabel: terminal,
            openingFloat: openingFloat,
            shiftLabel: _shiftCtrl.text.trim().isEmpty
                ? null
                : _shiftCtrl.text.trim(),
            notes: _notesCtrl.text.trim(),
          );
      if (!mounted) return;
      widget.onOpened();
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(e.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        CardSection(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Icon(Icons.lock_clock_outlined,
                      size: 20, color: context.colors.ink),
                  const SizedBox(width: 8),
                  Text(
                    S.of(context).openATillSession,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                S.of(context).openTillSubtitle,
                style: GoogleFonts.instrumentSans(
                  fontSize: 12,
                  color: context.colors.secondary,
                ),
              ),
              const SizedBox(height: 16),
              RawAutocomplete<String>(
                textEditingController: _terminalCtrl,
                focusNode: _terminalFocus,
                optionsBuilder: (value) {
                  final query = value.text.trim().toLowerCase();
                  if (query.isEmpty) return _terminalLabelOptions;
                  return _terminalLabelOptions
                      .where((label) => label.toLowerCase().contains(query));
                },
                fieldViewBuilder:
                    (context, textController, focusNode, onFieldSubmitted) {
                  return TextField(
                    controller: textController,
                    focusNode: focusNode,
                    decoration: InputDecoration(
                        labelText: S.of(context).terminalLabel),
                  );
                },
                optionsViewBuilder: (context, onSelected, options) {
                  final scheme = Theme.of(context).colorScheme;
                  return Align(
                    alignment: Alignment.topLeft,
                    child: Material(
                      elevation: 4,
                      borderRadius: BorderRadius.circular(10),
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(
                            maxHeight: 200, maxWidth: 320),
                        child: ListView.builder(
                          padding: EdgeInsets.zero,
                          shrinkWrap: true,
                          itemCount: options.length,
                          itemBuilder: (context, i) {
                            final option = options.elementAt(i);
                            return InkWell(
                              onTap: () {
                                onSelected(option);
                                _loadSuggestions(option);
                              },
                              child: Padding(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 12, vertical: 10),
                                child: Text(
                                  option,
                                  style: GoogleFonts.instrumentSans(
                                      fontSize: 13, color: scheme.onSurface),
                                ),
                              ),
                            );
                          },
                        ),
                      ),
                    ),
                  );
                },
              ),
              const SizedBox(height: 10),
              TextField(
                controller: _floatCtrl,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                style: GoogleFonts.instrumentSans(
                    fontSize: 19, fontWeight: FontWeight.w700),
                decoration: InputDecoration(
                  labelText: S.of(context).openingFloat,
                  prefixText: kCurrencyPrefix,
                ),
                onChanged: (_) {
                  if (_suggestedFloatFromCashier != null) {
                    setState(() => _suggestedFloatFromCashier = null);
                  }
                },
              ),
              if (_suggestedFloatFromCashier != null)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    S
                        .of(context)
                        .suggestedFloatHint(_suggestedFloatFromCashier!),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11,
                      color: context.colors.secondary,
                    ),
                  ),
                ),
              const SizedBox(height: 10),
              TextField(
                controller: _shiftCtrl,
                decoration: InputDecoration(
                  labelText: S.of(context).shiftLabelOptional,
                ),
                onChanged: (_) {
                  if (_suggestedShiftHours != null) {
                    setState(() => _suggestedShiftHours = null);
                  }
                },
              ),
              if (_suggestedShiftHours != null)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    S.of(context).suggestedShiftHint(_suggestedShiftHours!),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11,
                      color: context.colors.secondary,
                    ),
                  ),
                ),
              const SizedBox(height: 10),
              TextField(
                controller: _notesCtrl,
                decoration: InputDecoration(
                  labelText: S.of(context).notesOptional,
                ),
              ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _saving ? null : _open,
                style: FilledButton.styleFrom(
                  backgroundColor: context.colors.ink,
                  padding: const EdgeInsets.symmetric(vertical: 15),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14)),
                ),
                child: _saving
                    ? SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: context.colors.onInk),
                      )
                    : Text(S.of(context).openTill),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ── Open session: summary + cash drops + actions ────────────────────────────

class _OpenSessionBody extends StatelessWidget {
  const _OpenSessionBody({required this.session, required this.onCloseTill});

  final TillSession session;
  final VoidCallback onCloseTill;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
      children: [
        _SessionSummaryCard(session: session),
        const SizedBox(height: 16),
        _SectionLabel(
            S.of(context).cashDropsCount(session.cashDrops.length)),
        const SizedBox(height: 8),
        if (session.cashDrops.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 8, bottom: 8),
            child: EmptyView(
              message: S.of(context).noCashDropsYet,
              icon: Icons.payments_outlined,
            ),
          )
        else
          ...session.cashDrops.reversed.map((d) => _CashDropTile(drop: d)),
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Row(
            children: [
              const Spacer(),
              Text(
                S.of(context).runningTotal(
                    formatCurrency(session.cashDropsRunningTotal)),
                style: GoogleFonts.instrumentSans(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                  color: context.colors.secondary,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 20),
        OutlinedButton.icon(
          onPressed: onCloseTill,
          icon: Icon(Icons.lock_outline, size: 16, color: context.colors.red),
          label: Text(S.of(context).closeTill),
          style: OutlinedButton.styleFrom(
            foregroundColor: context.colors.red,
            side: BorderSide(color: context.colors.redBorder),
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            textStyle: GoogleFonts.instrumentSans(
                fontSize: 14, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    );
  }
}

class _SessionSummaryCard extends StatelessWidget {
  const _SessionSummaryCard({required this.session});

  final TillSession session;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(18),
            decoration: const BoxDecoration(gradient: AppGradients.brand),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        session.shiftLabel == null
                            ? session.terminalLabel
                            : '${session.terminalLabel} · ${session.shiftLabel}',
                        style: theme.textTheme.bodySmall
                            ?.copyWith(color: Colors.white70),
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(99),
                      ),
                      child: Text(
                        S.of(context).openBadge,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 10.5,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  S.of(context).openingFloat,
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: Colors.white70),
                ),
                Text(
                  formatCurrency(session.openingFloat),
                  style: theme.textTheme.headlineMedium?.copyWith(
                      color: Colors.white, fontWeight: FontWeight.bold),
                ),
                Text(
                  S.of(context).openedAtLine(
                      formatDayLong(session.openedAt),
                      formatTime(session.openedAt)),
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: Colors.white70),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: _InfoMetric(
                    label: S.of(context).cashier,
                    value: session.cashierName,
                  ),
                ),
                Container(
                    width: 1, height: 34, color: scheme.outlineVariant),
                Expanded(
                  child: _InfoMetric(
                    label: S.of(context).cashDrops,
                    value: formatCurrency(session.cashDropsRunningTotal),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoMetric extends StatelessWidget {
  const _InfoMetric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Text(label, style: theme.textTheme.bodySmall),
        const SizedBox(height: 4),
        FittedBox(
          child: Text(
            value,
            maxLines: 1,
            style: theme.textTheme.titleSmall?.copyWith(
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    );
  }
}

class _CashDropTile extends StatelessWidget {
  const _CashDropTile({required this.drop});

  final TillCashDrop drop;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      elevation: 0,
      margin: const EdgeInsets.only(bottom: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: scheme.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: context.colors.redBg,
              child: Icon(Icons.north_east,
                  size: 18, color: context.colors.red),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    drop.notes.isEmpty ? S.of(context).cashDrop : drop.notes,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 2),
                  Text(formatTime(drop.droppedAt),
                      style: theme.textTheme.bodySmall),
                ],
              ),
            ),
            Text(
              '−${formatCurrency(drop.amount)}',
              style: TextStyle(
                  fontWeight: FontWeight.w700, color: context.colors.red),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Text(text.toUpperCase(),
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
              letterSpacing: 0.6,
              fontWeight: FontWeight.w700,
              color: Theme.of(context).colorScheme.onSurfaceVariant)),
    );
  }
}

// ── Record cash drop (sheet) ─────────────────────────────────────────────────

class _CashDropSheet extends ConsumerStatefulWidget {
  const _CashDropSheet({required this.sessionId});

  final String sessionId;

  @override
  ConsumerState<_CashDropSheet> createState() => _CashDropSheetState();
}

class _CashDropSheetState extends ConsumerState<_CashDropSheet> {
  final _amountCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _amountCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final amount = double.tryParse(_amountCtrl.text.trim()) ?? 0;
    if (amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(S.of(context).enterAnAmount)));
      return;
    }

    setState(() => _saving = true);
    try {
      await ref.read(tillSessionRepositoryProvider).recordCashDrop(
            sessionId: widget.sessionId,
            amount: amount,
            notes: _notesCtrl.text.trim(),
          );
      if (mounted) Navigator.of(context).pop(true);
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(e.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding:
          EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              S.of(context).recordCashDrop,
              style: GoogleFonts.instrumentSans(
                  fontSize: 16, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 4),
            Text(
              S.of(context).cashDropSubtitle,
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                color: context.colors.secondary,
              ),
            ),
            const SizedBox(height: 14),
            TextField(
              controller: _amountCtrl,
              autofocus: true,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              style: GoogleFonts.instrumentSans(
                  fontSize: 19, fontWeight: FontWeight.w700),
              decoration: InputDecoration(
                labelText: S.of(context).amount,
                prefixText: kCurrencyPrefix,
              ),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _notesCtrl,
              decoration: InputDecoration(
                labelText: S.of(context).notesOptional,
              ),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: _saving ? null : _save,
              style: FilledButton.styleFrom(
                backgroundColor: context.colors.ink,
                padding: const EdgeInsets.symmetric(vertical: 15),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14)),
              ),
              child: _saving
                  ? SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: context.colors.onInk),
                    )
                  : Text(S.of(context).saveCashDrop),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Close till (sheet + confirmation) ────────────────────────────────────────

class _CloseTillSheet extends ConsumerStatefulWidget {
  const _CloseTillSheet({required this.session});

  final TillSession session;

  @override
  ConsumerState<_CloseTillSheet> createState() => _CloseTillSheetState();
}

class _CloseTillSheetState extends ConsumerState<_CloseTillSheet> {
  final _countedCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _countedCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _confirmAndClose() async {
    final counted = double.tryParse(_countedCtrl.text.trim()) ?? -1;
    if (counted < 0) {
      ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(S.of(context).enterCountedAmount)));
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(S.of(ctx).closeTillQuestion),
        content: Text(S.of(ctx).closeTillWarning),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(S.of(ctx).cancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: FilledButton.styleFrom(backgroundColor: context.colors.red),
            child: Text(S.of(ctx).closeTill),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _saving = true);
    try {
      final closed = await ref.read(tillSessionRepositoryProvider).close(
            sessionId: widget.session.id,
            countedAmount: counted,
            notes: _notesCtrl.text.trim(),
          );
      if (mounted) Navigator.of(context).pop(closed);
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() => _saving = false);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(e.message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding:
          EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              S.of(context).closeTillTitled(widget.session.terminalLabel),
              style: GoogleFonts.instrumentSans(
                  fontSize: 16, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: context.colors.amberBg,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: context.colors.amberBorder),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline,
                      size: 16, color: context.colors.amber),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      S.of(context).countDrawerWarning,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11.5, color: context.colors.amber),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            TextField(
              controller: _countedCtrl,
              autofocus: true,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              style: GoogleFonts.instrumentSans(
                  fontSize: 19, fontWeight: FontWeight.w700),
              decoration: InputDecoration(
                labelText: S.of(context).countedDrawerAmount,
                prefixText: kCurrencyPrefix,
              ),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _notesCtrl,
              decoration: InputDecoration(
                labelText: S.of(context).notesOptional,
              ),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: _saving ? null : _confirmAndClose,
              style: FilledButton.styleFrom(
                backgroundColor: context.colors.red,
                padding: const EdgeInsets.symmetric(vertical: 15),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14)),
              ),
              child: _saving
                  ? SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: context.colors.onInk),
                    )
                  : Text(S.of(context).reviewAndClose),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Closed: reconciliation summary + share PDF ──────────────────────────────

class _ClosedSummaryBody extends ConsumerStatefulWidget {
  const _ClosedSummaryBody({required this.session, required this.onDone});

  final TillSession session;
  final VoidCallback onDone;

  @override
  ConsumerState<_ClosedSummaryBody> createState() =>
      _ClosedSummaryBodyState();
}

class _ClosedSummaryBodyState extends ConsumerState<_ClosedSummaryBody> {
  bool _pdfLoading = false;

  Future<void> _sharePdf() async {
    setState(() => _pdfLoading = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      final Uint8List bytes = await ref
          .read(tillSessionRepositoryProvider)
          .downloadPdf(widget.session.id);
      final cacheDir = await getTemporaryDirectory();
      final timestamp = DateTime.now().millisecondsSinceEpoch;
      final path = '${cacheDir.path}/shift_report_$timestamp.pdf';
      await File(path).writeAsBytes(bytes);
      if (!mounted) return;
      await Share.shareXFiles(
        [
          XFile(path,
              mimeType: 'application/pdf', name: 'shift_report_$timestamp.pdf')
        ],
        subject: S.of(context).shiftReport,
      );
    } on AppException catch (e) {
      if (mounted) {
        messenger.clearSnackBars();
        messenger.showSnackBar(SnackBar(
          content: Text(e.message),
          backgroundColor: context.colors.red,
          behavior: SnackBarBehavior.floating,
        ));
      }
    } finally {
      if (mounted) setState(() => _pdfLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = widget.session;
    final overShort = session.overShortAmount;
    final Color reconColor;
    final String reconLabel;
    if (overShort == 0) {
      reconColor = context.colors.green;
      reconLabel = S.of(context).balanced;
    } else if (overShort < 0) {
      reconColor = context.colors.red;
      reconLabel = S.of(context).shortLabel;
    } else {
      reconColor = context.colors.amber;
      reconLabel = S.of(context).overLabel;
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        CardSection(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Icon(Icons.check_circle_outline,
                      size: 20, color: context.colors.green),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      S.of(context).tillClosedTitled(session.terminalLabel),
                      style: GoogleFonts.instrumentSans(
                          fontSize: 15, fontWeight: FontWeight.w700),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                session.closedAt == null
                    ? ''
                    : '${formatDayLong(session.closedAt!)}, ${formatTime(session.closedAt!)}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12, color: context.colors.secondary),
              ),
              const SizedBox(height: 16),
              _ReconRow(
                  label: S.of(context).openingFloat,
                  value: session.openingFloat),
              _ReconRow(
                  label: S.of(context).cashSales,
                  value: session.cashSalesTotal),
              _ReconRow(
                  label: S.of(context).cashRefunds,
                  value: -session.cashRefundsTotal),
              _ReconRow(
                  label: S.of(context).cashDrops,
                  value: -session.cashDropsTotal),
              const Divider(height: 20),
              _ReconRow(
                  label: S.of(context).expectedInDrawer,
                  value: session.expectedAmount,
                  bold: true),
              _ReconRow(
                  label: S.of(context).countedAmount,
                  value: session.closingCountedAmount ?? 0,
                  bold: true),
              const SizedBox(height: 12),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: reconColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: reconColor.withValues(alpha: 0.3)),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        reconLabel,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w600,
                          color: reconColor,
                        ),
                      ),
                    ),
                    Text(
                      formatCurrency(overShort.abs()),
                      style: GoogleFonts.instrumentSans(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: reconColor,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: _pdfLoading ? null : _sharePdf,
          icon: _pdfLoading
              ? SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: context.colors.onInk),
                )
              : const Icon(Icons.ios_share, size: 16),
          label: Text(S.of(context).shareShiftReportPdf),
          style: FilledButton.styleFrom(
            backgroundColor: context.colors.ink,
            foregroundColor: context.colors.onInk,
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
        ),
        const SizedBox(height: 10),
        OutlinedButton(
          onPressed: widget.onDone,
          style: OutlinedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
          child: Text(S.of(context).openNewTill),
        ),
      ],
    );
  }
}

class _ReconRow extends StatelessWidget {
  const _ReconRow({
    required this.label,
    required this.value,
    this.bold = false,
  });

  final String label;
  final double value;
  final bool bold;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: GoogleFonts.instrumentSans(
                fontSize: 13,
                fontWeight: bold ? FontWeight.w600 : FontWeight.w400,
                color: bold ? null : context.colors.secondary,
              ),
            ),
          ),
          Text(
            formatCurrency(value),
            style: GoogleFonts.instrumentSans(
              fontSize: 13,
              fontWeight: bold ? FontWeight.w700 : FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
