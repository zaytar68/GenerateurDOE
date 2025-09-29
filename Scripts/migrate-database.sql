-- ===================================================================
-- Script de Migration Base de Données - Générateur DOE v2.1.3
-- ===================================================================
--
-- Ce script permet de migrer la base de données de développement (LocalDB)
-- vers un serveur SQL Server de production
--
-- ÉTAPES REQUISES AVANT L'EXÉCUTION :
-- 1. Sauvegarde de la base de données de développement
-- 2. Création de la base de données GenerateurDOE_Prod sur le serveur cible
-- 3. Mise à jour de la chaîne de connexion dans appsettings.Production.json
-- 4. Test de connectivité au serveur SQL Server de production
--
-- ===================================================================

USE [master]
GO

-- Création de la base de données de production si elle n'existe pas
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GenerateurDOE_Prod')
BEGIN
    CREATE DATABASE [GenerateurDOE_Prod]
    COLLATE SQL_Latin1_General_CP1_CI_AS
END
GO

USE [GenerateurDOE_Prod]
GO

-- Message d'information
PRINT 'Migration vers GenerateurDOE_Prod - Début'
PRINT 'Version : 2.1.3'
PRINT 'Date : ' + CONVERT(VARCHAR(20), GETDATE(), 120)
GO

-- ===================================================================
-- ÉTAPE 1: Application des migrations Entity Framework
-- ===================================================================
--
-- IMPORTANT : Exécuter cette commande depuis le répertoire de l'application :
--
-- dotnet ef database update --environment Production
--
-- Cette commande appliquera automatiquement toutes les migrations EF Core
-- pour créer la structure complète de la base de données
-- ===================================================================

-- Vérification que les migrations EF ont été appliquées
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    RAISERROR('ERREUR: Les migrations Entity Framework n''ont pas été appliquées. Exécutez "dotnet ef database update --environment Production" avant ce script.', 16, 1)
    RETURN
END
GO

-- ===================================================================
-- ÉTAPE 2: Données initiales et configuration
-- ===================================================================

-- Insertion des types de produits par défaut si ils n'existent pas
IF NOT EXISTS (SELECT 1 FROM TypesProduits)
BEGIN
    PRINT 'Insertion des types de produits par défaut...'

    INSERT INTO TypesProduits (Nom, IsDefault) VALUES
    ('Isolation', 1),
    ('Plomberie', 1),
    ('Électricité', 1),
    ('Ventilation', 1),
    ('Carrelage', 1),
    ('Peinture', 1),
    ('Menuiserie', 1),
    ('Chauffage', 1)
END
GO

-- Insertion des types de documents par défaut si ils n'existent pas
IF NOT EXISTS (SELECT 1 FROM TypesDocuments)
BEGIN
    PRINT 'Insertion des types de documents par défaut...'

    INSERT INTO TypesDocuments (Nom, IsDefault) VALUES
    ('Fiche technique', 1),
    ('Notice d''installation', 1),
    ('Garantie', 1),
    ('Certification', 1),
    ('Fiche sécurité', 1)
END
GO

-- Insertion des types de sections par défaut si ils n'existent pas
IF NOT EXISTS (SELECT 1 FROM TypesSections)
BEGIN
    PRINT 'Insertion des types de sections par défaut...'

    INSERT INTO TypesSections (Nom, IsDefault) VALUES
    ('Introduction', 1),
    ('Présentation', 1),
    ('Méthodologie', 1),
    ('Technique', 1),
    ('Sécurité', 1),
    ('Qualité', 1),
    ('Conclusion', 1),
    ('Annexes', 1)
END
GO

-- ===================================================================
-- ÉTAPE 3: Configuration des répertoires de production
-- ===================================================================

PRINT 'Configuration des répertoires de stockage...'

-- Création des répertoires de stockage (à adapter selon l'environnement)
-- Ces commandes doivent être exécutées manuellement sur le serveur de production :
--
-- mkdir "C:\GenerateurDOE\Production\Documents\PDF"
-- mkdir "C:\GenerateurDOE\Production\Documents\Images"
-- mkdir "C:\GenerateurDOE\Production\Logs"
--
-- Permissions nécessaires :
-- - Lecture/Écriture pour le compte de service IIS ou l'utilisateur de l'application
-- - Accès réseau si les répertoires sont sur un partage réseau

-- ===================================================================
-- ÉTAPE 4: Vérifications post-migration
-- ===================================================================

PRINT 'Vérifications post-migration...'

-- Vérification des tables principales
DECLARE @TableCount INT = 0

SELECT @TableCount = COUNT(*)
FROM sys.tables
WHERE name IN ('Chantiers', 'FichesTechniques', 'DocumentsGeneres', 'SectionsLibres',
               'TypesProduits', 'TypesDocuments', 'TypesSections')

IF @TableCount = 7
    PRINT '✓ Tables principales créées avec succès'
ELSE
    PRINT '⚠ Attention : Tables manquantes détectées'

-- Vérification des données initiales
DECLARE @DataCount INT = 0

SELECT @DataCount =
    (SELECT COUNT(*) FROM TypesProduits WHERE IsDefault = 1) +
    (SELECT COUNT(*) FROM TypesDocuments WHERE IsDefault = 1) +
    (SELECT COUNT(*) FROM TypesSections WHERE IsDefault = 1)

IF @DataCount >= 20
    PRINT '✓ Données par défaut insérées avec succès'
ELSE
    PRINT '⚠ Attention : Données par défaut incomplètes'

-- ===================================================================
-- ÉTAPE 5: Post-déploiement manuel
-- ===================================================================

PRINT ''
PRINT '====================================================================='
PRINT 'ACTIONS MANUELLES REQUISES APRÈS CE SCRIPT :'
PRINT '====================================================================='
PRINT '1. Créer les répertoires de stockage sur le serveur :'
PRINT '   - C:\GenerateurDOE\Production\Documents\PDF'
PRINT '   - C:\GenerateurDOE\Production\Documents\Images'
PRINT '   - C:\GenerateurDOE\Production\Logs'
PRINT ''
PRINT '2. Configurer les permissions d''accès aux répertoires'
PRINT ''
PRINT '3. Migrer les données existantes si nécessaire :'
PRINT '   - Fichiers PDF depuis le répertoire de développement'
PRINT '   - Images depuis le répertoire de développement'
PRINT '   - Données de la base LocalDB si conservation nécessaire'
PRINT ''
PRINT '4. Tester la connexion de l''application avec appsettings.Production.json'
PRINT ''
PRINT '5. Effectuer un test de génération PDF en production'
PRINT '====================================================================='

PRINT 'Migration terminée avec succès !'
PRINT 'Base de données GenerateurDOE_Prod prête pour la production'
GO