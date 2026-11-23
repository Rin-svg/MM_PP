# Guide de contribution : travailler à 5 avec git

Ce guide explique **comment l'équipe travaille avec git**. Lisez-le en entier une fois, puis gardez la section 3 sous la main.

Les commandes sont écrites pour **Windows PowerShell** et utilisent `git switch` (Git 2.23 ou plus récent).

## 1. Mise en place (une seule fois par personne)

1. **Accepter l'invitation** de collaborateur reçue par e-mail (envoyée par le propriétaire du dépôt).
2. **Installer** [Git pour Windows](https://git-scm.com/download/win) (il inclut le gestionnaire d'identifiants : au premier `push`, une fenêtre de connexion GitHub s'ouvre) et le SDK **.NET 10**.
3. **Dire à git qui vous êtes.** C'est important : c'est ce qui associe vos commits à *votre* profil GitHub.
   ```powershell
   git config --global user.name "Prénom Nom"
   git config --global user.email "123456+votre-pseudo@users.noreply.github.com"
   ```
   Votre adresse se trouve sur GitHub dans *Settings → Emails* (si « Keep my email addresses private » est coché, GitHub vous donne cette adresse en `users.noreply.github.com`).
4. **Cloner** le dépôt :
   ```powershell
   git clone https://github.com/Rin-svg/MM_PP.git
   cd MM_PP
   ```
5. **Vérifier** que tout fonctionne chez vous :
   ```powershell
   dotnet build src/SlideStudio.Core
   dotnet test tests/SlideStudio.Core.Tests
   ```

## 2. Le principe : une branche par tâche

```
main      ●──────●──────────●──────────●──    toujours compilable, jamais de travail direct
           \              ↗   \       ↗
feat/a      ●──●──●──●───┘     \     │          une branche courte par tâche
feat/b                          ●──●─┘          fusionnée par Pull Request
```

- **`main`** contient uniquement du code qui compile et dont les tests passent. On n'y écrit **jamais** directement.
- Chaque tâche a **sa branche**, créée à partir de `main`, courte (quelques jours au plus).
- On fusionne dans `main` par **Pull Request (PR)** : une demande de fusion que quelqu'un d'autre relit.

Pourquoi pas une branche `develop` en plus ? Avec 5 personnes, une seule branche principale suffit et c'est plus simple à comprendre. Cette méthode s'appelle le *GitHub Flow*.

## 3. Le cycle de travail, pas à pas

**① Partir du code à jour**
```powershell
git switch main
git pull
```

**② Créer sa branche** (le nom suit la règle `type/zone-sujet`, voir section 4)
```powershell
git switch -c feat/persistance-lecture-slds
```

**③ Travailler et commiter par petites étapes** : un commit = une idée complète **qui compile**.
```powershell
git status                                  # qu'est-ce qui a changé ?
git add src/SlideStudio.Infrastructure      # ajouter des chemins précis
git commit -m "feat(infra): lecture d'un fichier .slds avec limite de taille"
```
Évitez `git add .` : il ajoute tout, y compris ce que vous n'avez pas voulu modifier. Regardez toujours `git status` avant de commiter.

**④ Avant de pousser : compiler et tester**
```powershell
dotnet build src/SlideStudio.Infrastructure     # 0 avertissement, 0 erreur
dotnet test tests/SlideStudio.Infrastructure.Tests
```
Si vous avez touché au `Core`, lancez aussi `dotnet test tests/SlideStudio.Core.Tests`.

**⑤ Pousser sa branche**
```powershell
git push -u origin feat/persistance-lecture-slds
```
(`-u` seulement la première fois ; ensuite un simple `git push` suffit.)

**⑥ Ouvrir la Pull Request** sur GitHub (le terminal affiche un lien, ou utilisez le bouton « Compare & pull request »). Remplissez le modèle proposé, puis demandez une relecture au relecteur prévu (section 5).

**⑦ Répondre à la relecture.** Si on vous demande des changements, faites-les sur la **même branche** puis `git push` : la PR se met à jour toute seule.

**⑧ Fusionner.** Une fois la PR approuvée, **l'auteur** clique sur **« Squash and merge »** (tous les commits de la branche deviennent un seul commit propre sur `main`, avec le titre de la PR comme message), puis sur « Delete branch ».

**⑨ Nettoyer en local**
```powershell
git switch main
git pull
git branch -d feat/persistance-lecture-slds
```

## 4. Nommage

**Branches** : `type/zone-sujet`, en minuscules, mots séparés par des tirets.

| Type | Quand | Exemple |
|---|---|---|
| `feat` | nouvelle fonctionnalité | `feat/canvas-poignees-redimensionnement` |
| `fix` | correction d'un bug | `fix/presenters-double-clic-enregistrer` |
| `test` | ajout ou amélioration de tests | `test/infra-fichiers-hostiles` |
| `docs` | documentation | `docs/equipe` |
| `refactor` | réorganisation sans changer le comportement | `refactor/core-commandes` |
| `chore` | configuration, outils | `chore/ci-github-actions` |

**Commits et titres de PR** : `type(portée): résumé` en français. La portée est la zone : `core`, `infra`, `presenters`, `ui`, `tests`, `docs`. Le résumé dit **ce que ça apporte**, pas « modifs diverses ».

- ✅ `feat(ui): panneau de miniatures avec glisser-déposer`
- ✅ `fix(core): refuse un texte de plus de 50 000 caractères`
- ❌ `modifs`, `ça marche`, `update`

Le titre de la PR devient le message du commit sur `main` (à cause du « Squash and merge ») : soignez-le.

## 5. Pull Requests et relectures

**Une bonne PR :**
- traite **un seul sujet** ;
- reste **petite** (idéalement moins de 400 lignes modifiées) : une grosse PR est difficile à relire sérieusement ;
- peut être ouverte en **brouillon** (*Draft*) très tôt pour montrer où vous en êtes ;
- décrit ce qu'elle fait et comment la tester, avec une capture d'écran pour l'interface.

**Qui relit qui** (relecture « en cercle », pour que chacun relise et soit relu) :

| Auteur | Relecteur par défaut |
|---|---|
| Membre 1 | Membre 2 |
| Membre 2 | Membre 3 |
| Membre 3 | Membre 4 |
| Membre 4 | Membre 5 |
| Membre 5 | Membre 1 |
| Toute modification du `Core` | **2 relecteurs** dont le responsable de la zone concernée |

**Le relecteur vérifie :**
- la liste de contrôle du modèle de PR est cochée **et vraie** (il lance le code s'il le faut) ;
- le code est compréhensible, les commentaires expliquent le *pourquoi* ;
- les cas limites et les entrées invalides sont testés ;
- toute entrée venant de l'extérieur est validée ;
- rien d'inutile n'a été ajouté (`bin/`, `obj/`, fichiers personnels).

Relire, c'est aussi **apprendre** : posez des questions plutôt que d'écrire seulement « OK ». Répondez aux relectures dans la journée si possible, pour ne bloquer personne.

## 6. Rester à jour et gérer les conflits

Un **conflit** arrive quand deux personnes modifient les mêmes lignes. C'est normal, et rare si chacun reste dans sa zone.

**Au moins une fois par jour, et toujours avant de demander une relecture**, récupérez les changements des autres :
```powershell
git fetch origin
git merge origin/main
```

Si git annonce `CONFLICT`, ouvrez le fichier indiqué. Vous y verrez :
```
<<<<<<< HEAD
votre version
=======
la version des autres
>>>>>>> origin/main
```
1. Décidez ce qu'il faut garder (souvent **les deux**), puis supprimez les trois lignes de marqueurs (`<<<<<<<`, `=======`, `>>>>>>>`).
2. Lancez `dotnet build` **et** `dotnet test` : un conflit mal résolu se voit là.
3. Terminez :
   ```powershell
   git add chemin/du/fichier
   git commit
   ```
Visual Studio et VS Code proposent un éditeur de fusion graphique qui facilite ces étapes.

**Fichiers partagés : attention.** `SlideStudio.sln`, les `*.csproj`, `Directory.Build.props`, `.editorconfig` et `Limits.cs` concernent tout le monde. Modifiez-les rarement, dans une PR dédiée, et **prévenez le groupe**. Pour `SlideStudio.sln`, chaque membre y ajoute son propre projet : en cas de conflit, gardez les deux lignes.

## 7. Règles d'or

1. **Jamais de push direct sur `main`.** Jamais de `git push --force` sur `main` ni sur une branche partagée.
2. **`main` compile toujours.** Compilez et testez avant de pousser.
3. **Une branche = une personne.** Si deux personnes doivent travailler sur la même branche, prévenez-vous et faites `git pull` très souvent.
4. **Ne commitez jamais** `bin/`, `obj/`, `.vs/`, des mots de passe, des clés ou des fichiers personnels. Le `.gitignore` en exclut déjà la plupart ; vérifiez avec `git status`.
5. **Ne désactivez pas** un avertissement ou une règle d'analyse pour « faire passer » la compilation : demandez au groupe.
6. **Pas de gros fichiers** (plusieurs Mo) sans en parler.
7. **Un doute ? Demandez avant de forcer.** Mieux vaut poser une question que perdre le travail de quelqu'un.

## 8. Problèmes fréquents

| Problème | Solution |
|---|---|
| `rejected … fetch first` en poussant ma branche | `git pull` puis `git push`. |
| J'ai commité sur `main` par erreur (pas encore poussé) | Voir l'encadré ci-dessous. |
| Je veux annuler mon dernier commit (pas encore poussé) en gardant mes modifications | `git reset --soft HEAD~1` |
| J'ai commité un fichier de trop (par exemple `bin/`) | `git rm -r --cached chemin/du/dossier`, vérifiez `.gitignore`, puis commitez à nouveau. |
| Les avertissements « LF will be replaced by CRLF » | Normaux sous Windows : git adapte les fins de ligne. Rien à faire. |
| Ma compilation échoue juste après un `git pull` | Quelqu'un a peut-être ajouté un projet ou un fichier : relancez `dotnet build`. Si ça persiste, demandez à l'auteur du dernier changement. |
| J'ai poussé un mot de passe ou une clé par erreur | **Prévenez le groupe tout de suite** et révoquez la clé : la supprimer du dépôt ne suffit pas, l'historique la garde. |

**J'ai commité sur `main` par erreur** (travail déjà commité, pas encore poussé) :
```powershell
git switch -c feat/ma-branche     # la nouvelle branche garde vos commits
git switch main
git reset --hard origin/main      # main redevient identique à GitHub
```
Attention : `reset --hard` efface les modifications **non commitées**. Si vous en avez, commitez-les d'abord sur la nouvelle branche.

## 9. Pour le propriétaire du dépôt (une seule fois)

À faire sur GitHub, dans les réglages du dépôt (*Settings*) :

1. **Collaborateurs** : *Collaborators → Add people* pour les 4 autres membres (chacun doit accepter l'invitation).
2. **Protéger `main`** : dans *Branches* (ou *Rules → Rulesets* selon l'interface), exiger une **Pull Request avant fusion** et **au moins 1 approbation**, et interdire les push forcés.
3. **Fusion** : dans *General → Pull Requests*, autoriser « Squash merging » et cocher la suppression automatique des branches fusionnées.
4. **Suivi** : créer une **Issue** par tâche du README (étiquette ou assigné = le responsable) et un tableau *Projects* avec les colonnes « À faire », « En cours », « En revue », « Terminé ».
5. Ajouter `.github/pull_request_template.md` (modèle de PR fourni avec ce guide) : GitHub l'affichera automatiquement à chaque nouvelle PR.
