/* ============================================================================
   MOB_001_Initial.sql — Schéma initial BD Mobile (MOB-0)

   Crée la base dédiée BD_ERP_MOBILE_APP (si absente) et les 3 tables MVP :
     - MOB_SESSION        : sessions / tokens équipage (legacy : T_EQUIPAGE_EQ.EQ_TOKEN)
     - MOB_MISSION_STATE  : timeline opérationnelle par mission (legacy : T_JOB_TIME)
     - MOB_SIGNATURE      : signature patient base64 (legacy : T_SIGNATURE_SIGN)

   Les colonnes *_CREW_ID / *_MISSION_ID référencent les entités ERP
   (CRW_CREW / ORD_MISSION de BD_ERP_SANITAIRE_DEV) par id, SANS contrainte FK
   (cross-database — cohérence assurée par l'Application).

   Idempotent — ré-exécutable sans effet de bord.
   Exécution :
     sqlcmd -S "192.168.1.109,1440" -U ErpAccount -P "***" -d master `
            -i "CaSoft.Erp.Mobile.Infrastructure\Sql\MOB_001_Initial.sql"
   ============================================================================ */

IF DB_ID(N'BD_ERP_MOBILE_APP') IS NULL
BEGIN
    PRINT 'Création de la base BD_ERP_MOBILE_APP...';
    CREATE DATABASE BD_ERP_MOBILE_APP;
END
ELSE
    PRINT 'Base BD_ERP_MOBILE_APP déjà présente.';
GO

USE BD_ERP_MOBILE_APP;
GO

/* ----------------------------------------------------------------------------
   MOB_SESSION — session de connexion d'un équipage (token mobile)
   Legacy : EQ_TOKEN / EQ_DEBUT / EQ_FIN portés par T_EQUIPAGE_EQ ;
   ici externalisés : l'équipage vit côté ERP, la session côté mobile.
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_SESSION', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_SESSION (
        SES_ID          UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_MOB_SESSION_ID DEFAULT NEWSEQUENTIALID(),
        SES_TOKEN       UNIQUEIDENTIFIER NOT NULL,          -- token présenté par l'app (AutorizeJob)
        SES_CREW_ID     UNIQUEIDENTIFIER NOT NULL,          -- réf. ERP CRW_CREW.CRW_ID (pas de FK cross-DB)
        SES_STARTED_AT  DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_SESSION_STARTED DEFAULT SYSUTCDATETIME(),
        SES_ENDED_AT    DATETIME2(0)     NULL,              -- NULL = session active (fin de service → MOB-12)

        CONSTRAINT PK_MOB_SESSION PRIMARY KEY (SES_ID)
    );

    CREATE UNIQUE INDEX UX_MOB_SESSION_TOKEN ON dbo.MOB_SESSION (SES_TOKEN);

    -- Une seule session active par équipage
    CREATE UNIQUE INDEX UX_MOB_SESSION_CREW_ACTIVE ON dbo.MOB_SESSION (SES_CREW_ID)
        WHERE SES_ENDED_AT IS NULL;

    PRINT 'Table MOB_SESSION créée.';
END
ELSE
    PRINT 'Table MOB_SESSION déjà présente.';
GO

/* ----------------------------------------------------------------------------
   MOB_MISSION_STATE — timeline opérationnelle d'une mission (1:1 ORD_MISSION)
   Legacy T_JOB_TIME : JOB_ACK / JOB_LU / JOB_ER / JOB_SP / JOB_TER
                       → ack / lu / en route / sur place / terminée
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_MISSION_STATE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_MISSION_STATE (
        MST_MISSION_ID    UNIQUEIDENTIFIER NOT NULL,        -- réf. ERP ORD_MISSION.MIS_ID (pas de FK cross-DB)
        MST_ACK_AT        DATETIME2(0)     NULL,            -- acquittée        (legacy JOB_ACK)
        MST_READ_AT       DATETIME2(0)     NULL,            -- lue              (legacy JOB_LU)
        MST_GO_AT         DATETIME2(0)     NULL,            -- en route         (legacy JOB_ER)
        MST_ONSITE_AT     DATETIME2(0)     NULL,            -- sur place        (legacy JOB_SP)
        MST_TERMINATED_AT DATETIME2(0)     NULL,            -- terminée         (legacy JOB_TER)
        MST_UPDATED_AT    DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_MISSION_STATE_UPDATED DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MOB_MISSION_STATE PRIMARY KEY (MST_MISSION_ID)
    );

    PRINT 'Table MOB_MISSION_STATE créée.';
END
ELSE
    PRINT 'Table MOB_MISSION_STATE déjà présente.';
GO

/* ----------------------------------------------------------------------------
   MOB_SIGNATURE — signature patient (1:1 ORD_MISSION)
   Legacy T_SIGNATURE_SIGN : MI_ID / SIGN_DATA / SIGN_DATETIME
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_SIGNATURE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_SIGNATURE (
        SIG_MISSION_ID  UNIQUEIDENTIFIER NOT NULL,          -- réf. ERP ORD_MISSION.MIS_ID (pas de FK cross-DB)
        SIG_DATA        NVARCHAR(MAX)    NOT NULL,          -- image base64
        SIG_DATETIME    DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_SIGNATURE_DATETIME DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MOB_SIGNATURE PRIMARY KEY (SIG_MISSION_ID)
    );

    PRINT 'Table MOB_SIGNATURE créée.';
END
ELSE
    PRINT 'Table MOB_SIGNATURE déjà présente.';
GO

PRINT 'MOB_001_Initial.sql terminé.';
GO
