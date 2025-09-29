# 🐘 pgAdmin Quick Start - Générateur DOE

## 📖 Accès à pgAdmin

### URLs par défaut
- **pgAdmin**: http://votre-serveur:8080
- **Connexion**:
  - Email: `cedric.tirolf@multisols.com`
  - Mot de passe: `GenerateurDOE2025!`

## 🔧 Configuration Serveur PostgreSQL

Une fois connecté à pgAdmin, ajoutez le serveur PostgreSQL :

### 1. Ajouter un nouveau serveur
1. Clic droit sur "Servers" > "Register" > "Server"
2. **General Tab**:
   - Name: `Générateur DOE`
   - Server group: `Servers`

3. **Connection Tab**:
   - Host: `localhost`
   - Port: `5432`
   - Database: `GenerateurDOE_Prod`
   - Username: `generateur_user`
   - Password: `GenerateurDOE2025!`
   - ✅ Save password: coché

### 2. Exploration de la base

Vous devriez voir les tables suivantes :
- **Chantiers** - Projets de construction
- **DocumentsGeneres** - Documents PDF générés
- **FichesTechniques** - Fiches produits
- **ImportsPDF** - Fichiers PDF importés
- **SectionsLibres** - Sections personnalisables
- **Methodes** - Méthodologies techniques
- Et leurs relations...

## 🚀 Utilisation Courante

### Requêtes utiles

```sql
-- Voir tous les chantiers
SELECT * FROM "Chantiers" ORDER BY "DateCreation" DESC;

-- Documents générés récents
SELECT d."NomFichier", c."NomProjet", d."DateCreation"
FROM "DocumentsGeneres" d
JOIN "Chantiers" c ON d."ChantierId" = c."Id"
ORDER BY d."DateCreation" DESC;

-- Fiches techniques par type
SELECT ft."NomProduit", tp."Nom" as "TypeProduit"
FROM "FichesTechniques" ft
LEFT JOIN "TypesProduits" tp ON ft."TypeProduitId" = tp."Id";
```

### Monitoring des performances

```sql
-- Taille des tables
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

## ⚠️ Sécurité Production

- [ ] **Changer le mot de passe par défaut** dans docker-compose
- [ ] **Limiter les accès réseau** si nécessaire
- [ ] **Backup régulier** avec pg_dump
- [ ] **Monitoring des connexions** actives

## 🔄 Backup & Restore

### Backup
```bash
# Via Docker
docker exec generateur-doe-postgres pg_dump -U generateur_user GenerateurDOE_Prod > backup.sql
```

### Restore
```bash
# Via Docker
docker exec -i generateur-doe-postgres psql -U generateur_user GenerateurDOE_Prod < backup.sql
```

---
**Version**: Générateur DOE v2.1.3
**Date**: Septembre 2025