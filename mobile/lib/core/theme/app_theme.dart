import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

/// Semantic colour tokens, resolved per brightness.
///
/// Read these through `context.colors` rather than referencing a palette
/// constant directly, so every surface follows the active theme.
@immutable
class AppPalette extends ThemeExtension<AppPalette> {
  const AppPalette({
    required this.bg,
    required this.surface,
    required this.surfaceSubtle,
    required this.border,
    required this.hairline,
    required this.ink,
    required this.onInk,
    required this.secondary,
    required this.muted,
    required this.disabled,
    required this.green,
    required this.greenBg,
    required this.greenBorder,
    required this.red,
    required this.redBg,
    required this.redBorder,
    required this.amber,
    required this.amberBg,
    required this.amberBorder,
  });

  /// Page background, behind [surface].
  final Color bg;

  /// Cards, sheets, inputs.
  final Color surface;

  /// Slightly recessed fill inside a [surface] (table headers, read-only rows).
  final Color surfaceSubtle;

  /// Default border for cards, inputs, chips.
  final Color border;

  /// Divider / separator, lighter than [border].
  final Color hairline;

  /// Primary text, and the fill of high-emphasis buttons.
  final Color ink;

  /// Text/icons drawn on top of any high-emphasis fill — [ink] as well as the
  /// saturated status colours ([green]/[red]/[amber]). All of those invert
  /// lightness between the two themes, so they share one contrast colour.
  final Color onInk;

  /// Secondary text — labels, captions.
  final Color secondary;

  /// Tertiary text — hints, placeholders.
  final Color muted;

  /// Disabled text and icons.
  final Color disabled;

  final Color green;
  final Color greenBg;
  final Color greenBorder;
  final Color red;
  final Color redBg;
  final Color redBorder;
  final Color amber;
  final Color amberBg;
  final Color amberBorder;

  static const light = AppPalette(
    bg: Color(0xFFF4F5F7),
    surface: Color(0xFFFFFFFF),
    surfaceSubtle: Color(0xFFFAFAFB),
    border: Color(0xFFE6E8EC),
    hairline: Color(0xFFEEF0F3),
    ink: Color(0xFF0F172A),
    onInk: Color(0xFFFFFFFF),
    secondary: Color(0xFF5B6472),
    muted: Color(0xFF8A93A2),
    disabled: Color(0xFFC3C9D2),
    // Status foregrounds are a touch darker than the original brand values so
    // the small bold text in a status pill clears 4.5:1 against its own tint.
    green: Color(0xFF0C7E4C),
    greenBg: Color(0xFFE9F7F0),
    greenBorder: Color(0xFFCDEADD),
    red: Color(0xFFD12B35),
    redBg: Color(0xFFFDEEEF),
    redBorder: Color(0xFFF6D5D7),
    amber: Color(0xFFA05F00),
    amberBg: Color(0xFFFDF3E2),
    amberBorder: Color(0xFFF3E2BD),
  );

  /// Dark tokens. Status foregrounds are lightened well past their light-mode
  /// values so they clear 4.5:1 against the dark [surface]; the matching `*Bg`
  /// fills are dark tints of the same hue rather than the light pastels.
  static const dark = AppPalette(
    bg: Color(0xFF0F1117),
    surface: Color(0xFF1A1D27),
    surfaceSubtle: Color(0xFF20242F),
    border: Color(0xFF2A2D3A),
    hairline: Color(0xFF242734),
    ink: Color(0xFFF8FAFC),
    onInk: Color(0xFF0F172A),
    secondary: Color(0xFF94A3B8),
    muted: Color(0xFF64748B),
    disabled: Color(0xFF4A5365),
    green: Color(0xFF3DD68C),
    greenBg: Color(0xFF10291F),
    greenBorder: Color(0xFF1D4634),
    red: Color(0xFFFF6B72),
    redBg: Color(0xFF2B1418),
    redBorder: Color(0xFF4C2126),
    amber: Color(0xFFF0B429),
    amberBg: Color(0xFF2A2010),
    amberBorder: Color(0xFF4A3A16),
  );

  @override
  AppPalette copyWith({
    Color? bg,
    Color? surface,
    Color? surfaceSubtle,
    Color? border,
    Color? hairline,
    Color? ink,
    Color? onInk,
    Color? secondary,
    Color? muted,
    Color? disabled,
    Color? green,
    Color? greenBg,
    Color? greenBorder,
    Color? red,
    Color? redBg,
    Color? redBorder,
    Color? amber,
    Color? amberBg,
    Color? amberBorder,
  }) {
    return AppPalette(
      bg: bg ?? this.bg,
      surface: surface ?? this.surface,
      surfaceSubtle: surfaceSubtle ?? this.surfaceSubtle,
      border: border ?? this.border,
      hairline: hairline ?? this.hairline,
      ink: ink ?? this.ink,
      onInk: onInk ?? this.onInk,
      secondary: secondary ?? this.secondary,
      muted: muted ?? this.muted,
      disabled: disabled ?? this.disabled,
      green: green ?? this.green,
      greenBg: greenBg ?? this.greenBg,
      greenBorder: greenBorder ?? this.greenBorder,
      red: red ?? this.red,
      redBg: redBg ?? this.redBg,
      redBorder: redBorder ?? this.redBorder,
      amber: amber ?? this.amber,
      amberBg: amberBg ?? this.amberBg,
      amberBorder: amberBorder ?? this.amberBorder,
    );
  }

