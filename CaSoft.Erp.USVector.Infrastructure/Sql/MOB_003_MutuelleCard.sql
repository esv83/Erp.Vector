/* ============================================================================
   MOB_003_MutuelleCard.sql — Photo de carte mutuelle d'un bénéficiaire (P1)

   Stockage en BD Mobile (MVP ; migration blob ultérieure). Binaire en VARBINARY
   (pas de base64). Clé = bénéficiaire (la carte le suit d'une mission à l'autre),
   avec traçabilité de capture (équipage, mission). Champs extraits (OCR/IA, P3)
   nullables dès maintenant pour éviter une re-migration.

   MMC_BENEFICIARY_ID / *_MISSION_ID / *_CREW_ID référencent l'ERP par id (pas de FK cross-DB).
   Idempotent. Exécution (compte db_owner) :
     sqlcmd -S "192.168.1.109,1440" -d BD_ERP_MOBILE_APP -E -i "...\Sql\MOB_003_MutuelleCard.sql"
   ============================================================================ */

USE BD_ERP_MOBILE_APP;
GO

IF OBJECT_ID(N'dbo.MOB_MUTUELLE_CARD', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_MUTUELLE_CARD (
        MMC_ID               UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_MOB_MMC_ID DEFAULT NEWSEQUENTIALID(),
        MMC_BENEFICIARY_ID   UNIQUEIDENTIFIER NOT NULL,
        MMC_IMAGE            VARBINARY(MAX)   NOT NULL,
        MMC_CONTENT_TYPE     NVARCHAR(100)    NOT NULL,
        MMC_BYTE_SIZE        INT              NOT NULL,
        MMC_CAPTURED_AT      DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_MMC_CAPTURED DEFAULT SYSUTCDATETIME(),
        MMC_CAPTURED_CREW_ID UNIQUEIDENTIFIER NULL,
        MMC_MISSION_ID       UNIQUEIDENTIFIER NULL,

        -- Champs extraits (OCR/IA — P3)
        MMC_MUTUELLE_NAME    NVARCHAR(200)    NULL,
        MMC_AMC_CODE         NVARCHAR(50)     NULL,
        MMC_CONCENTRATEUR    NVARCHAR(100)    NULL,
        MMC_TELETRANSMISSION NVARCHAR(50)     NULL,
        MMC_OCR_STATUS       NVARCHAR(20)     NULL,   -- none|pending|extracted|validated
        MMC_OCR_VALIDATED_AT DATETIME2(0)     NULL,

        CONSTRAINT PK_MOB_MUTUELLE_CARD PRIMARY KEY (MMC_ID)
    );

    CREATE INDEX IX_MOB_MUTUELLE_CARD_BENEFICIARY
        ON dbo.MOB_MUTUELLE_CARD (MMC_BENEFICIARY_ID, MMC_CAPTURED_AT DESC);

    PRINT 'Table MOB_MUTUELLE_CARD créée.';
END
ELSE
    PRINT 'Table MOB_MUTUELLE_CARD déjà présente.';
GO
