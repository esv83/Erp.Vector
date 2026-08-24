-- =============================================================================
-- MOB_007 — Corrige le libellé d'ART80 : « Article 83 » → « Article 80 ».
-- Base : BD_ERP_MOBILE_APP
-- Idempotent : rejouable, ne touche que la ligne dont le libellé est encore faux.
--
-- POURQUOI
-- --------
-- Relevé le 2026-08-24 en comparant les deux catalogues de contexte : Vector affiche
-- « Article 83 » là où Order affiche « Article 80 ». C'est Vector qui a tort — l'article 80
-- est celui du code de la sécurité sociale, il n'existe pas d'article 83 ici. Le libellé
-- fautif est celui que l'ambulancier voit dans son sélecteur aujourd'hui.
--
-- PORTÉE
-- ------
-- Un libellé d'affichage, rien d'autre : ni code technique (CTT_CODE reste 'ART80', c'est
-- lui qui sert à la correspondance vers Order, cf. ContextOrderSelectionService), ni
-- identifiant, ni donnée de mission. MOB_JOB_CONTRACT est vide (aucun type n'a jamais été
-- sélectionné), donc aucune donnée saisie n'est concernée.
--
-- La table entière disparaît avec OC-3b, quand la liste viendra d'Order. Ce correctif ne
-- vaut donc que pour la fenêtre d'ici là — mais c'est cette fenêtre que le terrain regarde.
-- =============================================================================
USE [BD_ERP_MOBILE_APP];
GO

UPDATE dbo.MOB_CONTRACT_TYPE
   SET CTT_DISPLAY = N'Article 80'
 WHERE CTT_CODE = N'ART80'
   AND CTT_DISPLAY <> N'Article 80';

PRINT CONCAT(N'MOB_007 - ART80 : ', @@ROWCOUNT, N' libelle(s) corrige(s).');
GO

-- Contrôle : le catalogue Vector tel que le terrain le voit.
SELECT CTT_ID, CTT_CODE, CTT_DISPLAY, CTT_ACTIVE
  FROM dbo.MOB_CONTRACT_TYPE
 ORDER BY CTT_ID;
