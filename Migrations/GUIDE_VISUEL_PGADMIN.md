# 🖼️ Guide Visuel pgAdmin - Application de la Migration

**Date**: 2025-10-28
**Fichier à exécuter**: `AddPersonnalisationColumns_PostgreSQL.sql`

---

## 📸 Captures d'Écran et Instructions Pas-à-Pas

### 🔹 Étape 1: Navigation dans pgAdmin

```
┌─ pgAdmin 4 ──────────────────────────────────────┐
│                                                   │
│  Servers                                          │
│  └─ PostgreSQL 13                                 │
│     ├─ Login/Group Roles                          │
│     ├─ Tablespaces                                │
│     └─ Databases                                  │
│        ├─ postgres                                │
│        └─ GenerateurDOE_Prod  ◄─── CLIC DROIT ICI│
│           ├─ Casts                                │
│           ├─ Catalogs                             │
│           ├─ Event Triggers                       │
│           ├─ Extensions                           │
│           ├─ Foreign Data Wrappers                │
│           ├─ Languages                            │
│           ├─ Schemas                              │
│           │  └─ public                            │
│           │     └─ Tables                         │
│           │        └─ SectionConteneurItems       │
│           └─ ...                                  │
└───────────────────────────────────────────────────┘
```

**Action**: Clic droit sur `GenerateurDOE_Prod` → Sélectionner **"Query Tool"**

---

### 🔹 Étape 2: Fenêtre Query Tool

```
┌─ Query Tool - GenerateurDOE_Prod ────────────────────────────────┐
│ File  Edit  View  Query  Debugger  Tools  Help                   │
│                                                                   │
│ [📁 Open] [💾 Save] [▶️ Execute] [🛑 Stop] [📋 Explain]          │
│                                                                   │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ -- Script de migration pour PostgreSQL                     │ │
│ │ -- Ajout des colonnes de personnalisation                  │ │
│ │                                                             │ │
│ │ DO $$                                                       │ │
│ │ BEGIN                                                       │ │
│ │     IF NOT EXISTS (SELECT 1 FROM information_schema.tables │ │
│ │                    WHERE table_name = 'SectionConteneurItems'│ │
│ │         ...                                                 │ │
│ │                                         ◄─── COLLER LE SCRIPT│ │
│ │                                                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│ ┌─ Messages ─────────────────────────────────────────────────┐  │
│ │                                                             │  │
│ └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│ ┌─ Data Output ──────────────────────────────────────────────┐  │
│ │                                                             │  │
│ └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

**Action**:
1. Coller le contenu complet de `AddPersonnalisationColumns_PostgreSQL.sql`
2. Cliquer sur le bouton **▶️ Execute** (ou `F5`)

---

### 🔹 Étape 3: Résultat - Messages

```
┌─ Messages ────────────────────────────────────────────────────────┐
│                                                                    │
│ ✅ Query returned successfully in 142 msec.                       │
│                                                                    │
│ NOTICE:  Colonne TitrePersonnalise ajoutée avec succès            │
│ NOTICE:  Colonne ContenuHtmlPersonnalise ajoutée avec succès      │
│ NOTICE:  Colonne DateModificationPersonnalisation ajoutée avec    │
│          succès                                                    │
│                                                                    │
│ NOTICE:  3 rows affected.                                         │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

**Signification**:
- ✅ **3 NOTICE affichés** → Les 3 colonnes ont été ajoutées avec succès
- ✅ **3 rows affected** → La requête de vérification a retourné 3 colonnes

---

### 🔹 Étape 4: Résultat - Data Output

```
┌─ Data Output ──────────────────────────────────────────────────────┐
│                                                                     │
│ Colonne                          │ Type           │ Longueur Max │ │
│──────────────────────────────────┼────────────────┼──────────────┤ │
│ ContenuHtmlPersonnalise          │ text           │ NULL         │ │
│ DateModificationPersonnalisation │ timestamp      │ NULL         │ │
│ TitrePersonnalise                │ character...   │ 200          │ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Signification**:
- ✅ **3 lignes affichées** → Les colonnes existent maintenant dans la table
- ✅ **Types corrects** → TEXT, TIMESTAMP, VARCHAR(200)
- ✅ **Nullable = YES** → Compatibilité avec données existantes

---

## 🎯 Vérification Alternative: Inspection de la Table

### Méthode 1: Via l'Arborescence pgAdmin

```
Schemas
└─ public
   └─ Tables
      └─ SectionConteneurItems  ◄─── CLIC DROIT ICI
         └─ Columns              ◄─── CLIQUER ICI
            ├─ Id (integer)
            ├─ Ordre (integer)
            ├─ DateAjout (timestamp)
            ├─ SectionConteneursId (integer)
            ├─ SectionLibreId (integer)
            ├─ TitrePersonnalise (character varying 200)        🆕
            ├─ ContenuHtmlPersonnalise (text)                   🆕
            └─ DateModificationPersonnalisation (timestamp)     🆕
```

**Action**: Vérifier visuellement que les 3 colonnes marquées 🆕 apparaissent

---

### Méthode 2: Requête de Vérification Manuelle

Exécutez cette requête dans Query Tool :
```sql
SELECT
    column_name AS "Colonne",
    data_type AS "Type",
    character_maximum_length AS "Longueur Max",
    is_nullable AS "Nullable",
    column_default AS "Valeur Par Défaut"
