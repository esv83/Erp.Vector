/* ============================================================================
   MOB_011_MutuelleCardInherited.sql — Une nouvelle photo hérite des champs validés

   Jusqu'ici, une nouvelle photo d'un patient déjà connu repartait avec des champs
   vides : la mutuelle, l'AMC, le concentrateur et la télétransmission validés sur
   la photo précédente disparaissaient de la carte courante — et du paquet de la
   facturation — jusqu'à une nouvelle validation, alors que la carte n'avait le
   plus souvent pas changé.

   Désormais la nouvelle photo les reprend, et dit d'où : MMC_FIELDS_INHERITED_FROM
   porte la date de la photo sur laquelle ils ont été validés (« repris de la photo
   du JJ/MM, à revérifier »). Une validation sur la nouvelle photo l'efface.

   Idempotent — ré-exécutable sans effet de bord.
   ⚠️ À jouer AVANT de publier le binaire qui lit cette colonne.
   Exécution :
     sqlcmd -S "192.168.1.109,1440" -U ErpAccount -P "***" -d BD_ERP_MOBILE_APP `
            -i "CaSoft.Erp.USVector.Infrastructure\Sql\MOB_011_MutuelleCardInherited.sql"
   ============================================================================ */

IF COL_LENGTH('dbo.MOB_MUTUELLE_CARD', 'MMC_FIELDS_INHERITED_FROM') IS NULL
    ALTER TABLE dbo.MOB_MUTUELLE_CARD ADD MMC_FIELDS_INHERITED_FROM datetime2(0) NULL;
GO

IF OBJECT_ID(N'dbo.__VectorSchema', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__VectorSchema WHERE ScriptId = N'MOB_011_MutuelleCardInherited')
    INSERT INTO dbo.__VectorSchema (ScriptId, Origine) VALUES (N'MOB_011_MutuelleCardInherited', N'script');
GO
