CREATE TABLE [Chantiers] (
    [Id] int NOT NULL IDENTITY,
    [NomProjet] nvarchar(200) NOT NULL,
    [MaitreOeuvre] nvarchar(200) NOT NULL,
    [MaitreOuvrage] nvarchar(200) NOT NULL,
    [Adresse] nvarchar(500) NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    [EstArchive] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_Chantiers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Methodes] (
    [Id] int NOT NULL IDENTITY,
    [Titre] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [OrdreAffichage] int NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Methodes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PageGardeTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ContenuHtml] nvarchar(max) NOT NULL,
    [ContenuJson] nvarchar(max) NOT NULL,
    [EstParDefaut] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_PageGardeTemplates] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TypesDocuments] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_TypesDocuments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TypesProduits] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_TypesProduits] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TypesSections] (
    [Id] int NOT NULL IDENTITY,
    [Nom] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_TypesSections] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ImagesMethode] (
    [Id] int NOT NULL IDENTITY,
    [CheminFichier] nvarchar(500) NOT NULL,
    [NomFichierOriginal] nvarchar(255) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [OrdreAffichage] int NOT NULL,
    [DateImport] datetime2 NOT NULL DEFAULT (GETDATE()),
    [MethodeId] int NOT NULL,
    CONSTRAINT [PK_ImagesMethode] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImagesMethode_Methodes_MethodeId] FOREIGN KEY ([MethodeId]) REFERENCES [Methodes] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [DocumentsGeneres] (
    [Id] int NOT NULL IDENTITY,
    [TypeDocument] int NOT NULL,
    [FormatExport] int NOT NULL,
    [NomFichier] nvarchar(255) NOT NULL,
    [CheminFichier] nvarchar(500) NOT NULL,
    [Parametres] nvarchar(max) NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IncludePageDeGarde] bit NOT NULL,
    [IncludeTableMatieres] bit NOT NULL,
    [PageGardeTemplateId] int NULL,
    [EnCours] bit NOT NULL DEFAULT CAST(1 AS bit),
    [NumeroLot] nvarchar(50) NOT NULL,
    [IntituleLot] nvarchar(300) NOT NULL,
    [ChantierId] int NOT NULL,
    CONSTRAINT [PK_DocumentsGeneres] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentsGeneres_Chantiers_ChantierId] FOREIGN KEY ([ChantierId]) REFERENCES [Chantiers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DocumentsGeneres_PageGardeTemplates_PageGardeTemplateId] FOREIGN KEY ([PageGardeTemplateId]) REFERENCES [PageGardeTemplates] ([Id])
);
GO


CREATE TABLE [FichesTechniques] (
    [Id] int NOT NULL IDENTITY,
    [NomProduit] nvarchar(200) NOT NULL,
    [NomFabricant] nvarchar(200) NOT NULL,
    [TypeProduit] nvarchar(100) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    [ChantierId] int NULL,
    [TypeProduitId] int NULL,
    CONSTRAINT [PK_FichesTechniques] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FichesTechniques_Chantiers_ChantierId] FOREIGN KEY ([ChantierId]) REFERENCES [Chantiers] ([Id]),
    CONSTRAINT [FK_FichesTechniques_TypesProduits_TypeProduitId] FOREIGN KEY ([TypeProduitId]) REFERENCES [TypesProduits] ([Id])
);
GO