FROM information_schema.columns
WHERE table_name = 'SectionConteneurItems'
ORDER BY ordinal_position;
```

**Résultat Attendu** (colonnes 1-7 existantes + 3 nouvelles) :
```
Colonne                          | Type                        | Longueur | Nullable | Défaut
─────────────────────────────────┼─────────────────────────────┼──────────┼──────────┼────────
Id                               | integer                     | NULL     | NO       | nextval...
Ordre                            | integer                     | NULL     | NO       | NULL
DateAjout                        | timestamp without time zone | NULL     | NO       | CURRENT...
SectionConteneursId              | integer                     | NULL     | NO       | NULL
SectionLibreId                   | integer                     | NULL     | NO       | NULL
TitrePersonnalise                | character varying           | 200      | YES      | NULL     🆕
ContenuHtmlPersonnalise          | text                        | NULL     | YES      | NULL     🆕
DateModificationPersonnalisation | timestamp without time zone | NULL     | YES      | NULL     🆕
```

---

## 🔍 Vérification des Contraintes (Optionnel)

### Requête pour Vérifier les Foreign Keys

```sql
SELECT
    tc.constraint_name AS "Nom Contrainte",
    tc.table_name AS "Table Source",
    kcu.column_name AS "Colonne Source",
    ccu.table_name AS "Table Référencée",
    ccu.column_name AS "Colonne Référencée"
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name = 'SectionConteneurItems'
  AND tc.constraint_type = 'FOREIGN KEY';
```

**Résultat Attendu** (2 foreign keys) :
```
Nom Contrainte                                       | Table Source           | Colonne Source       | Table Référencée      | Colonne Référencée
─────────────────────────────────────────────────────┼────────────────────────┼──────────────────────┼───────────────────────┼────────────────────
FK_SectionConteneurItems_SectionsConteneurs_...     | SectionConteneurItems  | SectionConteneursId  | SectionsConteneurs    | Id
FK_SectionConteneurItems_SectionsLibres_...         | SectionConteneurItems  | SectionLibreId       | SectionsLibres        | Id
```

---

## 🧪 Test Fonctionnel Final

### Test 1: Insérer une Ligne de Test

```sql
-- Supposons qu'il existe un SectionConteneur avec Id=1 et une SectionLibre avec Id=1
INSERT INTO "SectionConteneurItems" (
    "Ordre",
    "DateAjout",
    "SectionConteneursId",
    "SectionLibreId",
    "TitrePersonnalise",
    "ContenuHtmlPersonnalise",
    "DateModificationPersonnalisation"
) VALUES (
    1,
    CURRENT_TIMESTAMP,
    1,  -- Remplacer par un ID valide de SectionsConteneurs
    1,  -- Remplacer par un ID valide de SectionsLibres
    'Titre Personnalisé Test',
    '<p>Contenu HTML personnalisé</p>',
    CURRENT_TIMESTAMP
);

-- Vérifier l'insertion
SELECT * FROM "SectionConteneurItems" WHERE "TitrePersonnalise" = 'Titre Personnalisé Test';
```

**Résultat Attendu**:
- ✅ Insertion réussie sans erreur
- ✅ La ligne apparaît avec toutes les valeurs correctement enregistrées

### Test 2: Supprimer la Ligne de Test

```sql
DELETE FROM "SectionConteneurItems" WHERE "TitrePersonnalise" = 'Titre Personnalisé Test';
```

---

## 📊 Statistiques Post-Migration

### Taille de la Table Avant/Après

```sql
SELECT
    pg_size_pretty(pg_total_relation_size('"SectionConteneurItems"')) AS "Taille Totale",
    pg_size_pretty(pg_relation_size('"SectionConteneurItems"')) AS "Taille Table",
    pg_size_pretty(pg_indexes_size('"SectionConteneurItems"')) AS "Taille Index"
FROM pg_class
WHERE relname = 'SectionConteneurItems';
```

**Note**: L'ajout de colonnes `NULL` n'augmente pas significativement la taille de la table tant qu'elles ne sont pas remplies.

---

## ✅ Checklist de Validation Complète

Avant de déclarer la migration réussie, vérifiez :

- [ ] **Script exécuté sans erreur** (Messages: 3 NOTICE affichés)
- [ ] **3 colonnes visibles** dans Data Output de la requête de vérification
- [ ] **Colonnes visibles** dans l'arborescence pgAdmin (Columns)
- [ ] **Application redémarrée** (conteneur Docker)
- [ ] **Erreur 42703 disparue** (logs de l'application)
- [ ] **Fonctionnalité testée** (ajout de section libre dans un document)
- [ ] **Aucun effet de bord** (autres fonctionnalités OK)

---

## 🚨 Cas d'Erreur Fréquents

### ❌ Erreur: "permission denied for table SectionConteneurItems"
**Solution**:
```sql
-- Se connecter en tant qu'utilisateur postgres (admin)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO generateur_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO generateur_user;
```

### ❌ Erreur: "relation 'SectionConteneurItems' does not exist"
**Cause**: La table n'a jamais été créée (base vide).
**Solution**: Appliquer d'abord la migration `InitialCreate` complète via EF Core.

### ❌ Erreur: "syntax error at or near 'DO'"
**Cause**: Version PostgreSQL trop ancienne (< 9.0).
**Solution**: Mettre à jour PostgreSQL vers une version récente (13+).

---

## 📞 Contact et Support

Pour toute question ou problème :
1. Vérifiez les logs PostgreSQL : `Tools > Server Status > Log File`
2. Vérifiez les logs de l'application : `Logs/app-*.log`
3. Consultez la documentation PostgreSQL : https://www.postgresql.org/docs/

---

**🎉 Félicitations !** Si vous avez suivi toutes ces étapes, votre base de données PostgreSQL est maintenant synchronisée avec le modèle de l'application.

**📝 Conservation** : Gardez ce fichier et `AddPersonnalisationColumns_PostgreSQL.sql` pour référence future ou pour d'autres environnements.
