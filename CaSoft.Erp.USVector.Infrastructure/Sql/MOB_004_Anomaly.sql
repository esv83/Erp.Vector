/* ============================================================================
   MOB_004_Anomaly.sql — Anomalies terrain signalées par l'équipage (TRF-8, spec §17)

   Stockage en BD Mobile. Anomalies NON bloquantes : transférées dans le paquet
   field-data et arbitrées par la facturation. Rattachées à la mission ; historisées
   (plusieurs lignes possibles). ANO_MISSION_ID / *_CREW_ID référencent l'ERP par id
   (pas de FK cross-DB).

   ANO_TYPE (cf. EnAnomalyType) : 1=tél, 2=adresse, 3=patient, 4=admin, 5=impossibilité.
   Idempotent. Exécution (compte db_owner) :
     sqlcmd -S "192.168.1.109,1440" -d BD_ERP_MOBILE_APP -E -i "...\Sql\MOB_004_Anomaly.sql"
   ============================================================================ */

USE BD_ERP_MOBILE_APP;
GO

IF OBJECT_ID(N'dbo.MOB_ANOMALY', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_ANOMALY (
        ANO_ID             UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_MOB_ANO_ID DEFAULT NEWSEQUENTIALID(),
        ANO_MISSION_ID     UNIQUEIDENTIFIER NOT NULL,
        ANO_TYPE           INT              NOT NULL,   -- EnAnomalyType
        ANO_TEXT           NVARCHAR(MAX)    NULL,
        ANO_REPORTED_AT    DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_ANO_REPORTED DEFAULT SYSUTCDATETIME(),
        ANO_REPORTED_CREW_ID UNIQUEIDENTIFIER NULL,

        CONSTRAINT PK_MOB_ANOMALY PRIMARY KEY (ANO_ID)
    );

    CREATE INDEX IX_MOB_ANOMALY_MISSION
        ON dbo.MOB_ANOMALY (ANO_MISSION_ID, ANO_REPORTED_AT DESC);

    PRINT 'Table MOB_ANOMALY créée.';
END
ELSE
    PRINT 'Table MOB_ANOMALY déjà présente.';
GO
