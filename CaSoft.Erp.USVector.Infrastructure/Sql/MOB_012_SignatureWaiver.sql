/* ============================================================================
   MOB_012_SignatureWaiver.sql — « Le patient ne peut pas signer », et pourquoi

   L'alerte signature d'Order (Erp.Order, feature_alerte_signature_devplan.md)
   compte les missions soumises que le patient n'a pas signées. Certaines ne
   pouvaient pas l'être : patient inconscient, mineur seul, refus, décès. Sans
   moyen de le dire, l'équipage serait alerté pour ce qu'il ne pouvait pas faire,
   et la régulation Centre 15 serait bruyante. Arbitré le 10/11 : le motif entre
   en V1, choisi dans une liste tenue en base.

   MOB_SIGNATURE_WAIVER_REASON — la liste des motifs. Semis par MERGE : il se
     rejoue après correction d'un libellé. SWR_COMMENT_REQUIRED : le motif exige
     un commentaire (« Autre »). SWR_ACTIVE à 0 retire un motif sans effacer
     ceux déjà déclarés.

   MOB_SIGNATURE_WAIVER — au plus un motif par mission (1:1, comme la signature).
     SWV_DECLARED_AT est en UTC — contrairement à SIG_DATETIME, en heure locale
     par convention figée (decided.md, 04/10) : une donnée neuve naît en UTC.
     SWV_CREW_ID : l'équipage déclarant, s'il est fourni (traçabilité).
     Une mission peut porter une signature ET un motif : c'est Order qui juge,
     et la signature y prime.

   Une écriture dans l'une ou l'autre table arme l'outbox de projection
   (PRJ-1) : le motif part vers Order avec la présence de la signature (PRJ-2).

   Idempotent — ré-exécutable sans effet de bord.
   ⚠️ À jouer AVANT de publier le binaire qui lit ces tables.
   Exécution :
     sqlcmd -S "192.168.1.109,1440" -U ErpAccount -P "***" -d BD_ERP_MOBILE_APP `
            -i "CaSoft.Erp.USVector.Infrastructure\Sql\MOB_012_SignatureWaiver.sql"
   ============================================================================ */

IF OBJECT_ID(N'dbo.MOB_SIGNATURE_WAIVER_REASON', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_SIGNATURE_WAIVER_REASON (
        SWR_CODE             nvarchar(30)  NOT NULL CONSTRAINT PK_MOB_SIGNATURE_WAIVER_REASON PRIMARY KEY,
        SWR_LABEL            nvarchar(100) NOT NULL,
        SWR_COMMENT_REQUIRED bit           NOT NULL CONSTRAINT DF_MOB_SWR_COMMENT_REQUIRED DEFAULT (0),
        SWR_INDEX            int           NOT NULL,
        SWR_ACTIVE           bit           NOT NULL CONSTRAINT DF_MOB_SWR_ACTIVE DEFAULT (1)
    );
    PRINT 'MOB_SIGNATURE_WAIVER_REASON créée.';
END;
GO

MERGE dbo.MOB_SIGNATURE_WAIVER_REASON AS cible
USING (VALUES
    (N'ETAT_SANTE',    N'État de santé (inconscient, intubé, sédaté)', 0, 10),
    (N'MINEUR',        N'Mineur sans représentant',                    0, 20),
    (N'REFUS_PATIENT', N'Refus du patient',                            0, 30),
    (N'DECES',         N'Patient décédé',                              0, 40),
    (N'AUTRE',         N'Autre (à préciser)',                          1, 90)
) AS source (SWR_CODE, SWR_LABEL, SWR_COMMENT_REQUIRED, SWR_INDEX)
ON cible.SWR_CODE = source.SWR_CODE
WHEN MATCHED THEN
    UPDATE SET SWR_LABEL = source.SWR_LABEL,
               SWR_COMMENT_REQUIRED = source.SWR_COMMENT_REQUIRED,
               SWR_INDEX = source.SWR_INDEX
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SWR_CODE, SWR_LABEL, SWR_COMMENT_REQUIRED, SWR_INDEX, SWR_ACTIVE)
    VALUES (source.SWR_CODE, source.SWR_LABEL, source.SWR_COMMENT_REQUIRED, source.SWR_INDEX, 1);
GO

IF OBJECT_ID(N'dbo.MOB_SIGNATURE_WAIVER', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_SIGNATURE_WAIVER (
        SWV_MISSION_ID  uniqueidentifier NOT NULL CONSTRAINT PK_MOB_SIGNATURE_WAIVER PRIMARY KEY,
        SWV_REASON_CODE nvarchar(30)     NOT NULL,
        SWV_COMMENT     nvarchar(500)    NULL,
        SWV_DECLARED_AT datetime2(0)     NOT NULL,   -- UTC
        SWV_CREW_ID     uniqueidentifier NULL
    );
    PRINT 'MOB_SIGNATURE_WAIVER créée.';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MOB_SIGNATURE_WAIVER_REASON')
    ALTER TABLE dbo.MOB_SIGNATURE_WAIVER WITH CHECK
        ADD CONSTRAINT FK_MOB_SIGNATURE_WAIVER_REASON
        FOREIGN KEY (SWV_REASON_CODE) REFERENCES dbo.MOB_SIGNATURE_WAIVER_REASON (SWR_CODE);
GO

IF OBJECT_ID(N'dbo.__VectorSchema', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__VectorSchema WHERE ScriptId = N'MOB_012_SignatureWaiver')
    INSERT INTO dbo.__VectorSchema (ScriptId, Origine) VALUES (N'MOB_012_SignatureWaiver', N'script');
GO

-- Contrôle : les deux tables, la clé, et les cinq motifs actifs (attendu : 1, 1, 1, 5).
SELECT
    table_motifs   = CASE WHEN OBJECT_ID(N'dbo.MOB_SIGNATURE_WAIVER_REASON', N'U') IS NULL THEN 0 ELSE 1 END,
    table_declares = CASE WHEN OBJECT_ID(N'dbo.MOB_SIGNATURE_WAIVER', N'U') IS NULL THEN 0 ELSE 1 END,
    cle_motif      = (SELECT COUNT(*) FROM sys.foreign_keys WHERE name = N'FK_MOB_SIGNATURE_WAIVER_REASON'),
    motifs_actifs  = (SELECT COUNT(*) FROM dbo.MOB_SIGNATURE_WAIVER_REASON WHERE SWR_ACTIVE = 1);
GO
