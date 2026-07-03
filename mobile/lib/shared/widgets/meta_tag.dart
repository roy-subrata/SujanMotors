import 'package:flutter/material.dart';

/// Simple `Label  Value` text pair used to show a product's brand or
/// category — the label word is what disambiguates them, not color/icon.
class MetaTag extends StatelessWidget {
  const MetaTag({
    super.key,
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final String value;
  final Color color;

  factory MetaTag.category(String value) =>
      MetaTag(label: 'Category', value: value, color: const Color(0xFF4F46E5));

  factory MetaTag.brand(String value) =>
      MetaTag(label: 'Brand', value: value, color: Colors.amber.shade800);

  @override
  Widget build(BuildContext context) => RichText(
        overflow: TextOverflow.ellipsis,
        text: TextSpan(
          children: [
            TextSpan(
              text: '$label  ',
              style: TextStyle(
                fontSize: 11.5,
                fontWeight: FontWeight.w500,
                color: Colors.grey.shade600,
              ),
            ),
            TextSpan(
              text: value,
              style: TextStyle(
                fontSize: 11.5,
                fontWeight: FontWeight.w700,
                color: color,
              ),
            ),
          ],
        ),
      );
}
