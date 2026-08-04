-- GENERATED FILE -- DO NOT HAND-EDIT.
-- Legacy SQLite schema dumped from a real MimyShop Store.db.
-- Regenerate with (PowerShell):
--   $env:INDYPOS_REGENERATE_LEGACY_SCHEMA = "1"
--   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
-- Tables: 10
-- Schema only. Never add rows: this repository is public.

-- InventoryProduct
CREATE TABLE "InventoryProduct" (
	"InventoryProductId"	INTEGER NOT NULL UNIQUE,
	"Barcode"	TEXT NOT NULL UNIQUE,
	"Description"	TEXT NOT NULL,
	"Manufacturer"	TEXT,
	"Brand"	TEXT,
	"Category"	INTEGER,
	"QuantityInStock"	INTEGER NOT NULL DEFAULT 1,
	"GroupPriceQuantity"	INTEGER,
	"IsTrackable"	INTEGER DEFAULT 1,
	"DateCreated"	TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT, UnitPrice NUMERIC NOT NULL DEFAULT 0, GroupPrice NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InventoryProductId" AUTOINCREMENT)
);

-- Invoice
CREATE TABLE "Invoice" (
	"InvoiceId"	INTEGER NOT NULL UNIQUE,
	"UserId"	INTEGER NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP, Total NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InvoiceId" AUTOINCREMENT)
);

-- InvoiceProduct
CREATE TABLE "InvoiceProduct" (
	"InvoiceProductId"	INTEGER NOT NULL UNIQUE,
	"Priority"	INTEGER,
	"InvoiceId"	INTEGER NOT NULL,
	"InventoryProductId"	INTEGER NOT NULL,
	"Barcode"	TEXT,
	"Description"	TEXT NOT NULL,
	"Manufacturer"	TEXT,
	"Brand"	TEXT,
	"Category"	INTEGER,
	"Quantity"	INTEGER NOT NULL DEFAULT 1,
	"IsTrackable"	INTEGER DEFAULT 1,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"Note"	TEXT, UnitPrice NUMERIC NOT NULL DEFAULT 0, GroupPrice NUMERIC NOT NULL DEFAULT 0, IsGroupProduct INTEGER NOT NULL DEFAULT 0, OriginalUnitPrice NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("InvoiceProductId" AUTOINCREMENT)
);

-- Payment
CREATE TABLE "Payment" (
	"PaymentId"	INTEGER NOT NULL UNIQUE,
	"InvoiceId"	INTEGER NOT NULL,
	"PaymentTypeId"	INTEGER NOT NULL DEFAULT 1,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"Note"	TEXT, Amount NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY("PaymentId" AUTOINCREMENT)
);

-- PaymentType
CREATE TABLE "PaymentType" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Type"	TEXT,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

-- ProductBarcodeCounter
CREATE TABLE "ProductBarcodeCounter" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Counter"	INTEGER,
	PRIMARY KEY("Id" AUTOINCREMENT)
);

-- ProductCategory
CREATE TABLE "ProductCategory" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Category"	TEXT NOT NULL,
	PRIMARY KEY("Id")
);

-- User
CREATE TABLE "User" (
	"UserId"	INTEGER NOT NULL UNIQUE,
	"FirstName"	TEXT,
	"LastName"	TEXT,
	"RoleId"	INTEGER NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT,
	PRIMARY KEY("UserId" AUTOINCREMENT)
);

-- UserCredential
CREATE TABLE "UserCredential" (
	"UserId"	INTEGER NOT NULL UNIQUE,
	"Username"	TEXT,
	"Password"	TEXT NOT NULL,
	"DateCreated"	TEXT DEFAULT CURRENT_TIMESTAMP,
	"DateUpdated"	TEXT,
	PRIMARY KEY("UserId")
);

-- UserRole
CREATE TABLE "UserRole" (
	"Id"	INTEGER NOT NULL UNIQUE,
	"Role"	TEXT NOT NULL,
	PRIMARY KEY("Id")
);

