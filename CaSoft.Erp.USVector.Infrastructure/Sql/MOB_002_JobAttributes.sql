/* ============================================================================
   MOB_002_JobAttributes.sql — Overlay attributs de mission (MOB-13)

   Persistance OVERLAY en BD Mobile (aucune écriture ERP). Deux familles :

   Catalogue (définit le formulaire dynamique, possédé côté Mobile) :
     - MOB_CONTRACT_TYPE               : types de contrat
     - MOB_CONTRACT_ATTRIBUTE          : attributs (définis UNE fois)
     - MOB_CONTRACT_ATTRIBUTE_CONTRACT : liaison N..N attribut <-> contrats applicables
     - MOB_CONTRACT_ATTRIBUTE_OPTION   : options des champs « liste de choix »

   Applicabilité d'un attribut :
     - CAT_IS_GLOBAL = 1            -> s'applique à TOUS les types de contrat
     - CAT_IS_GLOBAL = 0 + liaisons -> s'applique aux contrats listés (1 ou plusieurs)

   Overlay (saisies terrain par mission) :
     - MOB_JOB_CONTRACT             : contrat sélectionné pour la mission
     - MOB_JOB_ATTRIBUTE_VALUE      : valeurs éditées (1 ligne par mission+attribut)

   Contrat d'affichage pour le dev web (par attribut) :
     - CAT_FIELD_TYPE : type de contrôle -> text | textarea | checkbox | list | phone | email | number | date
     - CAT_IS_MULTI   : champ multi-valué (saisie répétable : téléphones, e-mails)
     - Si CAT_FIELD_TYPE = 'list' : valeurs fournies par MOB_CONTRACT_ATTRIBUTE_OPTION.

   Les *_MISSION_ID référencent ERP ORD_MISSION par id, SANS FK cross-DB.
   Idempotent — ré-exécutable sans effet de bord. Inclut un SEED minimal provisoire.

   Exécution (compte db_owner — ErpAccount n'a pas CREATE TABLE) :
     sqlcmd -S "192.168.1.109,1440" -d BD_ERP_MOBILE_APP -E `
            -i "CaSoft.Erp.USVector.Infrastructure\Sql\MOB_002_JobAttributes.sql"
   ============================================================================ */

USE BD_ERP_MOBILE_APP;
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_CONTRACT_TYPE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_CONTRACT_TYPE (
        CTT_ID       INT IDENTITY(1,1) NOT NULL,
        CTT_CODE     NVARCHAR(50)  NOT NULL,
        CTT_DISPLAY  NVARCHAR(200) NOT NULL,
        CTT_ACTIVE   BIT NOT NULL CONSTRAINT DF_MOB_CTT_ACTIVE DEFAULT (1),
        CONSTRAINT PK_MOB_CONTRACT_TYPE PRIMARY KEY (CTT_ID)
    );
    CREATE UNIQUE INDEX UX_MOB_CONTRACT_TYPE_CODE ON dbo.MOB_CONTRACT_TYPE (CTT_CODE);
    PRINT 'Table MOB_CONTRACT_TYPE créée.';
END
ELSE PRINT 'Table MOB_CONTRACT_TYPE déjà présente.';
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_CONTRACT_ATTRIBUTE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_CONTRACT_ATTRIBUTE (
        CAT_ID             INT IDENTITY(1,1) NOT NULL,
        CAT_NAME           NVARCHAR(50) NOT NULL,       -- clé technique, unique
        CAT_LABEL          NVARCHAR(200) NOT NULL,
        CAT_FIELD_TYPE     NVARCHAR(30) NOT NULL,       -- type de contrôle web (cf. en-tête)
        CAT_INDEX          INT NOT NULL CONSTRAINT DF_MOB_CAT_INDEX DEFAULT (0),
        CAT_REQUIRED       BIT NOT NULL CONSTRAINT DF_MOB_CAT_REQUIRED DEFAULT (0),
        CAT_PLACEHOLDER    NVARCHAR(200) NULL,
        CAT_INSTANT_UPDATE BIT NOT NULL CONSTRAINT DF_MOB_CAT_INSTANT DEFAULT (0),
        CAT_IS_MULTI       BIT NOT NULL CONSTRAINT DF_MOB_CAT_MULTI DEFAULT (0),   -- saisie répétable
        CAT_IS_GLOBAL      BIT NOT NULL CONSTRAINT DF_MOB_CAT_GLOBAL DEFAULT (0),  -- 1 = tous les contrats
        CONSTRAINT PK_MOB_CONTRACT_ATTRIBUTE PRIMARY KEY (CAT_ID)
    );
    CREATE UNIQUE INDEX UX_MOB_CAT_NAME ON dbo.MOB_CONTRACT_ATTRIBUTE (CAT_NAME);
    PRINT 'Table MOB_CONTRACT_ATTRIBUTE créée.';