  @override
  AppPalette lerp(ThemeExtension<AppPalette>? other, double t) {
    if (other is! AppPalette) return this;
    return AppPalette(
      bg: Color.lerp(bg, other.bg, t)!,
      surface: Color.lerp(surface, other.surface, t)!,
      surfaceSubtle: Color.lerp(surfaceSubtle, other.surfaceSubtle, t)!,
      border: Color.lerp(border, other.border, t)!,
      hairline: Color.lerp(hairline, other.hairline, t)!,
      ink: Color.lerp(ink, other.ink, t)!,
      onInk: Color.lerp(onInk, other.onInk, t)!,
      secondary: Color.lerp(secondary, other.secondary, t)!,
      muted: Color.lerp(muted, other.muted, t)!,
      disabled: Color.lerp(disabled, other.disabled, t)!,
      green: Color.lerp(green, other.green, t)!,
      greenBg: Color.lerp(greenBg, other.greenBg, t)!,
      greenBorder: Color.lerp(greenBorder, other.greenBorder, t)!,
      red: Color.lerp(red, other.red, t)!,
      redBg: Color.lerp(redBg, other.redBg, t)!,
      redBorder: Color.lerp(redBorder, other.redBorder, t)!,
      amber: Color.lerp(amber, other.amber, t)!,
      amberBg: Color.lerp(amberBg, other.amberBg, t)!,
      amberBorder: Color.lerp(amberBorder, other.amberBorder, t)!,
    );
  }
}

extension AppPaletteX on BuildContext {
  /// The active theme's semantic colours.
  ///
  /// Falls back to the palette matching the ambient brightness when the theme
  /// carries no [AppPalette] — e.g. a nested `Theme` override or a plain
  /// `MaterialApp` in a test. Never returns null, so a widget can read colours
  /// without knowing who built the theme above it.
  AppPalette get colors {
    final theme = Theme.of(this);
    return theme.extension<AppPalette>() ??
        (theme.brightness == Brightness.dark
            ? AppPalette.dark
            : AppPalette.light);
  }
}

class AppGradients {
  const AppGradients._();

  /// Primary brand gradient — used by the login hero section. Deliberately
  /// dark in both themes; content on top of it uses fixed light colours.
  static const LinearGradient brand = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF0C0E1F), Color(0xFF1A1346), Color(0xFF2D1B69)],
  );
}

class AppTheme {
  const AppTheme._();

  /// [baseTextTheme] overrides the Instrument Sans base — pass a plain
  /// [TextTheme] in tests to avoid fetching the font.
  static ThemeData light({TextTheme? baseTextTheme}) => _build(
        palette: AppPalette.light,
        brightness: Brightness.light,
        baseTextTheme: baseTextTheme ?? GoogleFonts.instrumentSansTextTheme(),
        overlayStyle: SystemUiOverlayStyle.dark,
      );

  static ThemeData dark({TextTheme? baseTextTheme}) => _build(
        palette: AppPalette.dark,
        brightness: Brightness.dark,
        baseTextTheme: baseTextTheme ??
            GoogleFonts.instrumentSansTextTheme(ThemeData.dark().textTheme),
        overlayStyle: SystemUiOverlayStyle.light,
      );

