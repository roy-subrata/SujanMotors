import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import 'auth_controller.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _obscure = true;
  bool _submitting = false;

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() => _submitting = true);
    await ref
        .read(authControllerProvider.notifier)
        .login(_usernameCtrl.text.trim(), _passwordCtrl.text);
    if (mounted) setState(() => _submitting = false);
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final auth = ref.watch(authControllerProvider);
    final errorText = auth.hasError
        ? (auth.error is AppException
            ? (auth.error as AppException).message
            : s.loginFailed)
        : null;

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 28),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 56),

                // Logo + store name
                Center(
                  child: Column(
                    children: [
                      Container(
                        width: 56,
                        height: 56,
                        decoration: BoxDecoration(
                          color: context.colors.ink,
                          borderRadius: BorderRadius.circular(14),
                        ),
                        alignment: Alignment.center,
                        child: Text(
                          'SM',
                          style: GoogleFonts.instrumentSans(
                            color: Colors.white,
                            fontSize: 22,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Text(
                        s.brandName,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 20,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        s.brandSubtitle,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 48),

                // Username field
                _FieldLabel(label: s.usernameOrPhone),
                const SizedBox(height: 6),
                TextFormField(
                  controller: _usernameCtrl,
                  textInputAction: TextInputAction.next,
                  autocorrect: false,
                  enabled: !_submitting,
                  style: GoogleFonts.instrumentSans(
                      fontSize: 14, color: context.colors.ink),
                  decoration: InputDecoration(
                    hintText: s.enterUsernameHint,
                  ),
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? s.usernameRequired
                      : null,
                ),
                const SizedBox(height: 16),

                // Password field
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    _FieldLabel(label: s.passwordLabel),
                    GestureDetector(
                      onTap: () {},
                      child: Text(
                        s.forgotPassword,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 12.5,
                          fontWeight: FontWeight.w600
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                TextFormField(
                  controller: _passwordCtrl,
                  obscureText: _obscure,
                  enabled: !_submitting,
                  onFieldSubmitted: (_) => _submit(),
                  style: GoogleFonts.instrumentSans(
                      fontSize: 14, color: context.colors.ink),
                  decoration: InputDecoration(
                    hintText: s.enterPasswordHint,
                    suffixIcon: TextButton(
                      onPressed: () =>
                          setState(() => _obscure = !_obscure),
                      style: TextButton.styleFrom(
                          foregroundColor: context.colors.secondary),
                      child: Text(
                        _obscure ? s.showLabel : s.hideLabel,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 12.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                  validator: (v) =>
                      (v == null || v.isEmpty) ? s.passwordRequired : null,
                ),

                if (errorText != null) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 10),
                    decoration: BoxDecoration(
                      color: context.colors.redBg,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: context.colors.redBorder),
                    ),
                    child: Text(
                      errorText,
                      style: GoogleFonts.instrumentSans(
                        color: context.colors.red,
                        fontSize: 13,
                      ),
                    ),
                  ),
                ],

                const SizedBox(height: 24),

                // Sign in button
                SizedBox(
                  height: 50,
                  child: FilledButton(
                    style: FilledButton.styleFrom(
                      backgroundColor: context.colors.ink,
                      foregroundColor: context.colors.onInk,
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      padding: const EdgeInsets.symmetric(vertical: 15),
                      textStyle: GoogleFonts.instrumentSans(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    onPressed: _submitting ? null : _submit,
                    child: _submitting
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              color: Colors.white,
                            ),
                          )
                        : Text(s.signIn),
                  ),
                ),

                const SizedBox(height: 12),

                // Use PIN button
                SizedBox(
                  height: 48,
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: context.colors.ink,
                      side: BorderSide(color: Theme.of(context).colorScheme.outline),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      padding: const EdgeInsets.symmetric(vertical: 13),
                      textStyle: GoogleFonts.instrumentSans(
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    onPressed: () {},
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.dialpad_outlined, size: 16),
                        const SizedBox(width: 8),
                        Text(s.usePinInstead),
                      ],
                    ),
                  ),
                ),

                const SizedBox(height: 40),

                // Footer
                Center(
                  child: Text(
                    s.storeFooter,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 12
                    ),
                  ),
                ),
                const SizedBox(height: 24),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _FieldLabel extends StatelessWidget {
  const _FieldLabel({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Text(
      label,
      style: GoogleFonts.instrumentSans(
        fontSize: 12.5,
        fontWeight: FontWeight.w600
      ),
    );
  }
}
