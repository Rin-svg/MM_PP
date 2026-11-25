# SlideStudio — application de présentation en C# (WinForms)

Projet de cours réalisé en **équipe de 5** : une application de présentation « type PowerPoint », écrite en C# avec une interface WinForms.

**État actuel : M1, M2 et M3 sont fusionnés dans `main`.** Le modèle, la validation et l'historique du Core sont couverts par 215 tests; les Presenters par 12 tests; et le stockage `.slds` par 12 tests. Les médias sont embarqués dans `media/` et validés avant décodage (format, taille et dimensions). M4 et les jalons suivants restent à réaliser.

## Démarrage rapide

Prérequis : le SDK **.NET 10** et un IDE compatible (Visual Studio 2026, Rider récent ou VS Code). L'interface WinForms ne fonctionnera que sous **Windows**. Le `Core` et ses tests tournent partout.

```powershell
git clone https://github.com/Rin-svg/MM_PP.git
cd MM_PP
dotnet build src/SlideStudio.Core          # compile en mode strict (avertissements = erreurs)
dotnet test  tests/SlideStudio.Core.Tests  # lance les 215 tests
```

Pour compiler toute la solution et lancer les trois suites de tests, depuis Windows :

```powershell
dotnet build SlideStudio.sln
dotnet test tests/SlideStudio.Core.Tests
dotnet test tests/SlideStudio.Presenters.Tests
dotnet test tests/SlideStudio.Infrastructure.Tests
```

Les tests Infrastructure sont lancés explicitement : le projet de tests n'est pas exécuté par `dotnet test SlideStudio.sln`.

Avant votre premier commit : configurez votre identité git (voir [CONTRIBUTING.md](CONTRIBUTING.md), section 1), sinon vos commits ne seront pas associés à votre profil GitHub.

## Architecture

Les dépendances vont dans un seul sens : **UI → Presenters → Core ← Infrastructure**. Le `Core` ne dépend de rien ; seul le point d'entrée de l'UI (`Program.cs`) connaît toutes les couches pour les assembler.

| Projet | Cible | Rôle | Jalon | Responsable |
|---|---|---|---|---|
| `SlideStudio.Core` | net10.0 | Modèle (`Presentation`, `Slide`, éléments), validation, commandes, historique annuler/rétablir. Aucune dépendance. | **M1 ✔** | Tout le groupe : toute modification passe par une PR relue par **2 personnes** |
| `SlideStudio.Infrastructure` (`SlideStudio/SlideStudio.Infrastructure`) | net10.0 | Archive `.slds`, sérialisation JSON, écriture atomique avec copie de secours, lecture des présentations. | **M2 ✔** | Membre 1 |
| `SlideStudio.Presenters` (`src/SlideStudio.Presenters`) | net10.0 | Logique de chaque écran (patron MVP), testable sans fenêtre. | **M3 ✔** | Membre 2 |
| `SlideStudio.UI` (`SlideStudio/SlideStudio.UI`) | net10.0-windows | Interface WinForms : affichage et événements; fenêtre encore à compléter. | M4, M5 | Membres 3 et 4 |
| Diaporama, export, design, qualité | UI + documentation | Mode présentation, export, thèmes, tests d'intégration, CI. | M6, M7 | Membre 5 |

## L'équipe et la répartition des tâches

> **À faire en premier :** chaque personne écrit son pseudo GitHub dans la colonne « Responsable » (dans une petite PR `docs/equipe`). Les rôles ci-dessous sont une proposition : ajustez-les selon les envies et le niveau de chacun, mais gardez **un seul responsable par zone du code**, c'est ce qui évite les conflits.

| # | Rôle | Responsable (pseudo GitHub) | Zone du code | Jalon |
|---|---|---|---|---|
| 1 | Persistance et sécurité des fichiers | *à compléter* | `SlideStudio/SlideStudio.Infrastructure` | M2 |
| 2 | Logique applicative (Presenters) | *à compléter* | `src/SlideStudio.Presenters` | M3 |
| 3 | Fenêtre principale et panneaux | Atangana-Monique | `SlideStudio/SlideStudio.UI` (hors `Canvas/`) | M4 |
| 4 | Zone de dessin et édition | *à compléter* | `SlideStudio/SlideStudio.UI/Canvas` | M5 |
| 5 | Diaporama, export, design et qualité | *à compléter* | `SlideStudio/SlideStudio.UI/Slideshow`, `docs/`, CI | M6, M7 |

