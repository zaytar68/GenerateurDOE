-- ============================================================================
-- Script de nettoyage et recréation des tables PostgreSQL - VERSION CORRIGÉE
-- Générateur DOE v2.1.3
-- Correction ordre de création et syntaxe + Connexion à la bonne base
-- ============================================================================

-- CONNEXION À LA BASE GenerateurDOE_Prod
-- ============================================================================
\c GenerateurDOE_Prod;

-- ÉTAPE 1: Suppression des tables dans le bon ordre (contraintes de clé étrangère)
-- ============================================================================

DROP TABLE IF EXISTS "FTElements" CASCADE;
DROP TABLE IF EXISTS "SectionConteneurItems" CASCADE;
DROP TABLE IF EXISTS "ImportsPDF" CASCADE;
DROP TABLE IF EXISTS "DocumentGenereFicheTechniques" CASCADE;
DROP TABLE IF EXISTS "SectionsConteneurs" CASCADE;
DROP TABLE IF EXISTS "FTConteneurs" CASCADE;
DROP TABLE IF EXISTS "SectionsLibres" CASCADE;
DROP TABLE IF EXISTS "FichesTechniques" CASCADE;
DROP TABLE IF EXISTS "DocumentsGeneres" CASCADE;
DROP TABLE IF EXISTS "ImagesMethode" CASCADE;
DROP TABLE IF EXISTS "TypesSections" CASCADE;
DROP TABLE IF EXISTS "TypesProduits" CASCADE;
DROP TABLE IF EXISTS "TypesDocuments" CASCADE;
DROP TABLE IF EXISTS "PageGardeTemplates" CASCADE;
DROP TABLE IF EXISTS "Methodes" CASCADE;
DROP TABLE IF EXISTS "Chantiers" CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- ÉTAPE 2: Recréation avec l'ORDRE CORRECT
-- ============================================================================

