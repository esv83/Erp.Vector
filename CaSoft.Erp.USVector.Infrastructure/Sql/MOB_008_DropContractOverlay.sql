-- =============================================================================
-- MOB_008 — Supprime le magasin Vector du contexte de mission (OC-8 / §3.A6).
-- Base : BD_ERP_MOBILE_APP
-- Idempotent : rejouable, chaque DROP est gardé par un test d'existence.
--
-- ⛔ NE PAS JOUER EN L'ÉTAT — voir PRÉALABLE. Le script est versionné pour être prêt,
--    pas pour être exécuté aujourd'hui.
--
-- DÉCISION
-- --------
-- Abandon pur, arbitré le 2026-08-26. Les 2 132 lignes de MOB_JOB_ATTRIBUTE_VALUE sont
-- des données de test : aucune reprise vers ORD_ORDER_CONTEXT_VALUE n'est écrite, et le
-- script de reprise côté Order (042_MigrateVectorJobAttributeValues.sql, jamais joué)
-- reste sans emploi. MOB_JOB_CONTRACT est vide — aucun type n'a jamais été sélectionné.
--
-- Ce qu'on assume : la saisie terrain antérieure à la bascule du 2026-08-25 n'est plus
-- relisible. Elle n'a jamais alimenté la facturation, qui lit les valeurs chez Orders.
--
-- PRÉALABLE — le seul, et il n'est pas rempli
-- -------------------------------------------
-- Ces tables portent encore le **chemin de désarmement** de la bascule. Tant que
-- ContextOrder:UseOrderCatalog / UseOrderAttributes existent dans le code, les remettre à
-- false doit rendre une API qui fonctionne : c'est le levier d'incident décrit en §3.A1,
-- et le seul retour arrière qui n'impose pas de coupure d'API.
--
-- Jouer ce script avant d'avoir retiré les drapeaux transforme donc le levier d'incident
-- en panne : le désarmement rendrait des 500 au lieu de l'ancien comportement.
--
-- À jouer quand, et seulement quand, les « Fin » d'A1 et d'A2 sont atteintes :
--   1. les refus 409 / 400 sont annoncés au dev web et absorbés côté front ;
--   2. les drapeaux et le second chemin qu'ils portent sont retirés du code
--      (ClListContractsUseCase, ClSelectContractUseCase, ClGetJobEditFormStructureUseCase,
--       ClUpdateJobEditUseCase, IJobAttributeOverlay, JobAttributeOverlayRepository
--       et ses tests, JobRepository.Save) ;
--   3. la publication qui porte ce retrait est en service depuis assez longtemps pour
--      qu'un retour arrière ne soit plus envisagé.
--
-- Ordre des DROP : dicté par les clés étrangères de MOB_002 (enfants d'abord).
--
-- Exécution (compte db_owner — ErpAccount n'a pas DROP TABLE) :
--   sqlcmd -S 192.168.1.109,1440 -U <db_owner> -P <mdp> -d BD_ERP_MOBILE_APP -i MOB_008_DropContractOverlay.sql
-- =============================================================================
USE [BD_ERP_MOBILE_APP];
GO

-- ── État des lieux : à lire AVANT de dérouler la suite ───────────────────────
SELECT 'MOB_JOB_ATTRIBUTE_VALUE' AS [table], COUNT(*) AS [lignes] FROM dbo.MOB_JOB_ATTRIBUTE_VALUE
UNION ALL SELECT 'MOB_JOB_CONTRACT',              COUNT(*) FROM dbo.MOB_JOB_CONTRACT
UNION ALL SELECT 'MOB_CONTRACT_ATTRIBUTE_OPTION', COUNT(*) FROM dbo.MOB_CONTRACT_ATTRIBUTE_OPTION
UNION ALL SELECT 'MOB_CONTRACT_ATTRIBUTE_CONTRACT', COUNT(*) FROM dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT
UNION ALL SELECT 'MOB_CONTRACT_ATTRIBUTE',        COUNT(*) FROM dbo.MOB_CONTRACT_ATTRIBUTE
UNION ALL SELECT 'MOB_CONTRACT_TYPE',             COUNT(*) FROM dbo.MOB_CONTRACT_TYPE;
GO

-- ── Filet optionnel ──────────────────────────────────────────────────────────
-- L'abandon est la décision retenue ; ces deux lignes ne sont donc pas actives. Les
-- décommenter garde une copie horodatée si quelqu'un veut pouvoir répondre plus tard à
-- « qu'avait saisi l'ambulancier avant la bascule ? ». Une copie non nettoyée devient
-- elle-même une dette : si on la prend, on lui donne une date de péremption.
--
-- SELECT * INTO dbo.ZZ_ARCHIVE_MOB_JOB_ATTRIBUTE_VALUE_20260826 FROM dbo.MOB_JOB_ATTRIBUTE_VALUE;
-- SELECT * INTO dbo.ZZ_ARCHIVE_MOB_JOB_CONTRACT_20260826        FROM dbo.MOB_JOB_CONTRACT;
GO

-- ── Suppression ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.MOB_JOB_ATTRIBUTE_VALUE', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_JOB_ATTRIBUTE_VALUE;
GO

IF OBJECT_ID('dbo.MOB_CONTRACT_ATTRIBUTE_OPTION', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_CONTRACT_ATTRIBUTE_OPTION;
GO

IF OBJECT_ID('dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_CONTRACT_ATTRIBUTE_CONTRACT;
GO

IF OBJECT_ID('dbo.MOB_JOB_CONTRACT', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_JOB_CONTRACT;
GO

IF OBJECT_ID('dbo.MOB_CONTRACT_ATTRIBUTE', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_CONTRACT_ATTRIBUTE;
GO

IF OBJECT_ID('dbo.MOB_CONTRACT_TYPE', 'U') IS NOT NULL
    DROP TABLE dbo.MOB_CONTRACT_TYPE;
GO

-- ── Contrôle ─────────────────────────────────────────────────────────────────
-- Doit rendre 0 ligne. Toute table restante signale un objet dépendant non prévu.
SELECT name AS [table_restante]
  FROM sys.tables
 WHERE name IN (N'MOB_CONTRACT_TYPE', N'MOB_CONTRACT_ATTRIBUTE', N'MOB_CONTRACT_ATTRIBUTE_CONTRACT',
                N'MOB_CONTRACT_ATTRIBUTE_OPTION', N'MOB_JOB_CONTRACT', N'MOB_JOB_ATTRIBUTE_VALUE');
GO
