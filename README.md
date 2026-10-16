# FTELMarket — API

API ASP.NET Core (.NET 10) du TP « Installation et CRUD ». Elle sert le front
[FTEL-MARKET-FRONT](https://github.com/chrisfarin2/FTEL-MARKET-FRONT) et persiste
les données dans PostgreSQL via Entity Framework Core en *Code First*.

## Démarrage

Prérequis : SDK .NET 10, Docker Desktop, outil `dotnet-ef`
(`dotnet tool install --global dotnet-ef`).

```bash
# 1. Base de données PostgreSQL (port 5433 côté hôte)
docker compose up -d

# 2. Création des tables + données initiales (catégories, produits de démo)
cd src/FtelMarket.Api
dotnet ef database update

# 3. API sur http://localhost:5082 (profil "http")
dotnet run
```

- Documentation interactive : <http://localhost:5082/scalar>
- Contrat OpenAPI : <http://localhost:5082/openapi/v1.json>
- Requêtes prêtes à l'emploi : [`FtelMarket.Api.http`](src/FtelMarket.Api/FtelMarket.Api.http)

Puis lancer le front (`docker compose up -d` dans son dépôt) et ouvrir
<http://localhost:3000> : les 5 badges du catalogue passent au vert.

## Routes

| Méthode  | Route                 | Succès                    | Erreurs                                                     |
| -------- | --------------------- | ------------------------- | ----------------------------------------------------------- |
| `GET`    | `/api/products`       | 200 + tableau de produits | —                                                           |
| `GET`    | `/api/products/{id}`  | 200 + produit             | 404 `{ message }`                                           |
| `GET`    | `/api/categories`     | 200 + tableau             | —                                                           |
| `POST`   | `/api/products`       | 201 + produit + `Location`| 400 ProblemDetails (saisie) · 400 `{ message }` (catégorie) |
| `PUT`    | `/api/products/{id}`  | 200 + produit             | 400 (idem POST) · 404 `{ message }`                         |
| `DELETE` | `/api/products/{id}`  | 204                       | 404 `{ message }`                                           |

Deux formats d'erreur, comme l'attend le front (`src/lib/api/errors.ts`) :
les erreurs **de saisie** sont des ProblemDetails (RFC 9457) produits par
`[ApiController]`, avec un dictionnaire `errors` par champ et des messages en
français ; les erreurs **métier** sont un simple `{ "message": "..." }`.

## Organisation

```
src/FtelMarket.Api/
├── Controllers/          Points d'entrée HTTP (ControllerBase + [ApiController])
├── Models/               Contrat d'API : requêtes, réponses, erreurs (DTO)
├── Infrastructure/       Tout ce qui touche à la base de données
│   ├── Entities/         Tables : Product, Category, User
│   ├── Configurations/   Fluent API (IEntityTypeConfiguration<T>) par table
│   ├── Migrations/       Générées par `dotnet ef migrations add`
│   ├── DataSeeder.cs     Données initiales
│   └── FtelMarketDbContext.cs
├── appsettings*.json     Configuration (chaîne de connexion, CORS)
└── Program.cs            Enregistrement des services et pipeline HTTP
```

Les **entités** (base) et les **modèles d'API** (contrat) sont volontairement
séparés : la base peut évoluer (ajout de `SellerId`, index…) sans casser le
front, et l'API n'expose jamais un champ par accident.

## Modèle de données

```
users (1) ──< products >── (1) categories
```

- **User** : `id`, `user_name` (unique), `created_at`. Chaque utilisateur
  possède un catalogue de produits.
- **Product** : `id`, `name` (≤ 200), `description` (facultative, ≤ 1000),
  `price` `numeric(10,2)` > 0, `stock` ≥ 0, `created_at` (UTC),
  `category_id` (obligatoire), `seller_id` (obligatoire).
- **Category** : `id`, `name` (unique, ≤ 100). Une catégorie regroupe plusieurs
  produits ; elle ne peut pas être supprimée tant qu'elle en contient (`RESTRICT`).

Tant qu'il n'y a pas d'authentification, les produits créés rejoignent le
catalogue de l'utilisateur `demo` (créé par le seeding).

## Réponses aux questions du TP

### Rôle des fichiers générés

- **`Program.cs`** : point d'entrée. Il construit l'application en deux temps :
  enregistrement des services dans l'injecteur de dépendances
  (`builder.Services` : contrôleurs, EF Core, CORS, OpenAPI…), puis
  configuration du pipeline de middlewares traversé par chaque requête HTTP
  (gestion d'erreurs, CORS, routage vers les contrôleurs).
- **`appsettings.json`** : configuration de l'application (niveaux de log,
  chaînes de connexion, origines CORS…). `appsettings.{Environnement}.json`
  surcharge ses valeurs selon `ASPNETCORE_ENVIRONMENT` ; ici les identifiants
  de la base de développement sont dans `appsettings.Development.json`.
- **`Properties/launchSettings.json`** : utilisé **uniquement en local** par
  `dotnet run` / Visual Studio. Il définit des profils de lancement (`http` sur
  5082, `https` sur 7236 + 5082), les URL d'écoute et les variables
  d'environnement (`ASPNETCORE_ENVIRONMENT=Development`). Il n'est pas déployé.

### Le prix : `decimal`, `double` ou `float` ?

**`decimal`.** `float` et `double` sont des flottants **binaires** : ils ne
peuvent pas représenter exactement 0,1 ou 19,90 (`0.1 + 0.2 == 0.30000000000000004`).
Sur de l'argent, ces erreurs s'accumulent (totaux, TVA, remises) et faussent
les montants. `decimal` est un type **décimal** à 28-29 chiffres significatifs :
19,90 vaut exactement 19,90. En base, il est stocké en `numeric(10,2)`, lui
aussi exact, au centime près. L'API refuse un prix à plus de 2 décimales
plutôt que de l'arrondir en silence.

### Les dates et les fuseaux horaires

Toutes les dates sont **stockées et échangées en UTC** :
- générées côté serveur avec `TimeProvider.GetUtcNow()` (jamais `DateTime.Now`,
  qui dépend du fuseau du serveur) ;
- stockées en `timestamp with time zone` (PostgreSQL les conserve en UTC) ;
- sérialisées en ISO 8601 avec le suffixe `Z` (`2026-09-02T09:15:00Z`).

Le front convertit dans le fuseau du navigateur au moment de l'affichage. Un
même instant est ainsi correct pour un utilisateur à Paris comme à Montréal,
et les tris/comparaisons restent justes.

## Commandes Entity Framework utiles

```bash
dotnet ef migrations add <Nom> --output-dir Infrastructure/Migrations
dotnet ef database update            # applique les migrations + seeding
dotnet ef migrations list
dotnet ef migrations remove          # dernière migration non appliquée
dotnet ef migrations script          # SQL généré, sans toucher la base
dotnet ef database drop
```

Vérifier les tables en base :

```bash
docker exec -it ftelmarket-api-postgres psql -U ftelmarket -d ftelmarket -c "\d+ products"
```

ou avec DBeaver : hôte `localhost`, port `5433`, base/utilisateur/mot de passe
`ftelmarket`.
