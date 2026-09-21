/* ============================================================================
   MOB_010_MutuelleCardOcr.sql — Champs PROPOSÉS par la lecture automatique (P3)

   La lecture automatique n'écrit JAMAIS dans les quatre champs de facturation
   (M5 : jamais d'écriture aveugle). Elle écrit à côté, dans des colonnes
   `_OCR_`, que l'opérateur voit et valide — et c'est la validation humaine qui
   recopie dans les colonnes officielles, comme la saisie manuelle le fait déjà.

   MMC_OCR_STATUS suit le cycle : none → pending → extracted → validated,
   ou `error` quand la lecture a échoué (le motif est dans MMC_OCR_LAST_ERROR,
   et le nombre de tentatives dans MMC_OCR_ATTEMPTS : une carte illisible ne
   doit pas être relancée sans fin, comme la file de projection l'a appris).

   Idempotent — ré-exécutable sans effet de bord.
   Exécution :
     sqlcmd -S "192.168.1.109,1440" -U ErpAccount -P "***" -d BD_ERP_MOBILE_APP `
            -i "CaSoft.Erp.USVector.Infrastructure\Sql\MOB_010_MutuelleCardOcr.sql"
   ============================================================================ */

IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_MUTUELLE_NAME') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_MUTUELLE_NAME nvarchar(200) NULL;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_AMC_CODE') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_AMC_CODE nvarchar(50) NULL;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_CONCENTRATEUR') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_CONCENTRATEUR nvarchar(100) NULL;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_TELETRANSMISSION') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_TELETRANSMISSION nvarchar(100) NULL;
GO
-- Confiance rendue par le modèle, journalisée ET servie : un opérateur qui voit
-- « confiance 0,4 » ne valide pas de la même façon qu'à 0,95.
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_CONFIDENCE') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_CONFIDENCE decimal(4,3) NULL;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_EXTRACTED_AT') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_EXTRACTED_AT datetime2(0) NULL;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_ATTEMPTS') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_ATTEMPTS int NOT NULL
        CONSTRAINT DF_MOB_MUTUELLE_CARD_OCR_ATTEMPTS DEFAULT 0;
GO
IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_OCR_LAST_ERROR') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_OCR_LAST_ERROR nvarchar(400) NULL;
GO

-- La file de lecture se lit par le statut : un index filtré, parce que `pending`
-- est une poignée de lignes sur des milliers.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MOB_MUTUELLE_CARD_OCR_PENDING'
                                           AND object_id = OBJECT_ID('dbo.MOB_MUTUELLE_CARD'))
    CREATE INDEX IX_MOB_MUTUELLE_CARD_OCR_PENDING
        ON dbo.MOB_MUTUELLE_CARD (MMC_CAPTURED_AT)
        WHERE MMC_OCR_STATUS = 'pending';
GO

IF OBJECT_ID(N'dbo.__VectorSchema', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__VectorSchema WHERE ScriptId = N'MOB_010_MutuelleCardOcr')
    INSERT INTO dbo.__VectorSchema (ScriptId, Origine) VALUES (N'MOB_010_MutuelleCardOcr', N'script');
GO
