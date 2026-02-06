/*
  Backfill e-commerce data:
  - ProductCatalogEntries for all Parts
  - Default ProductVariants (one per Part)
  - Basic dynamic filters via ProductAttributes + CategoryAttributes
  - VariantAttributeValues for Brand, SKU, PartNumber

  Safe to re-run (uses NOT EXISTS guards).
*/

SET NOCOUNT ON;

DECLARE @now DATETIME2 = SYSDATETIME();

/* 1) Ensure attribute group exists */
DECLARE @generalGroupId UNIQUEIDENTIFIER;
SELECT @generalGroupId = Id
FROM ProductAttributeGroups
WHERE Name = 'General' AND Isdeleted = 0;

IF @generalGroupId IS NULL
BEGIN
    SET @generalGroupId = NEWID();
    INSERT INTO ProductAttributeGroups
    (Id, Name, SortOrder, IsActive, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
    VALUES
    (@generalGroupId, 'General', 0, 1, @now, @now, 'system', 'system', 0);
END

/* 2) Ensure attributes exist */
DECLARE @brandAttrId UNIQUEIDENTIFIER;
DECLARE @skuAttrId UNIQUEIDENTIFIER;
DECLARE @partNumberAttrId UNIQUEIDENTIFIER;

SELECT @brandAttrId = Id FROM ProductAttributes WHERE Code = 'BRAND' AND Isdeleted = 0;
IF @brandAttrId IS NULL
BEGIN
    SET @brandAttrId = NEWID();
    INSERT INTO ProductAttributes
    (Id, AttributeGroupId, Name, Code, DataType, Unit, IsActive, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
    VALUES
    (@brandAttrId, @generalGroupId, 'Brand', 'BRAND', 'text', NULL, 1, @now, @now, 'system', 'system', 0);
END

SELECT @skuAttrId = Id FROM ProductAttributes WHERE Code = 'SKU' AND Isdeleted = 0;
IF @skuAttrId IS NULL
BEGIN
    SET @skuAttrId = NEWID();
    INSERT INTO ProductAttributes
    (Id, AttributeGroupId, Name, Code, DataType, Unit, IsActive, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
    VALUES
    (@skuAttrId, @generalGroupId, 'SKU', 'SKU', 'text', NULL, 1, @now, @now, 'system', 'system', 0);
END

SELECT @partNumberAttrId = Id FROM ProductAttributes WHERE Code = 'PART_NUMBER' AND Isdeleted = 0;
IF @partNumberAttrId IS NULL
BEGIN
    SET @partNumberAttrId = NEWID();
    INSERT INTO ProductAttributes
    (Id, AttributeGroupId, Name, Code, DataType, Unit, IsActive, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
    VALUES
    (@partNumberAttrId, @generalGroupId, 'Part Number', 'PART_NUMBER', 'text', NULL, 1, @now, @now, 'system', 'system', 0);
END

/* 3) Map attributes to all categories (filterable) */
INSERT INTO CategoryAttributes
(Id, CategoryId, AttributeId, IsRequired, IsFilterable, FilterType, SortOrder, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT NEWID(), c.Id, a.AttributeId, 0, 1, 'select', a.SortOrder, @now, @now, 'system', 'system', 0
FROM Categories c
CROSS JOIN (VALUES
    (@brandAttrId, 1),
    (@skuAttrId, 2),
    (@partNumberAttrId, 3)
) a(AttributeId, SortOrder)
WHERE c.Isdeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM CategoryAttributes ca
    WHERE ca.CategoryId = c.Id AND ca.AttributeId = a.AttributeId AND ca.Isdeleted = 0
);

/* 4) ProductCatalogEntries for all Parts */
INSERT INTO ProductCatalogEntries
(Id, PartId, Slug, ShortDescription, IsPublished, PublishedAt, IsFeatured, FeaturedRank, PopularityScore, PrimaryImageUrl, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT
    NEWID(),
    p.Id,
    LOWER(
        REPLACE(REPLACE(REPLACE(p.Name, ' ', '-'), '/', '-'), '--', '-')
        + '-' + RIGHT(CONVERT(VARCHAR(36), p.Id), 8)
    ),
    LEFT(ISNULL(p.Description, ''), 300),
    1,
    @now,
    0,
    0,
    0,
    '',
    @now,
    @now,
    'system',
    'system',
    0
FROM Parts p
WHERE p.Isdeleted = 0 AND p.IsActive = 1
AND NOT EXISTS (
    SELECT 1 FROM ProductCatalogEntries e WHERE e.PartId = p.Id
);

/* 5) Default variants for Parts without variants */
INSERT INTO ProductVariants
(Id, PartId, Name, Code, SKU, CostPrice, SellingPrice, Currency, IsActive, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT
    NEWID(),
    p.Id,
    p.Name,
    'DEFAULT',
    p.SKU,
    p.CostPrice,
    p.SellingPrice,
    'BDT',
    1,
    @now,
    @now,
    'system',
    'system',
    0
FROM Parts p
WHERE p.Isdeleted = 0 AND p.IsActive = 1
AND NOT EXISTS (
    SELECT 1 FROM ProductVariants v WHERE v.PartId = p.Id AND v.Isdeleted = 0
);

/* 6) VariantAttributeValues for Brand / SKU / PartNumber */
INSERT INTO VariantAttributeValues
(Id, VariantId, AttributeId, OptionId, ValueText, ValueNumber, ValueBool, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT
    NEWID(),
    v.Id,
    @brandAttrId,
    NULL,
    ISNULL(b.Name, 'Unbranded'),
    NULL,
    NULL,
    @now,
    @now,
    'system',
    'system',
    0
FROM ProductVariants v
JOIN Parts p ON p.Id = v.PartId
LEFT JOIN Brands b ON b.Id = p.BrandId
WHERE v.Isdeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM VariantAttributeValues x
    WHERE x.VariantId = v.Id AND x.AttributeId = @brandAttrId
);

INSERT INTO VariantAttributeValues
(Id, VariantId, AttributeId, OptionId, ValueText, ValueNumber, ValueBool, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT
    NEWID(),
    v.Id,
    @skuAttrId,
    NULL,
    ISNULL(p.SKU, ''),
    NULL,
    NULL,
    @now,
    @now,
    'system',
    'system',
    0
FROM ProductVariants v
JOIN Parts p ON p.Id = v.PartId
WHERE v.Isdeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM VariantAttributeValues x
    WHERE x.VariantId = v.Id AND x.AttributeId = @skuAttrId
);

INSERT INTO VariantAttributeValues
(Id, VariantId, AttributeId, OptionId, ValueText, ValueNumber, ValueBool, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, Isdeleted)
SELECT
    NEWID(),
    v.Id,
    @partNumberAttrId,
    NULL,
    p.PartNumber,
    NULL,
    NULL,
    @now,
    @now,
    'system',
    'system',
    0
FROM ProductVariants v
JOIN Parts p ON p.Id = v.PartId
WHERE v.Isdeleted = 0
AND NOT EXISTS (
    SELECT 1 FROM VariantAttributeValues x
    WHERE x.VariantId = v.Id AND x.AttributeId = @partNumberAttrId
);

PRINT 'Backfill completed.';
