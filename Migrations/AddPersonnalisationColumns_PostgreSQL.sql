-- Script de migration pour PostgreSQL : Ajout des colonnes de personnalisation dans SectionConteneurItems
-- Date: 2025-10-28
-- Version: Compatible PostgreSQL 13+

-- ========================================
-- ÉTAPE 1: Vérifier l'existence de la table
-- ========================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables
                   WHERE table_name = 'SectionConteneurItems') THEN
        RAISE EXCEPTION 'La table SectionConteneurItems n''existe pas !';
    END IF;
END $$;

-- ========================================
-- ÉTAPE 2: Ajouter les colonnes si elles n'existent pas
-- ========================================

-- Colonne TitrePersonnalise (nullable, max 200 caractères)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'SectionConteneurItems'
                   AND column_name = 'TitrePersonnalise') THEN
        ALTER TABLE "SectionConteneurItems"
        ADD COLUMN "TitrePersonnalise" VARCHAR(200) NULL;

        RAISE NOTICE 'Colonne TitrePersonnalise ajoutée avec succès';
    ELSE
        RAISE NOTICE 'Colonne TitrePersonnalise existe déjà';
    END IF;
END $$;

-- Colonne ContenuHtmlPersonnalise (nullable, TEXT illimité)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'SectionConteneurItems'
                   AND column_name = 'ContenuHtmlPersonnalise') THEN
        ALTER TABLE "SectionConteneurItems"
        ADD COLUMN "ContenuHtmlPersonnalise" TEXT NULL;

        RAISE NOTICE 'Colonne ContenuHtmlPersonnalise ajoutée avec succès';
    ELSE
        RAISE NOTICE 'Colonne ContenuHtmlPersonnalise existe déjà';
    END IF;
END $$;

-- Colonne DateModificationPersonnalisation (nullable, timestamp)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'SectionConteneurItems'
                   AND column_name = 'DateModificationPersonnalisation') THEN
        ALTER TABLE "SectionConteneurItems"
        ADD COLUMN "DateModificationPersonnalisation" TIMESTAMP WITHOUT TIME ZONE NULL;

        RAISE NOTICE 'Colonne DateModificationPersonnalisation ajoutée avec succès';
    ELSE
        RAISE NOTICE 'Colonne DateModificationPersonnalisation existe déjà';
    END IF;
END $$;

-- ========================================
-- ÉTAPE 3: Vérification finale
-- ========================================
SELECT
    column_name AS "Colonne",
    data_type AS "Type",
    character_maximum_length AS "Longueur Max",
    is_nullable AS "Nullable"
FROM information_schema.columns
WHERE table_name = 'SectionConteneurItems'
  AND column_name IN ('TitrePersonnalise', 'ContenuHtmlPersonnalise', 'DateModificationPersonnalisation')
ORDER BY column_name;

-- ========================================
-- NOTES D'UTILISATION
-- ========================================
-- 1. Ce script est idempotent (peut être exécuté plusieurs fois sans erreur)
-- 2. Il vérifie l'existence de chaque colonne avant de l'ajouter
-- 3. Compatible avec PostgreSQL 13+ (EnableLegacyTimestampBehavior activé dans appsettings.PostgreSQL.json)
-- 4. Les colonnes sont nullable pour permettre la compatibilité avec les données existantes
-- 5. La requête finale affiche les colonnes ajoutées pour vérification

-- ========================================
-- COMMENT L'APPLIQUER VIA PGADMIN
-- ========================================
-- 1. Ouvrir pgAdmin 4
-- 2. Se connecter au serveur PostgreSQL (localhost:5432)
-- 3. Naviguer vers : Servers > PostgreSQL > Databases > GenerateurDOE_Prod
-- 4. Clic droit sur la base > Query Tool
-- 5. Copier/Coller ce script complet
-- 6. Cliquer sur Execute/Run (F5)
-- 7. Vérifier les messages NOTICE dans l'onglet "Messages"
-- 8. Vérifier le résultat de la requête SELECT finale