  /// Both themes are built from one spec so a styling change can't land in
  /// only one brightness.
  static ThemeData _build({
    required AppPalette palette,
    required Brightness brightness,
    required TextTheme baseTextTheme,
    required SystemUiOverlayStyle overlayStyle,
  }) {
    final isDark = brightness == Brightness.dark;

    // Every component style derives from [baseTextTheme] so the font is
    // resolved once, from a single source, for both brightnesses.
    final baseStyle = baseTextTheme.bodyMedium ?? const TextStyle();
    TextStyle font(double size, FontWeight weight, Color color) =>
        baseStyle.copyWith(fontSize: size, fontWeight: weight, color: color);

    final scheme = ColorScheme(
      brightness: brightness,
      primary: palette.ink,
      onPrimary: palette.onInk,
      primaryContainer: palette.surfaceSubtle,
      onPrimaryContainer: palette.ink,
      secondary: palette.secondary,
      onSecondary: palette.onInk,
      surface: palette.surface,
      onSurface: palette.ink,
      onSurfaceVariant: palette.secondary,
      surfaceContainerLowest: palette.surface,
      surfaceContainerHighest: palette.surfaceSubtle,
      outline: palette.border,
      outlineVariant: palette.hairline,
      error: palette.red,
      onError: palette.onInk,
      errorContainer: palette.redBg,
      onErrorContainer: palette.red,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      extensions: [palette],
      scaffoldBackgroundColor: palette.bg,
      canvasColor: palette.surface,
      colorScheme: scheme,
      // Body text defaults to `ink`; without this, dark mode inherits the
      // light text theme's near-black and disappears into the surface.
      textTheme: baseTextTheme.apply(
        bodyColor: palette.ink,
        displayColor: palette.ink,
      ),
      iconTheme: IconThemeData(color: palette.secondary),
      appBarTheme: AppBarTheme(
        backgroundColor: palette.surface,
        foregroundColor: palette.ink,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        shadowColor: Colors.transparent,
        centerTitle: false,
        iconTheme: IconThemeData(color: palette.ink),
        actionsIconTheme: IconThemeData(color: palette.ink),
        titleTextStyle: font(17, FontWeight.w700, palette.ink),
        systemOverlayStyle: overlayStyle.copyWith(
          statusBarColor: Colors.transparent,
        ),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: palette.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(13),
          side: BorderSide(color: palette.border),
        ),
        margin: EdgeInsets.zero,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isDark ? palette.surfaceSubtle : palette.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.ink, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.red),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.red, width: 1.5),
        ),
        disabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: BorderSide(color: palette.hairline),
        ),
        // Typed input inherits `subtitle1` from the text theme, which is not
        // palette-aware on its own — pin it to `ink`.
        hintStyle:
            font(14, FontWeight.w400, palette.muted),
        labelStyle:
            font(14, FontWeight.w400, palette.secondary),
        floatingLabelStyle:
            font(14, FontWeight.w400, palette.ink),
        errorStyle: font(12, FontWeight.w400, palette.red),
        prefixIconColor: palette.muted,
        suffixIconColor: palette.muted,
      ),
      textSelectionTheme: TextSelectionThemeData(
        cursorColor: palette.ink,
        selectionColor: palette.ink.withValues(alpha: 0.25),
        selectionHandleColor: palette.ink,
      ),
      dividerTheme: DividerThemeData(color: palette.hairline, space: 1),
      chipTheme: ChipThemeData(
        backgroundColor: palette.surface,
        selectedColor: palette.ink,
        checkmarkColor: palette.onInk,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(99)),
        side: BorderSide(color: palette.border),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        labelStyle: font(12, FontWeight.w500, palette.ink),
        secondaryLabelStyle: font(12, FontWeight.w500, palette.onInk),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: palette.ink,
          foregroundColor: palette.onInk,
          // Disabled colours are left to Material, which derives them from
          // onSurface and so already tracks the active brightness.
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
          // No colour here — the button's foregroundColor supplies it.
          textStyle: baseStyle.copyWith(
            fontSize: 15,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: palette.ink,
          side: BorderSide(color: palette.border),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
          textStyle: baseStyle.copyWith(
            fontSize: 15,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: palette.ink),
      ),
      popupMenuTheme: PopupMenuThemeData(
        color: palette.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: palette.border),
        ),
        textStyle: font(13.5, FontWeight.w400, palette.ink),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: palette.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: BorderSide(color: palette.border),
        ),
        titleTextStyle: font(17, FontWeight.w700, palette.ink),
        contentTextStyle: font(14, FontWeight.w400, palette.secondary),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: palette.surface,
        surfaceTintColor: Colors.transparent,
        modalBackgroundColor: palette.surface,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: palette.ink,
        contentTextStyle: font(13.5, FontWeight.w400, palette.onInk),
        actionTextColor: palette.onInk,
        behavior: SnackBarBehavior.floating,
        shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      listTileTheme: ListTileThemeData(
        iconColor: palette.secondary,
        textColor: palette.ink,
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith(
          (s) => s.contains(WidgetState.selected) ? palette.onInk : palette.surface,
        ),
        trackColor: WidgetStateProperty.resolveWith(
          (s) => s.contains(WidgetState.selected) ? palette.ink : palette.border,
        ),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: palette.ink),
      tabBarTheme: TabBarThemeData(
        labelColor: palette.ink,
        unselectedLabelColor: palette.muted,
        indicatorColor: palette.ink,
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: palette.surface,
        selectedItemColor: palette.ink,
        unselectedItemColor: palette.muted,
      ),
    );
  }
}

/// Legacy widget: a gradient-filled [DecoratedBox] for the login hero.
/// Not used on any AppBar in the new design.
class AppBarGradient extends StatelessWidget {
  const AppBarGradient({super.key});

  @override
  Widget build(BuildContext context) => const DecoratedBox(
        decoration: BoxDecoration(gradient: AppGradients.brand),
      );
}