-- 1. Tables de base (sans dépendances)
CREATE TABLE "Chantiers" (
    "Id" SERIAL PRIMARY KEY,
    "NomProjet" VARCHAR(200) NOT NULL,
    "MaitreOeuvre" VARCHAR(200) NOT NULL,
    "MaitreOuvrage" VARCHAR(200) NOT NULL,
    "Adresse" VARCHAR(500) NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "EstArchive" BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE "Methodes" (
    "Id" SERIAL PRIMARY KEY,
    "Titre" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "OrdreAffichage" INTEGER NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "PageGardeTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "ContenuHtml" TEXT NOT NULL,
    "ContenuJson" TEXT NOT NULL,
    "EstParDefaut" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "TypesDocuments" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "TypesProduits" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "TypesSections" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2. Tables avec dépendances de niveau 1
CREATE TABLE "ImagesMethode" (
    "Id" SERIAL PRIMARY KEY,
    "CheminFichier" VARCHAR(500) NOT NULL,
    "NomFichierOriginal" VARCHAR(255) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "OrdreAffichage" INTEGER NOT NULL,
    "DateImport" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "MethodeId" INTEGER NOT NULL,
    CONSTRAINT "FK_ImagesMethode_Methodes_MethodeId"
        FOREIGN KEY ("MethodeId") REFERENCES "Methodes"("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentsGeneres" (
    "Id" SERIAL PRIMARY KEY,
    "TypeDocument" INTEGER NOT NULL,
    "FormatExport" INTEGER NOT NULL,
    "NomFichier" VARCHAR(255) NOT NULL,
    "CheminFichier" VARCHAR(500) NOT NULL,
    "Parametres" TEXT NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IncludePageDeGarde" BOOLEAN NOT NULL,
    "IncludeTableMatieres" BOOLEAN NOT NULL,
    "PageGardeTemplateId" INTEGER,
    "EnCours" BOOLEAN NOT NULL DEFAULT TRUE,
    "NumeroLot" VARCHAR(50) NOT NULL,
    "IntituleLot" VARCHAR(300) NOT NULL,
    "ChantierId" INTEGER NOT NULL,
    CONSTRAINT "FK_DocumentsGeneres_Chantiers_ChantierId"
        FOREIGN KEY ("ChantierId") REFERENCES "Chantiers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentsGeneres_PageGardeTemplates_PageGardeTemplateId"
        FOREIGN KEY ("PageGardeTemplateId") REFERENCES "PageGardeTemplates"("Id")
);

CREATE TABLE "FichesTechniques" (
    "Id" SERIAL PRIMARY KEY,
    "NomProduit" VARCHAR(200) NOT NULL,
    "NomFabricant" VARCHAR(200) NOT NULL,
    "TypeProduit" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(1000) NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ChantierId" INTEGER,
    "TypeProduitId" INTEGER,
    CONSTRAINT "FK_FichesTechniques_Chantiers_ChantierId"
        FOREIGN KEY ("ChantierId") REFERENCES "Chantiers"("Id"),
    CONSTRAINT "FK_FichesTechniques_TypesProduits_TypeProduitId"
        FOREIGN KEY ("TypeProduitId") REFERENCES "TypesProduits"("Id")
);

CREATE TABLE "SectionsLibres" (
    "Id" SERIAL PRIMARY KEY,
    "Titre" VARCHAR(200) NOT NULL,
    "Ordre" INTEGER NOT NULL,
    "ContenuHtml" TEXT NOT NULL,
    "ContenuJson" TEXT NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TypeSectionId" INTEGER NOT NULL,
    CONSTRAINT "FK_SectionsLibres_TypesSections_TypeSectionId"
        FOREIGN KEY ("TypeSectionId") REFERENCES "TypesSections"("Id") ON DELETE CASCADE
);

-- 3. Tables avec dépendances de niveau 2
CREATE TABLE "FTConteneurs" (
    "Id" SERIAL PRIMARY KEY,
    "Titre" VARCHAR(200) NOT NULL,
    "Ordre" INTEGER NOT NULL,
    "AfficherTableauRecapitulatif" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DocumentGenereId" INTEGER NOT NULL,
    CONSTRAINT "FK_FTConteneurs_DocumentsGeneres_DocumentGenereId"
        FOREIGN KEY ("DocumentGenereId") REFERENCES "DocumentsGeneres"("Id") ON DELETE CASCADE
);

CREATE TABLE "SectionsConteneurs" (
    "Id" SERIAL PRIMARY KEY,
    "Ordre" INTEGER NOT NULL,
    "Titre" VARCHAR(200) NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DocumentGenereId" INTEGER NOT NULL,
    "TypeSectionId" INTEGER NOT NULL,
    CONSTRAINT "FK_SectionsConteneurs_DocumentsGeneres_DocumentGenereId"
        FOREIGN KEY ("DocumentGenereId") REFERENCES "DocumentsGeneres"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SectionsConteneurs_TypesSections_TypeSectionId"
        FOREIGN KEY ("TypeSectionId") REFERENCES "TypesSections"("Id")
);

CREATE TABLE "DocumentGenereFicheTechniques" (
    "DocumentGenereId" INTEGER NOT NULL,
    "FicheTechniqueId" INTEGER NOT NULL,
    PRIMARY KEY ("DocumentGenereId", "FicheTechniqueId"),
    CONSTRAINT "FK_DocumentGenereFicheTechniques_DocumentsGeneres_DocumentGenereId"
        FOREIGN KEY ("DocumentGenereId") REFERENCES "DocumentsGeneres"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentGenereFicheTechniques_FichesTechniques_FicheTechniqueId"
        FOREIGN KEY ("FicheTechniqueId") REFERENCES "FichesTechniques"("Id") ON DELETE CASCADE
);

CREATE TABLE "ImportsPDF" (
    "Id" SERIAL PRIMARY KEY,
    "CheminFichier" VARCHAR(500) NOT NULL,
    "NomFichierOriginal" VARCHAR(255) NOT NULL,
    "TypeDocumentImportId" INTEGER NOT NULL,
    "TailleFichier" BIGINT NOT NULL,
    "DateImport" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PageCount" INTEGER,
    "FicheTechniqueId" INTEGER NOT NULL,
    CONSTRAINT "FK_ImportsPDF_FichesTechniques_FicheTechniqueId"
        FOREIGN KEY ("FicheTechniqueId") REFERENCES "FichesTechniques"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ImportsPDF_TypesDocuments_TypeDocumentImportId"
        FOREIGN KEY ("TypeDocumentImportId") REFERENCES "TypesDocuments"("Id")
);

-- 4. Tables avec dépendances de niveau 3
CREATE TABLE "SectionConteneurItems" (
    "Id" SERIAL PRIMARY KEY,
    "Ordre" INTEGER NOT NULL,
    "DateAjout" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "SectionConteneursId" INTEGER NOT NULL,
    "SectionLibreId" INTEGER NOT NULL,
    CONSTRAINT "FK_SectionConteneurItems_SectionsConteneurs_SectionConteneursId"
        FOREIGN KEY ("SectionConteneursId") REFERENCES "SectionsConteneurs"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SectionConteneurItems_SectionsLibres_SectionLibreId"
        FOREIGN KEY ("SectionLibreId") REFERENCES "SectionsLibres"("Id") ON DELETE CASCADE
);

CREATE TABLE "FTElements" (
    "Id" SERIAL PRIMARY KEY,
    "PositionMarche" VARCHAR(100) NOT NULL,
    "NumeroPage" INTEGER,
    "Ordre" INTEGER NOT NULL,
    "Commentaire" VARCHAR(500),
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "FTConteneursId" INTEGER NOT NULL,
    "FicheTechniqueId" INTEGER NOT NULL,
    "ImportPDFId" INTEGER,
    CONSTRAINT "FK_FTElements_FTConteneurs_FTConteneursId"
        FOREIGN KEY ("FTConteneursId") REFERENCES "FTConteneurs"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FTElements_FichesTechniques_FicheTechniqueId"
        FOREIGN KEY ("FicheTechniqueId") REFERENCES "FichesTechniques"("Id"),
    CONSTRAINT "FK_FTElements_ImportsPDF_ImportPDFId"
        FOREIGN KEY ("ImportPDFId") REFERENCES "ImportsPDF"("Id") ON DELETE SET NULL
);

-- ÉTAPE 3: Recréation des index
-- ============================================================================

CREATE INDEX "IX_DocumentGenereFicheTechniques_FicheTechniqueId"
    ON "DocumentGenereFicheTechniques" ("FicheTechniqueId");

CREATE INDEX "IX_DocumentsGeneres_ChantierId"
    ON "DocumentsGeneres" ("ChantierId");

CREATE INDEX "IX_DocumentsGeneres_PageGardeTemplateId"
    ON "DocumentsGeneres" ("PageGardeTemplateId");

CREATE INDEX "IX_FichesTechniques_ChantierId"
    ON "FichesTechniques" ("ChantierId");

CREATE INDEX "IX_FichesTechniques_TypeProduitId"
    ON "FichesTechniques" ("TypeProduitId");

CREATE UNIQUE INDEX "IX_FTConteneurs_DocumentGenereId"
    ON "FTConteneurs" ("DocumentGenereId");

CREATE INDEX "IX_FTElements_FicheTechniqueId"
    ON "FTElements" ("FicheTechniqueId");

CREATE INDEX "IX_FTElements_FTConteneursId"
    ON "FTElements" ("FTConteneursId");

CREATE INDEX "IX_FTElements_ImportPDFId"
    ON "FTElements" ("ImportPDFId");

CREATE INDEX "IX_ImagesMethode_MethodeId"
    ON "ImagesMethode" ("MethodeId");

CREATE INDEX "IX_ImportsPDF_FicheTechniqueId"
    ON "ImportsPDF" ("FicheTechniqueId");

CREATE INDEX "IX_ImportsPDF_TypeDocumentImportId"
    ON "ImportsPDF" ("TypeDocumentImportId");

CREATE UNIQUE INDEX "IX_PageGardeTemplates_Nom"
    ON "PageGardeTemplates" ("Nom");

CREATE UNIQUE INDEX "IX_SectionConteneurItems_SectionConteneursId_SectionLibreId"
    ON "SectionConteneurItems" ("SectionConteneursId", "SectionLibreId");

CREATE INDEX "IX_SectionConteneurItems_SectionLibreId"
    ON "SectionConteneurItems" ("SectionLibreId");

CREATE UNIQUE INDEX "IX_SectionConteneur_DocumentGenere_TypeSection"
    ON "SectionsConteneurs" ("DocumentGenereId", "TypeSectionId");

CREATE INDEX "IX_SectionsConteneurs_TypeSectionId"
    ON "SectionsConteneurs" ("TypeSectionId");

CREATE INDEX "IX_SectionsLibres_TypeSectionId"
    ON "SectionsLibres" ("TypeSectionId");

-- ÉTAPE 4: Insertion des données par défaut
-- ============================================================================

-- TypesProduits par défaut
INSERT INTO "TypesProduits" ("Nom", "Description") VALUES
('Isolation thermique', 'Matériaux d''isolation thermique et phonique'),
('Plomberie', 'Équipements et matériaux de plomberie'),
('Électricité', 'Matériels et équipements électriques'),
('Chauffage', 'Systèmes de chauffage et climatisation'),
('Menuiserie', 'Portes, fenêtres et éléments de menuiserie'),
('Carrelage', 'Revêtements de sols et murs'),
('Peinture', 'Produits de peinture et finition'),
('Étanchéité', 'Produits d''étanchéité et imperméabilisation');

-- TypesDocuments par défaut
INSERT INTO "TypesDocuments" ("Nom", "Description") VALUES
('Fiche technique', 'Fiche technique produit standard'),
('Notice de pose', 'Instructions d''installation et de pose'),
('Certificat', 'Certificats et homologations'),
('Garantie', 'Documents de garantie constructeur'),
('Avis technique', 'Avis techniques officiels'),
('PV d''essai', 'Procès-verbaux d''essais et tests');

-- TypesSections par défaut
INSERT INTO "TypesSections" ("Nom", "Description") VALUES
('Introduction', 'Section d''introduction du document'),
('Présentation société', 'Présentation de l''entreprise'),
('Méthodologie', 'Description des méthodes de travail'),
('Matériaux', 'Description des matériaux utilisés'),
('Planning', 'Planning d''exécution des travaux'),
('Sécurité', 'Mesures de sécurité et prévention'),
('Qualité', 'Contrôles qualité et certifications'),
('Conclusion', 'Conclusion du document'),
('Annexes', 'Documents annexes et compléments');

-- Template de page de garde par défaut
INSERT INTO "PageGardeTemplates" ("Nom", "Description", "ContenuHtml", "ContenuJson", "EstParDefaut") VALUES
('Standard', 'Template de page de garde standard',
'<div class="page-garde"><h1>{{TitreDocument}}</h1><h2>{{NomProjet}}</h2><p>{{MaitreOeuvre}}</p><p>{{Adresse}}</p></div>',
'{"TitreDocument": "Document Technique", "NomProjet": "", "MaitreOeuvre": "", "Adresse": ""}',
true);

-- ÉTAPE 5: Recréation de la table EF Migrations
-- ============================================================================

CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20250926093700_InitialMigration', '8.0.8');

-- ÉTAPE 6: Vérification finale
-- ============================================================================

SELECT 'Recréation terminée avec succès!' as status;

SELECT
    tablename as "Table Name",
    schemaname as "Schema"
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;

SELECT
    COUNT(*) as "Total Tables Created"
FROM pg_tables
WHERE schemaname = 'public';