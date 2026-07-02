import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
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
    final auth = ref.watch(authControllerProvider);
    final isLoading = _submitting;
    final errorText = auth.hasError
        ? (auth.error is AppException
            ? (auth.error as AppException).message
            : 'Login failed. Please try again.')
        : null;

    return Scaffold(
      body: Column(
        children: [
          // Dark hero section — shrinks naturally when keyboard appears
          Expanded(
            child: ClipRect(child: _HeroSection()),
          ),
          // White form panel — sits at the bottom
          _LoginPanel(
            formKey: _formKey,
            usernameCtrl: _usernameCtrl,
            passwordCtrl: _passwordCtrl,
            obscure: _obscure,
            isLoading: isLoading,
            errorText: errorText,
            onToggleObscure: () => setState(() => _obscure = !_obscure),
            onSubmit: _submit,
          ),
        ],
      ),
    );
  }
}

// ── Hero section ─────────────────────────────────────────────────────────────

class _HeroSection extends StatelessWidget {
  const _HeroSection();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0C0E1F), Color(0xFF1A1346), Color(0xFF2D1B69)],
        ),
      ),
      child: SafeArea(
        bottom: false,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            // ── Shop name ──────────────────────────────
            const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.settings, color: Color(0xFFF59E0B), size: 28),
                SizedBox(width: 12),
                Text(
                  'SUJAN MOTORS',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.w900,
                    letterSpacing: 3.5,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            const Text(
              'AUTO PARTS & ACCESSORIES',
              style: TextStyle(
                color: Color(0xFFF59E0B),
                fontSize: 11,
                letterSpacing: 3,
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 28),

            // ── Truck illustration ─────────────────────
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: SizedBox(
                height: 155,
                child: CustomPaint(
                  painter: _TruckPainter(),
                  size: Size.infinite,
                ),
              ),
            ),
            const SizedBox(height: 10),

            // ── Road line ──────────────────────────────
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Container(
                height: 2,
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    colors: [
                      Color(0x00FFFFFF),
                      Colors.white24,
                      Colors.white38,
                      Colors.white24,
                      Color(0x00FFFFFF),
                    ],
                  ),
                ),
              ),
            ),
            const SizedBox(height: 4),
          ],
        ),
      ),
    );
  }
}

// ── Login panel ──────────────────────────────────────────────────────────────

class _LoginPanel extends StatelessWidget {
  const _LoginPanel({
    required this.formKey,
    required this.usernameCtrl,
    required this.passwordCtrl,
    required this.obscure,
    required this.isLoading,
    required this.errorText,
    required this.onToggleObscure,
    required this.onSubmit,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController usernameCtrl;
  final TextEditingController passwordCtrl;
  final bool obscure;
  final bool isLoading;
  final String? errorText;
  final VoidCallback onToggleObscure;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(32)),
        boxShadow: [
          BoxShadow(
            color: Color(0x33000000),
            blurRadius: 24,
            offset: Offset(0, -6),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(28, 24, 28, 20),
          child: Form(
            key: formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Drag handle
                Center(
                  child: Container(
                    width: 36,
                    height: 4,
                    decoration: BoxDecoration(
                      color: Colors.grey.shade300,
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                ),
                const SizedBox(height: 18),

                const Text(
                  'Staff Sign In',
                  style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: Color(0xFF1A1346),
                  ),
                ),
                const SizedBox(height: 4),
                const Text(
                  'Enter your credentials to continue',
                  style: TextStyle(color: Colors.grey, fontSize: 13),
                ),
                const SizedBox(height: 20),

                TextFormField(
                  controller: usernameCtrl,
                  textInputAction: TextInputAction.next,
                  autocorrect: false,
                  enabled: !isLoading,
                  decoration: const InputDecoration(
                    labelText: 'Username',
                    prefixIcon: Icon(Icons.person_outline),
                  ),
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? 'Username is required'
                      : null,
                ),
                const SizedBox(height: 14),

                TextFormField(
                  controller: passwordCtrl,
                  obscureText: obscure,
                  enabled: !isLoading,
                  onFieldSubmitted: (_) => onSubmit(),
                  decoration: InputDecoration(
                    labelText: 'Password',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(obscure
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined),
                      onPressed: onToggleObscure,
                    ),
                  ),
                  validator: (v) =>
                      (v == null || v.isEmpty) ? 'Password is required' : null,
                ),

                if (errorText != null) ...[
                  const SizedBox(height: 12),
                  Text(
                    errorText!,
                    style: const TextStyle(color: Colors.red, fontSize: 13),
                    textAlign: TextAlign.center,
                  ),
                ],
                const SizedBox(height: 20),

                SizedBox(
                  height: 52,
                  child: FilledButton(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF4F46E5),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                      ),
                    ),
                    onPressed: isLoading ? null : onSubmit,
                    child: isLoading
                        ? const SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.5,
                              color: Colors.white,
                            ),
                          )
                        : const Text(
                            'Sign In',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Truck painter ─────────────────────────────────────────────────────────────
//
// Draws a stylised Tata cab-over truck silhouette in a 300×155 logical space.
// Right-facing profile: cargo on the left, cab (flat front) on the right.

class _TruckPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final s = size.width / 300; // horizontal scale
    final v = size.height / 155; // vertical scale

    // Helper to build a filled Paint.
    Paint f(Color c) => Paint()
      ..color = c
      ..style = PaintingStyle.fill;

    // Helper to build a stroked Paint.
    Paint st(Color c, double w) => Paint()
      ..color = c
      ..style = PaintingStyle.stroke
      ..strokeWidth = w
      ..strokeCap = StrokeCap.round;

    final indigo = f(const Color(0xFF6366F1)); // body colour (brand)
    final indigoDk = f(const Color(0xFF3730A3)); // darker body detail
    final glass = f(const Color(0xFF1E1B4B)); // windshield
    final amber = f(const Color(0xFFF59E0B)); // headlight / accent
    final amberSoft = f(const Color(0xFFFBBF24)); // indicator
    final exhaust = f(const Color(0xFF2D2A6E)); // exhaust pipe

    // ── Cargo box ─────────────────────────────────────
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(5 * s, 35 * v, 170 * s, 84 * v),
        Radius.circular(3 * s),
      ),
      indigo,
    );

