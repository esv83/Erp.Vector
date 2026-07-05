-- =============================================================================
-- MOB_006 — Outbox de projection opérationnelle (synchro régulation garantie).
-- Base : BD_ERP_MOBILE_APP
-- À exécuter avec un compte privilégié (db_owner / db_ddladmin).
-- Idempotent : ne fait rien si la table existe déjà.
--
-- 1 ligne par mission en attente de projection vers Orders.Api. Écrite dans la même
-- transaction qu'un changement de jalon (MOB_MISSION_STATE) ; un worker projette l'état
-- consolidé après un délai de debounce, avec relance jusqu'à succès, puis supprime la ligne.
-- =============================================================================
USE [BD_ERP_MOBILE_APP];
GO

IF OBJECT_ID('dbo.MOB_OPERATIONAL_OUTBOX', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_OPERATIONAL_OUTBOX
    (
        OOB_MISSION_ID     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MOB_OPERATIONAL_OUTBOX PRIMARY KEY,
        OOB_DISPATCH_AFTER DATETIME2(0)     NOT NULL,   -- UTC ; repoussé à chaque changement (debounce)
        OOB_ATTEMPTS       INT              NOT NULL CONSTRAINT DF_MOB_OOB_ATTEMPTS DEFAULT (0),
        OOB_LAST_ERROR     NVARCHAR(MAX)    NULL,
        OOB_UPDATED_AT     DATETIME2(0)     NOT NULL
    );

    CREATE INDEX IX_MOB_OPERATIONAL_OUTBOX_DISPATCH
        ON dbo.MOB_OPERATIONAL_OUTBOX (OOB_DISPATCH_AFTER);

    PRINT 'MOB_OPERATIONAL_OUTBOX créée.';
END
ELSE
    PRINT 'MOB_OPERATIONAL_OUTBOX existe déjà — aucune action.';
GO

-- Contrôle
SELECT CASE WHEN OBJECT_ID('dbo.MOB_OPERATIONAL_OUTBOX','U') IS NOT NULL
            THEN 'OK : table MOB_OPERATIONAL_OUTBOX présente'
            ELSE 'KO : table absente' END AS Resultat;
GO
