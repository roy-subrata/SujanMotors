import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

class AppColors {
  const AppColors._();
  static const bg            = Color(0xFFF4F5F7);
  static const surface       = Color(0xFFFFFFFF);
  static const surfaceSubtle = Color(0xFFFAFAFB);
  static const border        = Color(0xFFE6E8EC);
  static const hairline      = Color(0xFFEEF0F3);
  static const ink           = Color(0xFF0F172A);
  static const secondary     = Color(0xFF5B6472);
  static const muted         = Color(0xFF8A93A2);
  static const disabled      = Color(0xFFC3C9D2);
  static const green         = Color(0xFF0D8A53);
  static const greenBg       = Color(0xFFE9F7F0);
  static const red           = Color(0xFFD63841);
  static const redBg         = Color(0xFFFDEEEF);
  static const redBorder     = Color(0xFFF6D5D7);
  static const amber         = Color(0xFFB26A00);
  static const amberBg       = Color(0xFFFDF3E2);
  static const amberBorder   = Color(0xFFF3E2BD);
}

class AppGradients {
  const AppGradients._();

  /// Primary brand gradient — used by the login hero section.
  static const LinearGradient brand = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF0C0E1F), Color(0xFF1A1346), Color(0xFF2D1B69)],
  );
}

class AppTheme {
  const AppTheme._();

  static ThemeData light() {
    final base = GoogleFonts.instrumentSansTextTheme();
    return ThemeData(
      useMaterial3: true,
      scaffoldBackgroundColor: AppColors.bg,
      colorScheme: const ColorScheme.light(
        primary: AppColors.ink,
        onPrimary: Colors.white,
        secondary: AppColors.secondary,
        surface: AppColors.surface,
        onSurface: AppColors.ink,
        outline: AppColors.border,
        error: AppColors.red,
      ),
      textTheme: base,
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.surface,
        foregroundColor: AppColors.ink,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        shadowColor: Colors.transparent,
        centerTitle: false,
        iconTheme: const IconThemeData(color: AppColors.ink),
        actionsIconTheme: const IconThemeData(color: AppColors.ink),
        titleTextStyle: GoogleFonts.instrumentSans(
          color: AppColors.ink,
          fontSize: 17,
          fontWeight: FontWeight.w700,
        ),
        systemOverlayStyle: SystemUiOverlayStyle.dark.copyWith(
          statusBarColor: Colors.transparent,
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: AppColors.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(13),
          side: const BorderSide(color: AppColors.border),
        ),
        margin: EdgeInsets.zero,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: AppColors.ink, width: 1.5),
        ),
        hintStyle: GoogleFonts.instrumentSans(
            color: AppColors.muted, fontSize: 14),
        labelStyle: GoogleFonts.instrumentSans(
            color: AppColors.secondary, fontSize: 14),
      ),
      dividerTheme: const DividerThemeData(
          color: AppColors.hairline, space: 1),
      chipTheme: ChipThemeData(
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(99)),
        side: const BorderSide(color: AppColors.border),
        padding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        labelStyle: GoogleFonts.instrumentSans(
            fontSize: 12, fontWeight: FontWeight.w500),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.ink,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14)),
          padding:
              const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
          textStyle: GoogleFonts.instrumentSans(
              fontSize: 15, fontWeight: FontWeight.w700),
        ),
      ),
    );
  }

  static ThemeData dark() {
    final base = GoogleFonts.instrumentSansTextTheme(ThemeData.dark().textTheme);
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: const Color(0xFF0F1117),
      colorScheme: const ColorScheme.dark(
        primary: Color(0xFFF8FAFC),
        onPrimary: Color(0xFF0F172A),
        secondary: Color(0xFF94A3B8),
        surface: Color(0xFF1A1D27),
        onSurface: Color(0xFFF8FAFC),
        outline: Color(0xFF2A2D3A),
        error: Color(0xFFD63841),
        primaryContainer: Color(0xFF2A2D3A),
        onPrimaryContainer: Color(0xFFF8FAFC),
        surfaceContainerLowest: Color(0xFF1A1D27),
        outlineVariant: Color(0xFF2A2D3A),
      ),
      textTheme: base,
      appBarTheme: AppBarTheme(
        backgroundColor: const Color(0xFF1A1D27),
        foregroundColor: const Color(0xFFF8FAFC),
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        shadowColor: Colors.transparent,
        centerTitle: false,
        iconTheme: const IconThemeData(color: Color(0xFFF8FAFC)),
        actionsIconTheme: const IconThemeData(color: Color(0xFFF8FAFC)),
        titleTextStyle: GoogleFonts.instrumentSans(
          color: const Color(0xFFF8FAFC),
          fontSize: 17,
          fontWeight: FontWeight.w700,
        ),
        systemOverlayStyle: SystemUiOverlayStyle.light.copyWith(
          statusBarColor: Colors.transparent,
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: const Color(0xFF1A1D27),
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(13),
          side: const BorderSide(color: Color(0xFF2A2D3A)),
        ),
        margin: EdgeInsets.zero,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFF1E2130),
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: Color(0xFF2A2D3A)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: Color(0xFF2A2D3A)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide:
              const BorderSide(color: Color(0xFFF8FAFC), width: 1.5),
        ),
        hintStyle: GoogleFonts.instrumentSans(
            color: const Color(0xFF64748B), fontSize: 14),
        labelStyle: GoogleFonts.instrumentSans(
            color: const Color(0xFF94A3B8), fontSize: 14),
      ),
      dividerTheme:
          const DividerThemeData(color: Color(0xFF2A2D3A), space: 1),
      chipTheme: ChipThemeData(
        shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(99)),
        side: const BorderSide(color: Color(0xFF2A2D3A)),
        padding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        labelStyle: GoogleFonts.instrumentSans(
            fontSize: 12, fontWeight: FontWeight.w500),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: const Color(0xFFF8FAFC),
          foregroundColor: const Color(0xFF0F172A),
          shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14)),
          padding:
              const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
          textStyle: GoogleFonts.instrumentSans(
              fontSize: 15, fontWeight: FontWeight.w700),
        ),
      ),
    );
  }
}

/// Legacy widget: a gradient-filled [DecoratedBox] for the login hero.
/// Not used on any AppBar in the new design (AppBar is plain white).
class AppBarGradient extends StatelessWidget {
  const AppBarGradient({super.key});

  @override
  Widget build(BuildContext context) => const DecoratedBox(
        decoration: BoxDecoration(gradient: AppGradients.brand),
      );
}
