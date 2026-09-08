import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import 'sales_repository.dart';

/// Modal bottom sheet asking a manager to approve a sale that would otherwise
/// be rejected as below cost or above MRP (`PRICE_OVERRIDE_REQUIRED`).
///
/// On a successful credential check it calls [retryWithApprovalToken] with
/// the returned token, which re-submits the exact same sale — the cashier
/// never re-enters anything. On failure it shows the backend's message
/// inline and stays open so the manager can retry.
Future<void> showPriceOverrideDialog(
  BuildContext context, {
  required String message,
  required Future<void> Function(String token) retryWithApprovalToken,
}) {
  return showModalBottomSheet<void>(
    context: context,
    useSafeArea: true,
    isScrollControlled: true,
    isDismissible: false,
    enableDrag: false,
    builder: (_) => _PriceOverrideSheet(
      message: message,
      retryWithApprovalToken: retryWithApprovalToken,
    ),
  );
}

class _PriceOverrideSheet extends ConsumerStatefulWidget {
  const _PriceOverrideSheet({
    required this.message,
    required this.retryWithApprovalToken,
  });

  final String message;
  final Future<void> Function(String token) retryWithApprovalToken;

  @override
  ConsumerState<_PriceOverrideSheet> createState() =>
      _PriceOverrideSheetState();
}

class _PriceOverrideSheetState extends ConsumerState<_PriceOverrideSheet> {
  final _usernameCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _obscure = true;
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    final username = _usernameCtrl.text.trim();
    final password = _passwordCtrl.text;
    if (username.isEmpty || password.isEmpty) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      final approval = await ref
          .read(salesRepositoryProvider)
          .requestPriceOverride(username: username, password: password);
      if (!mounted) return;
      // Close the sheet before retrying: the retry pops the charge screen
      // itself on success, and that pop must land on this screen, not on
      // this still-open sheet sitting on top of it.
      final retry = widget.retryWithApprovalToken;
      Navigator.of(context).pop();
      await retry(approval.token);
    } on AppException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final colors = context.colors;
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
          16,
          14,
          16,
          16 + MediaQuery.of(context).viewInsets.bottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(s.priceOverrideTitle,
                style: GoogleFonts.instrumentSans(
                    fontSize: 16, fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Text(widget.message,
                style: GoogleFonts.instrumentSans(
                    fontSize: 13, color: colors.ink)),
            const SizedBox(height: 4),
            Text(s.priceOverrideHint,
                style:
                    GoogleFonts.instrumentSans(fontSize: 12.5, color: colors.muted)),
            const SizedBox(height: 16),
            TextField(
              controller: _usernameCtrl,
              enabled: !_submitting,
              textInputAction: TextInputAction.next,
              decoration: InputDecoration(labelText: s.managerUsername),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: _passwordCtrl,
              enabled: !_submitting,
              obscureText: _obscure,
              textInputAction: TextInputAction.done,
              onSubmitted: (_) => _submit(),
              decoration: InputDecoration(
                labelText: s.managerPassword,
                suffixIcon: IconButton(
                  icon: Icon(
                      _obscure ? Icons.visibility_off : Icons.visibility,
                      size: 20),
                  onPressed: () => setState(() => _obscure = !_obscure),
                ),
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: colors.redBg,
                  border: Border.all(color: colors.redBorder),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(_error!,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 12.5, color: colors.red)),
              ),
            ],
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: _submitting
                        ? null
                        : () => Navigator.of(context).pop(),
                    child: Text(s.cancel),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  flex: 2,
                  child: FilledButton(
                    onPressed: _submitting ? null : _submit,
                    child: _submitting
                        ? SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                                strokeWidth: 2.5, color: colors.onInk),
                          )
                        : Text(s.approveAndContinue),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