Chaque tâche ci-dessous devient une **Issue GitHub** (une par ligne de liste), assignée à son responsable et suivie dans un tableau « À faire / En cours / En revue / Terminé ».

### Membre 1 — Persistance et sécurité des fichiers (jalon M2)
- Ajouter au `Core` `MediaStore` (images embarquées) et `DocumentPackage` (présentation + images).
- Définir le format de fichier `.slds` (archive zip : `presentation.json` et dossier `media/`).
- Lire et écrire un fichier `.slds` ; écriture « atomique » (jamais de fichier à moitié écrit) ; messages d'erreur clairs (fichier verrouillé, disque plein…).
- Vérifier les images **avant** de les décoder (taille, dimensions) ; copie de secours automatique.
- Se protéger des fichiers piégés (zip malveillant, JSON trop profond, image géante) et le **prouver par des tests**.
- *Livrable :* une implémentation de `IDocumentStorage` + tests (aller-retour, fichiers hostiles). *Fournit à :* membre 2 (ouvrir/enregistrer), membre 4 (images).

### Membre 2 — Logique applicative, les Presenters (jalon M3)
- Définir les interfaces des vues et des services (`IMainView`, `IDialogService`, `IDocumentStorage`…) : c'est la **première tâche de l'équipe**, voir « Contrats communs » plus bas.
- `MainPresenter` : nouveau, ouvrir, enregistrer, enregistrer sous, fermeture avec confirmation s'il y a des modifications non enregistrées, annuler/rétablir, état des boutons (actifs/grisés), ajout, déplacement et duplication de diapositives.
- `SelectionPresenter` : sélection, copier/couper/coller, dupliquer, supprimer, ordre de superposition et mise en forme du texte et des formes.
- Protéger contre les doubles clics et les actions pendant une opération de fichier.
- Tester avec de « fausses vues » (aucune fenêtre nécessaire) : 12 tests M3.
- *Livrable :* Presenters + tests. *Fournit à :* membres 3 et 4.

### Membre 3 — Fenêtre principale et panneaux (jalon M4)
- Compléter le projet WinForms existant (`SlideStudio/SlideStudio.UI`, `net10.0-windows`) et `Program.cs` : gestion de l'échelle d'affichage (DPI), filets pour les erreurs non gérées, assemblage des couches. Actuellement, `Program.cs` lance seulement le formulaire vide `Form1`.
- Fenêtre principale : menus, barre d'outils (ou ruban simplifié), raccourcis (Ctrl+N/O/S/Z/Y…), barre d'état, astérisque « modifié » dans le titre.
- Panneau de miniatures (liste fluide même avec 500 diapositives, glisser-déposer pour réordonner, menu contextuel).
- Panneau de propriétés (police, taille, couleurs) et boîtes de dialogue (`WinFormsDialogService`).
- *Livrable :* fenêtre complète branchée aux Presenters. *Dépend de :* interfaces du membre 2, `SlideRenderer` du membre 4 (miniatures).

### Membre 4 — Zone de dessin et édition (jalon M5)
- `SlideRenderer` : dessine une diapositive (texte, formes, images) à n'importe quelle échelle. **À livrer très tôt**, car miniatures, diaporama et export le réutilisent.
- `SlideCanvas` : affichage, zoom, sélection à la souris, poignées de redimensionnement et de rotation, déplacement. Un glisser de souris = **une seule** étape d'annulation (`gestureId`, déjà prévu dans le `Core`).
- Édition du texte directement sur la diapositive ; insertion d'images ; outils de création des formes.
- Affichage fluide (double buffering), sans scintillement.
- *Livrable :* contrôle `SlideCanvas`. C'est la partie **la plus longue** : les autres membres peuvent l'aider une fois leur zone terminée.

### Membre 5 — Diaporama, design et qualité (jalons M6 et M7, et transversal)
- Mode diaporama plein écran (F5, flèches, Échap, écran noir).
- Export des diapositives en images PNG ; export PDF (piste à étudier ensemble).
- Design : palette, thèmes de diapositives, icônes, cohérence visuelle, contraste du texte (≥ 4,5:1) ; accessibilité (utilisable au clavier seul).
- Qualité : dérouler une liste de scénarios à risque (double-clic sur Enregistrer, fichier verrouillé, fermeture sans enregistrer…) avant chaque grande fusion ; mettre en place la CI (compilation + tests à chaque PR) ; tenir à jour ce README et les schémas d'architecture ; préparer la démonstration finale.
- *Dépend de :* `SlideRenderer` du membre 4.

