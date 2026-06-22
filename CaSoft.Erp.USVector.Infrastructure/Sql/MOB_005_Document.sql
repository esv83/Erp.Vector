/* ============================================================================
   MOB_005_Document.sql — Documents/photos terrain d'une mission (TRF-10, spec §14)

   Stockage en BD Mobile (MVP ; migration blob ultérieure). Binaire en VARBINARY.
   Rattaché à la mission, catégorisé (bon de transport / prescription / admin / autre).
   DOC_MISSION_ID / *_CREW_ID référencent l'ERP par id (pas de FK cross-DB).

   DOC_CATEGORY (cf. EnDocumentCategory) : 1=bon transport, 2=prescription, 3=admin, 4=autre.
   Idempotent. Exécution (compte db_owner) :
     sqlcmd -S "192.168.1.109,1440" -d BD_ERP_MOBILE_APP -E -i "...\Sql\MOB_005_Document.sql"
   ============================================================================ */

USE BD_ERP_MOBILE_APP;
GO

IF OBJECT_ID(N'dbo.MOB_DOCUMENT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_DOCUMENT (
        DOC_ID             UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_MOB_DOC_ID DEFAULT NEWSEQUENTIALID(),
        DOC_MISSION_ID     UNIQUEIDENTIFIER NOT NULL,
        DOC_CATEGORY       INT              NOT NULL,   -- EnDocumentCategory
        DOC_CONTENT        VARBINARY(MAX)   NOT NULL,
        DOC_CONTENT_TYPE   NVARCHAR(100)    NOT NULL,
        DOC_BYTE_SIZE      INT              NOT NULL,
        DOC_FILE_NAME      NVARCHAR(260)    NULL,
        DOC_CAPTURED_AT    DATETIME2(0)     NOT NULL
            CONSTRAINT DF_MOB_DOC_CAPTURED DEFAULT SYSUTCDATETIME(),
        DOC_CAPTURED_CREW_ID UNIQUEIDENTIFIER NULL,

        CONSTRAINT PK_MOB_DOCUMENT PRIMARY KEY (DOC_ID)
    );

    CREATE INDEX IX_MOB_DOCUMENT_MISSION
        ON dbo.MOB_DOCUMENT (DOC_MISSION_ID, DOC_CAPTURED_AT DESC);

    PRINT 'Table MOB_DOCUMENT créée.';
END
ELSE
    PRINT 'Table MOB_DOCUMENT déjà présente.';
GO
