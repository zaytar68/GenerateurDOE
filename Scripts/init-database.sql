-- ============================================================================
-- Script d'initialisation de la base de données PostgreSQL
-- Générateur DOE v2.1.3
-- À exécuter dans pgAdmin sur la base de données: GenerateurDOE_Prod
-- ============================================================================

-- Vérification de la base de données
SELECT current_database();

-- ============================================================================
-- CRÉATION DES TABLES PRINCIPALES
-- ============================================================================

-- Table: Chantiers
CREATE TABLE IF NOT EXISTS "Chantiers" (
    "Id" SERIAL PRIMARY KEY,
    "NomProjet" VARCHAR(200) NOT NULL,
    "MaitreOeuvre" VARCHAR(200) NOT NULL,
    "MaitreOuvrage" VARCHAR(200) NOT NULL,
    "Adresse" VARCHAR(500) NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "EstArchive" BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table: Methodes
CREATE TABLE IF NOT EXISTS "Methodes" (
    "Id" SERIAL PRIMARY KEY,
    "Titre" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "OrdreAffichage" INTEGER NOT NULL,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table: PageGardeTemplates
CREATE TABLE IF NOT EXISTS "PageGardeTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "ContenuHtml" TEXT NOT NULL,
    "ContenuJson" TEXT NOT NULL,
    "EstParDefaut" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table: TypesDocuments
CREATE TABLE IF NOT EXISTS "TypesDocuments" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table: TypesProduits
CREATE TABLE IF NOT EXISTS "TypesProduits" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table: TypesSections
CREATE TABLE IF NOT EXISTS "TypesSections" (
    "Id" SERIAL PRIMARY KEY,
    "Nom" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "DateCreation" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateModification" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- TABLES DE LIAISON ET RELATIONS
-- ============================================================================

-- Table: ImagesMethode
CREATE TABLE IF NOT EXISTS "ImagesMethode" (
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

-- Table: DocumentsGeneres
CREATE TABLE IF NOT EXISTS "DocumentsGeneres" (
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

-- Table: FichesTechniques
CREATE TABLE IF NOT EXISTS "FichesTechniques" (
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

-- Table: SectionsLibres
CREATE TABLE IF NOT EXISTS "SectionsLibres" (
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

-- Table: FTConteneurs
CREATE TABLE IF NOT EXISTS "FTConteneurs" (
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

-- Table: SectionsConteneurs
CREATE TABLE IF NOT EXISTS "SectionsConteneurs" (
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

-- Table: DocumentGenereFicheTechniques (many-to-many)
CREATE TABLE IF NOT EXISTS "DocumentGenereFicheTechniques" (
    "DocumentGenereId" INTEGER NOT NULL,
    "FicheTechniqueId" INTEGER NOT NULL,
    PRIMARY KEY ("DocumentGenereId", "FicheTechniqueId"),
    CONSTRAINT "FK_DocumentGenereFicheTechniques_DocumentsGeneres_DocumentGenereId"
        FOREIGN KEY ("DocumentGenereId") REFERENCES "DocumentsGeneres"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DocumentGenereFicheTechniques_FichesTechniques_FicheTechniqueId"
        FOREIGN KEY ("FicheTechniqueId") REFERENCES "FichesTechniques"("Id") ON DELETE CASCADE
);

-- Table: ImportsPDF
CREATE TABLE IF NOT EXISTS "ImportsPDF" (
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

-- Table: SectionConteneurItems
CREATE TABLE IF NOT EXISTS "SectionConteneurItems" (
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

-- Table: FTElements
CREATE TABLE IF NOT EXISTS "FTElements" (
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

-- ============================================================================
-- CRÉATION DES INDEX
-- ============================================================================

-- Index pour les performances
CREATE INDEX IF NOT EXISTS "IX_DocumentGenereFicheTechniques_FicheTechniqueId"
    ON "DocumentGenereFicheTechniques" ("FicheTechniqueId");

CREATE INDEX IF NOT EXISTS "IX_DocumentsGeneres_ChantierId"
    ON "DocumentsGeneres" ("ChantierId");

CREATE INDEX IF NOT EXISTS "IX_DocumentsGeneres_PageGardeTemplateId"
    ON "DocumentsGeneres" ("PageGardeTemplateId");

CREATE INDEX IF NOT EXISTS "IX_FichesTechniques_ChantierId"
    ON "FichesTechniques" ("ChantierId");

CREATE INDEX IF NOT EXISTS "IX_FichesTechniques_TypeProduitId"
    ON "FichesTechniques" ("TypeProduitId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FTConteneurs_DocumentGenereId"
    ON "FTConteneurs" ("DocumentGenereId");

CREATE INDEX IF NOT EXISTS "IX_FTElements_FicheTechniqueId"
    ON "FTElements" ("FicheTechniqueId");

CREATE INDEX IF NOT EXISTS "IX_FTElements_FTConteneursId"
    ON "FTElements" ("FTConteneursId");

CREATE INDEX IF NOT EXISTS "IX_FTElements_ImportPDFId"
    ON "FTElements" ("ImportPDFId");

CREATE INDEX IF NOT EXISTS "IX_ImagesMethode_MethodeId"
    ON "ImagesMethode" ("MethodeId");

CREATE INDEX IF NOT EXISTS "IX_ImportsPDF_FicheTechniqueId"
    ON "ImportsPDF" ("FicheTechniqueId");

CREATE INDEX IF NOT EXISTS "IX_ImportsPDF_TypeDocumentImportId"
    ON "ImportsPDF" ("TypeDocumentImportId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PageGardeTemplates_Nom"
    ON "PageGardeTemplates" ("Nom");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectionConteneurItems_SectionConteneursId_SectionLibreId"
    ON "SectionConteneurItems" ("SectionConteneursId", "SectionLibreId");

CREATE INDEX IF NOT EXISTS "IX_SectionConteneurItems_SectionLibreId"
    ON "SectionConteneurItems" ("SectionLibreId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_SectionConteneur_DocumentGenere_TypeSection"
    ON "SectionsConteneurs" ("DocumentGenereId", "TypeSectionId");

CREATE INDEX IF NOT EXISTS "IX_SectionsConteneurs_TypeSectionId"
    ON "SectionsConteneurs" ("TypeSectionId");

CREATE INDEX IF NOT EXISTS "IX_SectionsLibres_TypeSectionId"
    ON "SectionsLibres" ("TypeSectionId");

-- ============================================================================
-- INSERTION DES DONNÉES PAR DÉFAUT
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
('Étanchéité', 'Produits d''étanchéité et imperméabilisation')
ON CONFLICT DO NOTHING;

-- TypesDocuments par défaut
INSERT INTO "TypesDocuments" ("Nom", "Description") VALUES
('Fiche technique', 'Fiche technique produit standard'),
('Notice de pose', 'Instructions d''installation et de pose'),
('Certificat', 'Certificats et homologations'),
('Garantie', 'Documents de garantie constructeur'),
('Avis technique', 'Avis techniques officiels'),
('PV d''essai', 'Procès-verbaux d''essais et tests')
ON CONFLICT DO NOTHING;

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
('Annexes', 'Documents annexes et compléments')
ON CONFLICT DO NOTHING;

-- Template de page de garde par défaut
INSERT INTO "PageGardeTemplates" ("Nom", "Description", "ContenuHtml", "ContenuJson", "EstParDefaut") VALUES
('Standard', 'Template de page de garde standard',
'<div class="page-garde"><h1>{{TitreDocument}}</h1><h2>{{NomProjet}}</h2><p>{{MaitreOeuvre}}</p><p>{{Adresse}}</p></div>',
'{"TitreDocument": "Document Technique", "NomProjet": "", "MaitreOeuvre": "", "Adresse": ""}',
true)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- CRÉATION DE LA TABLE __EFMigrationsHistory
-- ============================================================================

-- Table de suivi des migrations Entity Framework
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

-- Insertion de la migration consolidée
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20250926093700_InitialMigration', '8.0.8')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- VÉRIFICATION DES TABLES CRÉÉES
-- ============================================================================

-- Affichage des tables créées
SELECT
    schemaname,
    tablename,
    tableowner
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;

-- Compte des enregistrements dans les tables de référence
SELECT
    'TypesProduits' as table_name, COUNT(*) as count FROM "TypesProduits"
UNION ALL
SELECT
    'TypesDocuments', COUNT(*) FROM "TypesDocuments"
UNION ALL
SELECT
    'TypesSections', COUNT(*) FROM "TypesSections"
UNION ALL
SELECT
    'PageGardeTemplates', COUNT(*) FROM "PageGardeTemplates";

-- ============================================================================
-- SCRIPT TERMINÉ AVEC SUCCÈS
-- ============================================================================

SELECT 'Base de données initialisée avec succès!' as status;