END
ELSE PRINT 'Table MOB_CONTRACT_ATTRIBUTE déjà présente.';
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT (
        CAC_ATTRIBUTE_ID INT NOT NULL,
        CAC_CONTRACT_ID  INT NOT NULL,
        CONSTRAINT PK_MOB_CAC PRIMARY KEY (CAC_ATTRIBUTE_ID, CAC_CONTRACT_ID),
        CONSTRAINT FK_MOB_CAC_ATTRIBUTE FOREIGN KEY (CAC_ATTRIBUTE_ID)
            REFERENCES dbo.MOB_CONTRACT_ATTRIBUTE (CAT_ID),
        CONSTRAINT FK_MOB_CAC_CONTRACT FOREIGN KEY (CAC_CONTRACT_ID)
            REFERENCES dbo.MOB_CONTRACT_TYPE (CTT_ID)
    );
    PRINT 'Table MOB_CONTRACT_ATTRIBUTE_CONTRACT créée.';
END
ELSE PRINT 'Table MOB_CONTRACT_ATTRIBUTE_CONTRACT déjà présente.';
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_CONTRACT_ATTRIBUTE_OPTION', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_CONTRACT_ATTRIBUTE_OPTION (
        CAO_ID           INT IDENTITY(1,1) NOT NULL,
        CAO_ATTRIBUTE_ID INT NOT NULL,
        CAO_KEY          INT NOT NULL,
        CAO_LABEL        NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_MOB_CONTRACT_ATTRIBUTE_OPTION PRIMARY KEY (CAO_ID),
        CONSTRAINT FK_MOB_CAO_ATTRIBUTE FOREIGN KEY (CAO_ATTRIBUTE_ID)
            REFERENCES dbo.MOB_CONTRACT_ATTRIBUTE (CAT_ID)
    );
    CREATE UNIQUE INDEX UX_MOB_CAO_ATTR_KEY
        ON dbo.MOB_CONTRACT_ATTRIBUTE_OPTION (CAO_ATTRIBUTE_ID, CAO_KEY);
    PRINT 'Table MOB_CONTRACT_ATTRIBUTE_OPTION créée.';
END
ELSE PRINT 'Table MOB_CONTRACT_ATTRIBUTE_OPTION déjà présente.';
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_JOB_CONTRACT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_JOB_CONTRACT (
        JCT_MISSION_ID  UNIQUEIDENTIFIER NOT NULL,     -- réf ERP ORD_MISSION (pas de FK cross-DB)
        JCT_CONTRACT_ID INT NOT NULL,
        JCT_UPDATED_AT  DATETIME2(0) NOT NULL
            CONSTRAINT DF_MOB_JCT_UPDATED DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MOB_JOB_CONTRACT PRIMARY KEY (JCT_MISSION_ID),
        CONSTRAINT FK_MOB_JCT_CONTRACT FOREIGN KEY (JCT_CONTRACT_ID)
            REFERENCES dbo.MOB_CONTRACT_TYPE (CTT_ID)
    );
    PRINT 'Table MOB_JOB_CONTRACT créée.';
END
ELSE PRINT 'Table MOB_JOB_CONTRACT déjà présente.';
GO

/* ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.MOB_JOB_ATTRIBUTE_VALUE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MOB_JOB_ATTRIBUTE_VALUE (
        JAV_MISSION_ID     UNIQUEIDENTIFIER NOT NULL,  -- réf ERP ORD_MISSION (pas de FK cross-DB)
        JAV_ATTRIBUTE_NAME NVARCHAR(50)  NOT NULL,
        JAV_VALUE          NVARCHAR(MAX) NULL,         -- multi-valué = JSON des items AJOUTÉS (hors ERP)
        JAV_UPDATED_AT     DATETIME2(0)  NOT NULL
            CONSTRAINT DF_MOB_JAV_UPDATED DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_MOB_JOB_ATTRIBUTE_VALUE PRIMARY KEY (JAV_MISSION_ID, JAV_ATTRIBUTE_NAME)
    );
    PRINT 'Table MOB_JOB_ATTRIBUTE_VALUE créée.';
END
ELSE PRINT 'Table MOB_JOB_ATTRIBUTE_VALUE déjà présente.';
GO

/* ============================================================================
   SEED minimal provisoire (MOB-13.2) — un exemple de chaque type de contrôle.
   ============================================================================ */

