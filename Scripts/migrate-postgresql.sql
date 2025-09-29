-- ===================================================================
-- Script de Migration PostgreSQL - Générateur DOE v2.1.3
-- ===================================================================
--
-- Ce script permet de configurer PostgreSQL pour l'application
-- Générateur DOE avec les bonnes pratiques PostgreSQL
--
-- ÉTAPES REQUISES AVANT L'EXÉCUTION :
-- 1. PostgreSQL installé et fonctionnel
-- 2. Création de l'utilisateur et de la base de données
-- 3. Configuration des permissions appropriées
-- 4. Test de connectivité
--
-- ===================================================================

-- Connexion en tant que superutilisateur postgres
-- psql -U postgres -h localhost

-- ===================================================================
-- ÉTAPE 1: Création de l'utilisateur et de la base de données
-- ===================================================================

-- Créer l'utilisateur dédié
CREATE USER generateur_user WITH ENCRYPTED PASSWORD 'GenerateurDOE2025!';

-- Créer la base de données
CREATE DATABASE "GenerateurDOE_Prod"
    WITH OWNER = generateur_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'fr_FR.UTF-8'
    LC_CTYPE = 'fr_FR.UTF-8'
    TEMPLATE = template0;

-- Accorder les privilèges
GRANT ALL PRIVILEGES ON DATABASE "GenerateurDOE_Prod" TO generateur_user;

-- Connexion à la base de données cible
\c GenerateurDOE_Prod generateur_user

-- Accorder les privilèges sur le schéma public
GRANT ALL ON SCHEMA public TO generateur_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO generateur_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO generateur_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO generateur_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO generateur_user;

-- ===================================================================
-- ÉTAPE 2: Application des migrations Entity Framework
-- ===================================================================
--
-- IMPORTANT : Exécuter ces commandes depuis le répertoire de l'application :
--
-- # Configuration pour PostgreSQL
-- $env:DatabaseProvider = "PostgreSQL"
-- $env:ASPNETCORE_ENVIRONMENT = "PostgreSQL"
--
-- # Application des migrations
-- dotnet ef database update --environment PostgreSQL
--
-- Cette commande appliquera automatiquement toutes les migrations EF Core
-- pour créer la structure complète de la base de données PostgreSQL
-- ===================================================================

-- ===================================================================
-- ÉTAPE 3: Optimisations PostgreSQL spécifiques
-- ===================================================================

-- Optimisation des performances pour les chaînes de caractères
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Index pour les recherches de texte (si nécessaire)
-- CREATE INDEX IF NOT EXISTS idx_fiches_nom_gin ON "FichesTechniques" USING gin(to_tsvector('french', "Nom"));
-- CREATE INDEX IF NOT EXISTS idx_chantiers_nom_gin ON "Chantiers" USING gin(to_tsvector('french', "Nom"));

-- Configuration des timeouts
ALTER DATABASE "GenerateurDOE_Prod" SET statement_timeout = '300s';
ALTER DATABASE "GenerateurDOE_Prod" SET lock_timeout = '30s';

-- ===================================================================
-- ÉTAPE 4: Données initiales et configuration
-- ===================================================================

-- Note : Les données initiales seront insérées automatiquement
-- par l'application au démarrage via les services d'initialisation

-- ===================================================================
-- ÉTAPE 5: Vérifications post-migration
-- ===================================================================

-- Vérifier la connexion utilisateur
SELECT current_user, current_database();

-- Vérifier les extensions installées
SELECT name, default_version, installed_version
FROM pg_available_extensions
WHERE name IN ('uuid-ossp', 'pg_trgm');

-- Vérifier les privilèges
SELECT grantee, privilege_type
FROM information_schema.role_table_grants
WHERE grantee = 'generateur_user';

-- ===================================================================
-- ÉTAPE 6: Configuration de sauvegarde (Optionnel)
-- ===================================================================

-- Script de sauvegarde automatique (à adapter selon l'environnement)
-- pg_dump -h localhost -U generateur_user -d GenerateurDOE_Prod > backup_$(date +%Y%m%d_%H%M%S).sql

-- ===================================================================
-- ÉTAPE 7: Post-déploiement manuel
-- ===================================================================

-- Affichage des informations importantes
SELECT 'PostgreSQL Configuration Summary' as info;
SELECT 'Database: GenerateurDOE_Prod' as database_info;
SELECT 'User: generateur_user' as user_info;
SELECT 'Encoding: UTF8' as encoding_info;
SELECT version() as postgresql_version;

-- ===================================================================
-- ACTIONS MANUELLES REQUISES APRÈS CE SCRIPT :
-- ===================================================================
--
-- 1. Configurer pg_hba.conf pour les connexions :
--    # Ajouter cette ligne pour l'authentification locale
--    local   GenerateurDOE_Prod   generateur_user   md5
--    host    GenerateurDOE_Prod   generateur_user   127.0.0.1/32   md5
--
-- 2. Redémarrer PostgreSQL si nécessaire :
--    sudo systemctl restart postgresql
--
-- 3. Tester la connexion :
--    psql -h localhost -U generateur_user -d GenerateurDOE_Prod
--
-- 4. Configurer les sauvegardes automatiques
--
-- 5. Ajuster postgresql.conf pour la production :
--    - max_connections = 100
--    - shared_buffers = 256MB
--    - effective_cache_size = 1GB
--    - work_mem = 4MB
--    - maintenance_work_mem = 64MB
--
-- ===================================================================