## Ordre de travail

1. **Étape 0 — Mise en place (tous).** Cloner, configurer son identité git, compiler, lancer les tests. Le propriétaire du dépôt ajoute les collaborateurs et protège `main` (voir CONTRIBUTING.md, section 9).
2. **Étape 1 — Contrats communs (petites PR, relues par tous).** Avant de coder en parallèle, on se met d'accord sur les « prises » entre les parties :

   | Contrat | Où | Écrit par | Utilisé par |
   |---|---|---|---|
   | `MediaStore`, `DocumentPackage` | Core | Membre 1 | 2, 3, 4 |
   | `IDocumentStorage` (ouvrir / enregistrer) | Core | Membre 2 définit, membre 1 implémente | 2 |
   | `IDialogService`, interfaces de vues (`IMainView`…) | Presenters | Membre 2 | 3, 4 |
   | `SlideRenderer` (dessiner une diapositive à une échelle) | UI | Membre 4 | 3 (miniatures), 5 (diaporama, export) |

3. **M1, M2 et M3 sont intégrés dans `main`.** Le stockage embarque les médias, et `MainPresenter` utilise le contrat de paquet pour ouvrir et enregistrer.
4. **Poursuivre M4 et démarrer tôt `SlideRenderer` (M5).** Le projet WinForms existe, mais sa fenêtre principale reste un formulaire vide. Livrer le renderer dès que possible : les miniatures de M4 et le diaporama/export en dépendent.
5. **Développement en parallèle.** M4 peut avancer sur les menus, raccourcis et panneaux; brancher l'ouverture/enregistrement via M2, et les miniatures après le renderer.
6. **Intégration.** Le membre 3 assemble les couches ; le membre 5 déroule les scénarios de test ; tout le monde corrige son périmètre.
7. **Finitions et rendu (M7).** Thèmes, accessibilité, échelle d'affichage, documentation, démonstration.

## Travailler avec git (résumé)

On applique le principe le plus simple : **`main` ne contient que du code qui compile et que les tests valident ; tout le travail se fait sur des branches courtes, fusionnées par Pull Request.**

1. `git switch main` puis `git pull` : on part du code à jour.
2. `git switch -c feat/persistance-lecture-slds` : **une branche par tâche** (nommage : `type/zone-sujet`).
3. On travaille, on commit souvent : `git commit -m "feat(infra): lecture d'un fichier .slds"`.
4. `dotnet build` (0 avertissement) puis `dotnet test` : **avant** de pousser.
5. `git push -u origin feat/persistance-lecture-slds`, puis on ouvre une **Pull Request** sur GitHub.
6. Un autre membre **relit** et approuve (jamais le même que l'auteur).
7. L'auteur fusionne avec **« Squash and merge »** et supprime la branche.
8. Tout le monde fait `git switch main` puis `git pull`.

Jamais de push direct sur `main`, jamais de `git push --force` sur une branche partagée. Le guide complet (commandes pas à pas, conflits, problèmes fréquents, règles de relecture) est dans **[CONTRIBUTING.md](CONTRIBUTING.md)**.

## Où lire le code en premier
1. `src/SlideStudio.Core/Limits.cs` : toutes les limites (taille de texte, nombre de diapositives…) et pourquoi elles existent.
2. `src/SlideStudio.Core/DocumentRules.cs` : la validation en liste blanche, c'est la barrière de sécurité du modèle.
3. `src/SlideStudio.Core/Editing/UndoHistory.cs` : l'historique annuler/rétablir et le suivi des modifications non enregistrées.
4. `tests/` : chaque règle est documentée par un test ; les noms de tests se lisent comme des phrases.

## Conventions de code
- Commentaires en français qui expliquent le **pourquoi**, pas le quoi. Balises : `POURQUOI`, `SÉCURITÉ`, `ROBUSTESSE`, `UTILISATEUR`, `LIMITE`, `ATTENTION`.
- Toute modification du document passe par une **commande** (`EditCommands`, `UndoHistory.Execute`) pour rester annulable.
- Une valeur refusée par la validation ne modifie jamais le document.
- Toute entrée venant de l'extérieur (fichier, saisie, presse-papiers) est validée en **liste blanche**.
- Pas de `// TODO` dans le code fusionné : une tâche à faire est une Issue.
- Les avertissements du compilateur sont des erreurs : on corrige, on ne désactive pas une règle sans en parler au groupe.
