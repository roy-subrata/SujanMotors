# Database Standards

## Purpose

This document defines the database design, naming conventions, and best practices for creating and maintaining a scalable, performant, and maintainable database.

---

# 1. General Principles

Always design the database to be:

- Consistent
- Scalable
- Performant
- Secure
- Maintainable
- Normalized (unless denormalization is justified)

Follow:

- First Normal Form (1NF)
- Second Normal Form (2NF)
- Third Normal Form (3NF)

Only denormalize when there is a proven performance requirement.

---

# 2. Naming Conventions

## Tables

Use **plural** nouns — this matches every existing table in this repo
(`Brands`, `Categories`, `SalesOrders`, `AuditLogs`, …). Set names explicitly
with `builder.ToTable("...")` in the entity configuration.

✅ Good

```text
Products
Customers
PurchaseOrders
SalesOrders
```

❌ Bad

```text
Product
TblProduct
tbl_customer
ProductMaster
```

---

## Columns

Use PascalCase.

✅ Good

```text
Id
Name
CreatedAt
UpdatedAt
IsActive
CustomerId
```

❌ Bad

```text
customer_name
custname
created_date
```

---

## Primary Key

Every table must have

```text
Id
```

Example

```text
Product
--------
Id
Name
Price
```

Avoid

```text
ProductId
```

unless there is a specific reason.

---

## Foreign Keys

Use

```text
<TableName>Id
```

Example

```text
CustomerId
SupplierId
CategoryId
BrandId
```

---

# 3. Table Design

Every table should include

```text
Id
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Optional

```text
DeletedAt
DeletedBy
IsDeleted
```

for soft delete.

---

# 4. Data Types

Use appropriate types.

## IDs

```sql
uniqueidentifier
```

or

```sql
bigint
```

depending on the project.

---

## Text

Use

```sql
nvarchar
```

instead of

```sql
varchar
```

when Unicode support is required.

Avoid

```sql
nvarchar(max)
```

unless absolutely necessary.

---

## Money

Use

```sql
decimal(18,2)
```

Never use

```sql
float
```

for financial values.

---

## Boolean

Use

```sql
bit
```

---

## Date

Use

```sql
datetime2
```

instead of

```sql
datetime
```

---

# 5. Constraints

Always define

- Primary Keys
- Foreign Keys
- Unique Constraints
- Check Constraints
- Default Constraints

Example

```sql
Price >= 0
```

---

# 6. Primary Keys

Every table must have one primary key.

Example

```sql
Id UNIQUEIDENTIFIER PRIMARY KEY
```

---

# 7. Foreign Keys

Always enforce relationships.

Example

```text
Product
--------
CategoryId

Category
--------
Id
```

Never rely only on application logic.

---

# 8. Indexing

Create indexes for

- Foreign Keys
- Search columns
- Frequently filtered columns
- Frequently sorted columns

Avoid indexing every column.

Too many indexes reduce write performance.

---

# 9. Composite Indexes

Create composite indexes based on query patterns.

Example

```text
(CustomerId, OrderDate)
```

---

# 10. Unique Constraints

Use unique constraints for business rules.

Example

```text
ProductCode

Email

PhoneNumber

InvoiceNumber
```

---

# 11. Soft Delete

Prefer soft delete when business requires data recovery.

Columns

```text
IsDeleted

DeletedAt

DeletedBy
```

Always filter deleted records.

---

# 12. Audit Fields

Every transactional table should include

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Optional

```text
DeletedAt
DeletedBy
```

---

# 13. Lookup Tables

Use lookup tables instead of hardcoded values.

Example

```text
Country

Currency

PaymentMethod

OrderStatus
```

---

# 14. Junction Tables

Many-to-many relationships should use junction tables.

Example

```text
UserRole

ProductSupplier

OrderProduct
```

---

# 15. Transactions

Wrap multiple related updates in a transaction.

Example

```text
Create Purchase Order

↓

Insert Purchase

↓

Insert Purchase Details

↓

Update Inventory

↓

Commit
```

---

# 16. Cascade Delete

Avoid cascade delete on transactional tables.

Prefer application-controlled deletion.

---

# 17. Stored Procedures

Use only when

- Complex reporting
- Heavy aggregation
- Performance optimization

Avoid placing business logic inside stored procedures.

---

# 18. Views

Use views for

- Reports
- Read-only projections
- Dashboard queries

Avoid updating data through views.

---

# 19. Migrations

Never modify production databases manually.

Use EF Core Migrations.

Example

```bash
dotnet ef migrations add AddProductTable

dotnet ef database update
```

Every schema change must be version controlled.

---

# 20. Default Values

Use default constraints.

Example

```text
CreatedAt = GETUTCDATE()

IsActive = 1
```

---

# 21. Performance

Avoid

```sql
SELECT *
```

Select only required columns.

Example

```sql
SELECT
    Id,
    Name
FROM Product
```

---

# 22. Pagination

Always paginate large datasets.

Avoid

```sql
SELECT * FROM Product
```

Use

```sql
OFFSET

FETCH
```

or keyset pagination for large tables.

---

# 23. Security

Never store

- Passwords
- API Keys
- Tokens

in plain text.

Passwords must be hashed.

Sensitive data should be encrypted where appropriate.

---

# 24. Backup

Ensure

- Daily backups
- Point-in-time recovery
- Backup verification
- Disaster recovery plan

---

# 25. Documentation

Every table should have documented

- Purpose
- Relationships
- Constraints
- Indexes

---

# 26. Example Table

```text
Product
-----------------------------------------

Id

ProductCode

Name

CategoryId

BrandId

PurchasePrice

SalePrice

CurrentStock

MinimumStock

IsActive

CreatedAt

CreatedBy

UpdatedAt

UpdatedBy
```

---

# 27. Anti-Patterns

Avoid

❌ Generic columns

```text
Field1

Field2

Data1
```

❌ Multiple values in one column

```text
CategoryIds = "1,2,3"
```

❌ Duplicate data

❌ Missing foreign keys

❌ Missing indexes

❌ Nullable columns without reason

❌ Using float for money

❌ SELECT *

---

# 28. Database Checklist

Before creating a table

- Appropriate data types
- Primary key
- Foreign keys
- Required indexes
- Audit columns
- Soft delete (if needed)
- Constraints
- Default values
- Migration created
- Documentation updated