    // Cargo horizontal panel lines
    final panelPaint = st(const Color(0xFF4338CA), 1.5 * v);
    for (var i = 1; i <= 3; i++) {
      final y = (35 + i * 21) * v;
      canvas.drawLine(Offset(8 * s, y), Offset(172 * s, y), panelPaint);
    }

    // Rear door seam (vertical)
    canvas.drawLine(
      Offset(158 * s, 38 * v),
      Offset(158 * s, 116 * v),
      st(const Color(0xFF4338CA), 2 * s),
    );

    // ── Cab body (flat-front cab-over design) ─────────
    final cabPath = Path()
      ..moveTo(178 * s, 119 * v)
      ..lineTo(178 * s, 55 * v)
      ..lineTo(186 * s, 27 * v) // cab roof rises above cargo
      ..lineTo(282 * s, 27 * v)
      ..lineTo(292 * s, 46 * v) // front corner slope
      ..lineTo(295 * s, 119 * v)
      ..close();
    canvas.drawPath(cabPath, indigo);

    // Windshield (large, typical of cab-over trucks)
    final windPath = Path()
      ..moveTo(184 * s, 57 * v)
      ..lineTo(193 * s, 37 * v)
      ..lineTo(278 * s, 37 * v)
      ..lineTo(288 * s, 55 * v)
      ..lineTo(288 * s, 97 * v)
      ..lineTo(184 * s, 97 * v)
      ..close();
    canvas.drawPath(windPath, glass);

    // Centre windshield post / A-pillar
    canvas.drawRect(
      Rect.fromLTWH(234 * s, 37 * v, 5 * s, 60 * v),
      indigoDk,
    );

    // Side window strip (below windshield, above door sill)
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(184 * s, 99 * v, 103 * s, 14 * v),
        Radius.circular(2 * s),
      ),
      f(const Color(0xFF2E2668)),
    );

    // Door mirror arm
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(176 * s, 65 * v, 5 * s, 14 * v),
        Radius.circular(2 * s),
      ),
      indigoDk,
    );

    // Front grille bars
    for (var i = 0; i < 4; i++) {
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromLTWH((282 + i * 3.5) * s, 99 * v, 2 * s, 18 * v),
          Radius.circular(s),
        ),
        indigoDk,
      );
    }

    // Headlight (amber rectangle with rounded corners)
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(281 * s, 59 * v, 13 * s, 18 * v),
        Radius.circular(3 * s),
      ),
      amber,
    );

    // Turn indicator (softer amber, below headlight)
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(281 * s, 81 * v, 13 * s, 8 * v),
        Radius.circular(2 * s),
      ),
      amberSoft,
    );

    // Front bumper strip
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(280 * s, 114 * v, 15 * s, 6 * v),
        Radius.circular(2 * s),
      ),
      indigoDk,
    );

    // ── Exhaust pipe (rises above cab roof) ───────────
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(190 * s, 8 * v, 10 * s, 22 * v),
        Radius.circular(5 * s),
      ),
      exhaust,
    );
    // Smoke puffs
    canvas.drawCircle(Offset(195 * s, 7 * v), 4.5 * s,
        f(Colors.white.withValues(alpha: 0.14)));
    canvas.drawCircle(Offset(202 * s, 3 * v), 3 * s,
        f(Colors.white.withValues(alpha: 0.09)));

    // ── Chassis / frame rail ──────────────────────────
    canvas.drawRect(
      Rect.fromLTWH(5 * s, 119 * v, 290 * s, 7 * v),
      indigoDk,
    );

    // ── Wheels ────────────────────────────────────────
    void wheel(double cx, double cy) {
      final c = Offset(cx * s, cy * v);
      // Rim
      canvas.drawCircle(c, 20 * s, f(const Color(0xFFE2E8F0)));
      // Tyre
      canvas.drawCircle(c, 13 * s, f(const Color(0xFF334155)));
      // Hub cap
      canvas.drawCircle(c, 5.5 * s, f(const Color(0xFFCBD5E1)));
      // Hub ring
      canvas.drawCircle(c, 8 * s, st(const Color(0xFFCBD5E1), 1.5 * s));
    }

    wheel(63, 132); // rear axle 1
    wheel(98, 132); // rear axle 2 (dual rear — Tata style)
    wheel(252, 132); // front axle
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
