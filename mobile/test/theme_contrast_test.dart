import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:autopartshop_mobile/core/theme/app_theme.dart';

/// Relative luminance per WCAG 2.1.
double _luminance(Color c) {
  double channel(double v) =>
      v <= 0.03928 ? v / 12.92 : math.pow((v + 0.055) / 1.055, 2.4).toDouble();
  return 0.2126 * channel(c.r) +
      0.7152 * channel(c.g) +
      0.0722 * channel(c.b);
}

/// WCAG contrast ratio between two opaque colours.
double _contrast(Color fg, Color bg) {
  final a = _luminance(fg);
  final b = _luminance(bg);
  final hi = math.max(a, b);
  final lo = math.min(a, b);
  return (hi + 0.05) / (lo + 0.05);
}

void main() {
  // AppTheme's default base pulls the Instrument Sans *TextTheme*, which the
  // test env can't fetch; inject a plain TextTheme instead.
  ThemeData light() => AppTheme.light(baseTextTheme: const TextTheme());
  ThemeData dark() => AppTheme.dark(baseTextTheme: const TextTheme());

  final themes = {'light': AppPalette.light, 'dark': AppPalette.dark};

  for (final entry in themes.entries) {
    final name = entry.key;
    final c = entry.value;

    group('$name palette', () {
      // The bug this guards: body/label text rendered in a near-black `ink`
      // on a dark surface (or vice versa) is unreadable.
      test('text tokens meet 4.5:1 on surface and bg', () {
        final surfaces = {'surface': c.surface, 'bg': c.bg};
        final texts = {'ink': c.ink, 'secondary': c.secondary};
        for (final s in surfaces.entries) {
          for (final t in texts.entries) {
            expect(
              _contrast(t.value, s.value),
              greaterThanOrEqualTo(4.5),
              reason: '$name: ${t.key} on ${s.key} is unreadable',
            );
          }
        }
      });

      test('muted meets 3:1 on surface', () {
        expect(_contrast(c.muted, c.surface), greaterThanOrEqualTo(3.0),
            reason: '$name: muted on surface');
      });

      test('onInk is readable on every high-emphasis fill', () {
        final fills = {
          'ink': c.ink,
          'green': c.green,
          'red': c.red,
          'amber': c.amber,
        };
        for (final f in fills.entries) {
          expect(
            _contrast(c.onInk, f.value),
            greaterThanOrEqualTo(4.5),
            reason: '$name: onInk on ${f.key} fill',
          );
        }
      });

      test('status foregrounds are readable on their own tinted bg', () {
        final pairs = {
          'green': (c.green, c.greenBg),
          'red': (c.red, c.redBg),
          'amber': (c.amber, c.amberBg),
        };
        for (final p in pairs.entries) {
          expect(
            _contrast(p.value.$1, p.value.$2),
            greaterThanOrEqualTo(4.5),
            reason: '$name: ${p.key} on ${p.key}Bg (status pill)',
          );
        }
      });

      test('status foregrounds are readable directly on surface', () {
        final fgs = {'green': c.green, 'red': c.red, 'amber': c.amber};
        for (final f in fgs.entries) {
          expect(
            _contrast(f.value, c.surface),
            greaterThanOrEqualTo(4.5),
            reason: '$name: ${f.key} text on surface',
          );
        }
      });

      test('borders are visible against their surface', () {
        expect(_contrast(c.border, c.surface), greaterThanOrEqualTo(1.15),
            reason: '$name: border on surface');
      });
    });
  }

  test('both ThemeData variants expose an AppPalette', () {
    for (final t in [light(), dark()]) {
      expect(t.extension<AppPalette>(), isNotNull);
    }
    expect(light().extension<AppPalette>(), same(AppPalette.light));
    expect(dark().extension<AppPalette>(), same(AppPalette.dark));
  });

  testWidgets('context.colors follows the active theme', (tester) async {
    late AppPalette seen;
    Future<void> pump(ThemeData theme) async {
      await tester.pumpWidget(MaterialApp(
        theme: theme,
        home: Builder(builder: (context) {
          seen = context.colors;
          return const SizedBox();
        }),
      ));
      // MaterialApp lerps between themes via AnimatedTheme; settle so we read
      // the destination palette rather than a mid-transition blend.
      await tester.pumpAndSettle();
    }

    await pump(light());
    expect(seen.ink, AppPalette.light.ink);

    await pump(dark());
    expect(seen.ink, AppPalette.dark.ink);

    // A theme that carries no AppPalette must fall back, not throw.
    await pump(ThemeData(brightness: Brightness.light));
    expect(seen.ink, AppPalette.light.ink);

    await pump(ThemeData(brightness: Brightness.dark));
    expect(seen.ink, AppPalette.dark.ink);
  });
}