-- Attributs GLOBAUX (appliqués à tous les contrats) : commentaires + coordonnées.
MERGE dbo.MOB_CONTRACT_ATTRIBUTE AS tgt
USING (VALUES
    (N'COMMENTS', N'Commentaires', N'textarea', 100, 0),  -- texte multi-ligne
    (N'PHONES',   N'Téléphones',   N'phone',    101, 1),  -- multi-valué
    (N'MAILS',    N'E-mails',      N'email',    102, 1)   -- multi-valué
) AS src (CAT_NAME, CAT_LABEL, CAT_FIELD_TYPE, CAT_INDEX, CAT_IS_MULTI)
    ON tgt.CAT_NAME = src.CAT_NAME
WHEN NOT MATCHED THEN
    INSERT (CAT_NAME, CAT_LABEL, CAT_FIELD_TYPE, CAT_INDEX, CAT_IS_MULTI, CAT_IS_GLOBAL)
    VALUES (src.CAT_NAME, src.CAT_LABEL, src.CAT_FIELD_TYPE, src.CAT_INDEX, src.CAT_IS_MULTI, 1);

-- Attributs spécifiques (non globaux) : un text, un checkbox, une liste de choix.
MERGE dbo.MOB_CONTRACT_ATTRIBUTE AS tgt
USING (VALUES
    (N'REFERENCE', N'Référence',   N'text',     1, 0),
    (N'URGENT',    N'Urgent',      N'checkbox', 2, 0),
    (N'PMT',       N'PMT présent', N'list',     3, 0)
) AS src (CAT_NAME, CAT_LABEL, CAT_FIELD_TYPE, CAT_INDEX, CAT_IS_MULTI)
    ON tgt.CAT_NAME = src.CAT_NAME
WHEN NOT MATCHED THEN
    INSERT (CAT_NAME, CAT_LABEL, CAT_FIELD_TYPE, CAT_INDEX, CAT_IS_MULTI, CAT_IS_GLOBAL)
    VALUES (src.CAT_NAME, src.CAT_LABEL, src.CAT_FIELD_TYPE, src.CAT_INDEX, src.CAT_IS_MULTI, 0);

-- Contrat exemple.
IF NOT EXISTS (SELECT 1 FROM dbo.MOB_CONTRACT_TYPE WHERE CTT_CODE = N'STANDARD')
    INSERT dbo.MOB_CONTRACT_TYPE (CTT_CODE, CTT_DISPLAY) VALUES (N'STANDARD', N'Transport standard');

DECLARE @stdId INT = (SELECT CTT_ID FROM dbo.MOB_CONTRACT_TYPE WHERE CTT_CODE = N'STANDARD');

-- Liaison des attributs spécifiques au contrat STANDARD (1..N contrats possibles).
MERGE dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT AS tgt
USING (
    SELECT a.CAT_ID, @stdId AS CTT_ID
    FROM dbo.MOB_CONTRACT_ATTRIBUTE a
    WHERE a.CAT_NAME IN (N'REFERENCE', N'URGENT', N'PMT')
) AS src (CAC_ATTRIBUTE_ID, CAC_CONTRACT_ID)
    ON tgt.CAC_ATTRIBUTE_ID = src.CAC_ATTRIBUTE_ID AND tgt.CAC_CONTRACT_ID = src.CAC_CONTRACT_ID
WHEN NOT MATCHED THEN
    INSERT (CAC_ATTRIBUTE_ID, CAC_CONTRACT_ID) VALUES (src.CAC_ATTRIBUTE_ID, src.CAC_CONTRACT_ID);

-- Valeurs de la liste de choix PMT.
DECLARE @pmtId INT = (SELECT CAT_ID FROM dbo.MOB_CONTRACT_ATTRIBUTE WHERE CAT_NAME = N'PMT');
MERGE dbo.MOB_CONTRACT_ATTRIBUTE_OPTION AS tgt
USING (VALUES (@pmtId, 0, N'Non'), (@pmtId, 1, N'Oui')) AS src (CAO_ATTRIBUTE_ID, CAO_KEY, CAO_LABEL)
    ON tgt.CAO_ATTRIBUTE_ID = src.CAO_ATTRIBUTE_ID AND tgt.CAO_KEY = src.CAO_KEY
WHEN NOT MATCHED THEN
    INSERT (CAO_ATTRIBUTE_ID, CAO_KEY, CAO_LABEL) VALUES (src.CAO_ATTRIBUTE_ID, src.CAO_KEY, src.CAO_LABEL);

PRINT 'MOB_002_JobAttributes.sql terminé.';
GO
