# SlideStudio — application de présentation en C# (WinForms)

Projet de cours : une application de présentation « type PowerPoint », écrite en C# avec une interface WinForms.

**État : jalon M1 terminé** — le « cerveau » de l'application (modèle de document, validation, annuler/rétablir)
est écrit et testé. L'interface graphique arrive aux jalons M4 et suivants.

## Prérequis
- Le SDK **.NET 10** et un IDE compatible (Visual Studio 2026, Rider récent ou VS Code).
- L'interface WinForms (jalon M4) ne fonctionnera que sous **Windows**. Le `Core` et ses tests tournent partout.

## Commandes
```
dotnet build src/SlideStudio.Core          # compile le Core en mode strict (avertissements = erreurs)
dotnet test  tests/SlideStudio.Core.Tests  # lance les 215 tests (nécessite l'accès à NuGet pour xUnit)
```

## Architecture (dépendances dans un seul sens : UI → Presenters → Core ← Infrastructure)
| Projet | Cible | Rôle | État |
|---|---|---|---|
| `SlideStudio.Core` | net10.0 | Modèle (`Presentation`, `Slide`, éléments), validation, commandes, historique annuler/rétablir. Aucune dépendance UI ni NuGet. | **M1 ✔** |
| `SlideStudio.Infrastructure` | net10.0 | Format de fichier `.slds`, écriture atomique, copie de secours, lecture d'images. | M2 |
| `SlideStudio.Presenters` | net10.0 | Logique de chaque écran (patron MVP), testable sans fenêtre. | M3 |
| `SlideStudio.UI` | net10.0-windows | Formulaires WinForms « bêtes » : affichage et événements seulement. | M4–M7 |

## Où lire le code en premier
1. `Core/Limits.cs` — toutes les limites (taille de texte, nombre de diapositives…) et pourquoi elles existent.
2. `Core/DocumentRules.cs` — la validation en liste blanche (c'est la barrière de sécurité du modèle).
3. `Core/Editing/UndoHistory.cs` — l'historique annuler/rétablir et le suivi des modifications non enregistrées.
4. `tests/` — chaque règle est documentée par un test ; les noms de tests se lisent comme des phrases.

## Conventions
- Commentaires en français qui expliquent le **pourquoi** ; balises `POURQUOI`, `SÉCURITÉ`, `ROBUSTESSE`, `UTILISATEUR`, `LIMITE`, `ATTENTION`.
- Toute modification du document passe par une **commande** (`EditCommands`, `UndoHistory.Execute`) pour rester annulable.
- Une valeur refusée par la validation ne modifie jamais le document.

## Workflow Git
- Branche principale : `main`. Une branche courte par fonctionnalité (par exemple `feat/enregistrement`), fusionnée quand la compilation et les tests passent.
- Messages de commit : `type(portée): résumé` en français, avec `type` parmi `feat`, `fix`, `test`, `docs`, `refactor`, `chore`.
- Avant chaque commit : `dotnet build` (0 avertissement) puis `dotnet test`.
- Un commit = une idée complète **et qui compile**.