CREATE TABLE [SectionsLibres] (
    [Id] int NOT NULL IDENTITY,
    [Titre] nvarchar(200) NOT NULL,
    [Ordre] int NOT NULL,
    [ContenuHtml] nvarchar(max) NOT NULL,
    [ContenuJson] nvarchar(max) NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    [TypeSectionId] int NOT NULL,
    CONSTRAINT [PK_SectionsLibres] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SectionsLibres_TypesSections_TypeSectionId] FOREIGN KEY ([TypeSectionId]) REFERENCES [TypesSections] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [FTConteneurs] (
    [Id] int NOT NULL IDENTITY,
    [Titre] nvarchar(200) NOT NULL,
    [Ordre] int NOT NULL,
    [AfficherTableauRecapitulatif] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DocumentGenereId] int NOT NULL,
    CONSTRAINT [PK_FTConteneurs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FTConteneurs_DocumentsGeneres_DocumentGenereId] FOREIGN KEY ([DocumentGenereId]) REFERENCES [DocumentsGeneres] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SectionsConteneurs] (
    [Id] int NOT NULL IDENTITY,
    [Ordre] int NOT NULL,
    [Titre] nvarchar(200) NOT NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DateModification] datetime2 NOT NULL DEFAULT (GETDATE()),
    [DocumentGenereId] int NOT NULL,
    [TypeSectionId] int NOT NULL,
    CONSTRAINT [PK_SectionsConteneurs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SectionsConteneurs_DocumentsGeneres_DocumentGenereId] FOREIGN KEY ([DocumentGenereId]) REFERENCES [DocumentsGeneres] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SectionsConteneurs_TypesSections_TypeSectionId] FOREIGN KEY ([TypeSectionId]) REFERENCES [TypesSections] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DocumentGenereFicheTechniques] (
    [DocumentGenereId] int NOT NULL,
    [FicheTechniqueId] int NOT NULL,
    CONSTRAINT [PK_DocumentGenereFicheTechniques] PRIMARY KEY ([DocumentGenereId], [FicheTechniqueId]),
    CONSTRAINT [FK_DocumentGenereFicheTechniques_DocumentsGeneres_DocumentGenereId] FOREIGN KEY ([DocumentGenereId]) REFERENCES [DocumentsGeneres] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DocumentGenereFicheTechniques_FichesTechniques_FicheTechniqueId] FOREIGN KEY ([FicheTechniqueId]) REFERENCES [FichesTechniques] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ImportsPDF] (
    [Id] int NOT NULL IDENTITY,
    [CheminFichier] nvarchar(500) NOT NULL,
    [NomFichierOriginal] nvarchar(255) NOT NULL,
    [TypeDocumentImportId] int NOT NULL,
    [TailleFichier] bigint NOT NULL,
    [DateImport] datetime2 NOT NULL DEFAULT (GETDATE()),
    [PageCount] int NULL,
    [FicheTechniqueId] int NOT NULL,
    CONSTRAINT [PK_ImportsPDF] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImportsPDF_FichesTechniques_FicheTechniqueId] FOREIGN KEY ([FicheTechniqueId]) REFERENCES [FichesTechniques] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ImportsPDF_TypesDocuments_TypeDocumentImportId] FOREIGN KEY ([TypeDocumentImportId]) REFERENCES [TypesDocuments] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SectionConteneurItems] (
    [Id] int NOT NULL IDENTITY,
    [Ordre] int NOT NULL,
    [DateAjout] datetime2 NOT NULL DEFAULT (GETDATE()),
    [SectionConteneursId] int NOT NULL,
    [SectionLibreId] int NOT NULL,
    CONSTRAINT [PK_SectionConteneurItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SectionConteneurItems_SectionsConteneurs_SectionConteneursId] FOREIGN KEY ([SectionConteneursId]) REFERENCES [SectionsConteneurs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SectionConteneurItems_SectionsLibres_SectionLibreId] FOREIGN KEY ([SectionLibreId]) REFERENCES [SectionsLibres] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [FTElements] (
    [Id] int NOT NULL IDENTITY,
    [PositionMarche] nvarchar(100) NOT NULL,
    [NumeroPage] int NULL,
    [Ordre] int NOT NULL,
    [Commentaire] nvarchar(500) NULL,
    [DateCreation] datetime2 NOT NULL DEFAULT (GETDATE()),
    [FTConteneursId] int NOT NULL,
    [FicheTechniqueId] int NOT NULL,
    [ImportPDFId] int NULL,
    CONSTRAINT [PK_FTElements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FTElements_FTConteneurs_FTConteneursId] FOREIGN KEY ([FTConteneursId]) REFERENCES [FTConteneurs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FTElements_FichesTechniques_FicheTechniqueId] FOREIGN KEY ([FicheTechniqueId]) REFERENCES [FichesTechniques] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FTElements_ImportsPDF_ImportPDFId] FOREIGN KEY ([ImportPDFId]) REFERENCES [ImportsPDF] ([Id]) ON DELETE SET NULL
);
GO


CREATE INDEX [IX_DocumentGenereFicheTechniques_FicheTechniqueId] ON [DocumentGenereFicheTechniques] ([FicheTechniqueId]);
GO


CREATE INDEX [IX_DocumentsGeneres_ChantierId] ON [DocumentsGeneres] ([ChantierId]);
GO


CREATE INDEX [IX_DocumentsGeneres_PageGardeTemplateId] ON [DocumentsGeneres] ([PageGardeTemplateId]);
GO


CREATE INDEX [IX_FichesTechniques_ChantierId] ON [FichesTechniques] ([ChantierId]);
GO


CREATE INDEX [IX_FichesTechniques_TypeProduitId] ON [FichesTechniques] ([TypeProduitId]);
GO


CREATE UNIQUE INDEX [IX_FTConteneurs_DocumentGenereId] ON [FTConteneurs] ([DocumentGenereId]);
GO


CREATE INDEX [IX_FTElements_FicheTechniqueId] ON [FTElements] ([FicheTechniqueId]);
GO


CREATE INDEX [IX_FTElements_FTConteneursId] ON [FTElements] ([FTConteneursId]);
GO


CREATE INDEX [IX_FTElements_ImportPDFId] ON [FTElements] ([ImportPDFId]);
GO


CREATE INDEX [IX_ImagesMethode_MethodeId] ON [ImagesMethode] ([MethodeId]);
GO


CREATE INDEX [IX_ImportsPDF_FicheTechniqueId] ON [ImportsPDF] ([FicheTechniqueId]);
GO


CREATE INDEX [IX_ImportsPDF_TypeDocumentImportId] ON [ImportsPDF] ([TypeDocumentImportId]);
GO


CREATE UNIQUE INDEX [IX_PageGardeTemplates_Nom] ON [PageGardeTemplates] ([Nom]);
GO


CREATE UNIQUE INDEX [IX_SectionConteneurItems_SectionConteneursId_SectionLibreId] ON [SectionConteneurItems] ([SectionConteneursId], [SectionLibreId]);
GO


CREATE INDEX [IX_SectionConteneurItems_SectionLibreId] ON [SectionConteneurItems] ([SectionLibreId]);
GO


CREATE UNIQUE INDEX [IX_SectionConteneur_DocumentGenere_TypeSection] ON [SectionsConteneurs] ([DocumentGenereId], [TypeSectionId]);
GO


CREATE INDEX [IX_SectionsConteneurs_TypeSectionId] ON [SectionsConteneurs] ([TypeSectionId]);
GO


CREATE INDEX [IX_SectionsLibres_TypeSectionId] ON [SectionsLibres] ([TypeSectionId]);
GO


