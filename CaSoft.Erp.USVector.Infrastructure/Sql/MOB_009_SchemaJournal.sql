/* ============================================================================
   MOB_009_SchemaJournal.sql — Journal du schéma de la BD Vector (G4)

   Pose `__VectorSchema`, la table qui dit quels scripts ont été appliqués, et
   l'amorce avec ce qu'elle peut CONSTATER dans la base — jamais avec ce qu'on
   suppose. Une ligne amorcée porte `Origine = 'constat'`, une ligne posée par
   un script à venir portera `'script'` : on ne confondra pas les deux.

   POURQUOI. Prod et dev ont divergé en sens inverse le 06/08 (MOB_004/005/006) :
   500 opaque, une journée sans données terrain. Rien ne disait quel script
   manquait, parce que rien ne disait quels scripts étaient passés.

   L'APPLICATION NE MIGRE JAMAIS AU DÉMARRAGE. Elle lit cette table, la compare
   aux scripts embarqués, et le dit — au journal, et sur api/version/runtime.

   Idempotent — ré-exécutable sans effet de bord.
   Exécution :
     sqlcmd -S "192.168.1.109,1440" -U ErpAccount -P "***" -d BD_ERP_MOBILE_APP `
            -i "CaSoft.Erp.USVector.Infrastructure\Sql\MOB_009_SchemaJournal.sql"
   ============================================================================ */

IF OBJECT_ID(N'dbo.__VectorSchema', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__VectorSchema (
        ScriptId   nvarchar(150)  NOT NULL,
        AppliqueLe datetimeoffset NOT NULL CONSTRAINT DF___VectorSchema_AppliqueLe DEFAULT SYSDATETIMEOFFSET(),
        -- 'script' : la ligne a été posée par le script lui-même, au moment où il s'est appliqué.
        -- 'constat' : la ligne a été DÉDUITE de la présence de ses objets, par celui-ci.
        Origine    nvarchar(20)   NOT NULL CONSTRAINT DF___VectorSchema_Origine DEFAULT N'script',
        CONSTRAINT PK___VectorSchema PRIMARY KEY (ScriptId)
    );
END;
GO

/* ── Amorce : uniquement ce qui se constate ─────────────────────────────────
   MOB_002 et MOB_007 ne sont PAS amorcés, et c'est voulu : MOB_008 a supprimé
   les tables qu'ils posaient (overlay d'attributs), donc rien dans la base ne
   peut plus témoigner de leur passage. Ils ne sont pas non plus attendus par le
   contrôle au démarrage — un script dont l'effet a été défait n'a plus à être
   réclamé. L'historique, lui, vit dans `delivered.md` §5.1.
   ------------------------------------------------------------------------- */

MERGE dbo.__VectorSchema AS cible
USING (
    SELECT ScriptId, Present FROM (VALUES
        (N'MOB_001_Initial',
            CASE WHEN OBJECT_ID(N'dbo.MOB_MISSION_STATE', N'U') IS NOT NULL
                  AND OBJECT_ID(N'dbo.MOB_SIGNATURE', N'U')     IS NOT NULL
                  AND OBJECT_ID(N'dbo.MOB_SESSION', N'U')       IS NOT NULL
                 THEN 1 ELSE 0 END),
        (N'MOB_003_MutuelleCard',
            CASE WHEN OBJECT_ID(N'dbo.MOB_MUTUELLE_CARD', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (N'MOB_004_Anomaly',
            CASE WHEN OBJECT_ID(N'dbo.MOB_ANOMALY', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (N'MOB_005_Document',
            CASE WHEN OBJECT_ID(N'dbo.MOB_DOCUMENT', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (N'MOB_006_OperationalOutbox',
            CASE WHEN OBJECT_ID(N'dbo.MOB_OPERATIONAL_OUTBOX', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        -- Une base VIDE n'a pas d'overlay non plus : on n'en déduit « supprimé » que sur une base
        -- qui porte déjà le socle. Sans cette garde, une base neuve se déclarerait à jour.
        (N'MOB_008_DropContractOverlay',
            CASE WHEN OBJECT_ID(N'dbo.MOB_CONTRACT_TYPE', N'U') IS NULL
                  AND OBJECT_ID(N'dbo.MOB_MISSION_STATE', N'U') IS NOT NULL
                 THEN 1 ELSE 0 END)
    ) AS constats(ScriptId, Present)
    WHERE Present = 1
) AS source
ON cible.ScriptId = source.ScriptId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ScriptId, Origine) VALUES (source.ScriptId, N'constat');
GO

-- Celui-ci s'inscrit lui-même : il vient de s'appliquer, l'heure est donc la bonne.
IF NOT EXISTS (SELECT 1 FROM dbo.__VectorSchema WHERE ScriptId = N'MOB_009_SchemaJournal')
    INSERT INTO dbo.__VectorSchema (ScriptId, Origine) VALUES (N'MOB_009_SchemaJournal', N'script');
GO

SELECT ScriptId, AppliqueLe, Origine FROM dbo.__VectorSchema ORDER BY ScriptId;
GO
