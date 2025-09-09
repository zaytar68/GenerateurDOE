# Composant AutoComplete

Le composant `AutoComplete<T>` est un composant Blazor réutilisable qui fournit une fonctionnalité d'autocomplétion pour tous types d'objets.

## Fonctionnalités

- **Générique** : Fonctionne avec n'importe quel type T
- **Recherche en temps réel** : Filtrage instantané lors de la saisie
- **Navigation clavier** : Support des flèches, Enter et Escape
- **Template personnalisable** : Affichage customisé des éléments
- **Validation intégrée** : Compatible avec les DataAnnotations
- **Responsive** : Utilise Bootstrap pour un design adaptatif

## Paramètres

| Paramètre | Type | Description | Obligatoire |
|-----------|------|-------------|-------------|
| `Items` | `List<T>` | Liste des éléments à rechercher | Oui |
| `DisplaySelector` | `Func<T, string>` | Fonction pour extraire le texte d'affichage | Oui |
| `ValueSelector` | `Func<T, object>` | Fonction pour extraire la valeur | Non |
| `ItemTemplate` | `RenderFragment<T>` | Template personnalisé pour l'affichage | Non |
| `Placeholder` | `string` | Texte d'aide dans le champ | Non |
| `MinSearchLength` | `int` | Longueur minimale pour déclencher la recherche | Non (défaut: 1) |
| `OnItemSelected` | `EventCallback<T>` | Callback lors de la sélection d'un élément | Non |
| `OnTextChanged` | `EventCallback<string>` | Callback lors du changement de texte | Non |
| `ValidationMessage` | `string` | Message de validation à afficher | Non |

## Exemples d'utilisation

### Exemple simple avec des chaînes de caractères

```razor
<AutoComplete T="string"
              Items="fabricants"
              DisplaySelector="f => f"
              Placeholder="Rechercher un fabricant..."
              OnItemSelected="OnFabricantSelected" />
```

### Exemple avec un objet personnalisé

```razor
<AutoComplete T="TypeProduit"
              Items="typesProduits"
              DisplaySelector="t => t.Nom"
              Placeholder="Sélectionner un type de produit..."
              OnItemSelected="OnTypeProduitSelected">
    <ItemTemplate Context="type">
        <div>
            <strong>@type.Nom</strong>
            @if (!string.IsNullOrEmpty(type.Description))
            {
                <br />
                <small class="text-muted">@type.Description</small>
            }
        </div>
    </ItemTemplate>
</AutoComplete>
```

### Utilisation pour le filtrage

```razor
<AutoComplete T="string"
              Items="fabricantsDisponibles"
              DisplaySelector="f => f"
              Placeholder="Filtrer par fabricant..."
              OnTextChanged="OnFabricantFilterChanged"
              MinSearchLength="2" />
```

## Code C# associé

```csharp
private List<string> fabricants = new();
private List<TypeProduit> typesProduits = new();

private void OnFabricantSelected(string fabricant)
{
    // Logique lors de la sélection d'un fabricant
    selectedFabricant = fabricant;
}

private void OnTypeProduitSelected(TypeProduit type)
{
    // Logique lors de la sélection d'un type
    selectedType = type;
}

private void OnFabricantFilterChanged(string text)
{
    // Logique pour le filtrage
    filtreText = text;
    FilterData();
}
```

## Navigation clavier

- **Flèche bas** : Sélectionner l'élément suivant
- **Flèche haut** : Sélectionner l'élément précédent  
- **Enter** : Valider la sélection courante
- **Escape** : Fermer la liste déroulante

## Styling

Le composant utilise les classes Bootstrap standard :
- `form-control` pour le champ de saisie
- `is-invalid` et `invalid-feedback` pour la validation
- CSS personnalisé pour la dropdown

## Performance

- Limite automatiquement les résultats à 10 éléments
- Utilise `StringComparison.OrdinalIgnoreCase` pour la recherche
- Délai de 150ms sur le blur pour permettre les clics sur les éléments