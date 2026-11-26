# Scénarios de tests - SlideStudio

## 1. Gestion des fichiers
- Vérifier la création d'une nouvelle présentation.
- Vérifier l'ouverture d'une présentation existante.
- Vérifier l'enregistrement d'une présentation.
- Vérifier le comportement avec un fichier verrouillé.

## 2. Sécurité
- Vérifier le refus des fichiers invalides.
- Vérifier la protection contre les fichiers malveillants.

## 3. Historique
- Vérifier la fonction Annuler (Ctrl+Z).
- Vérifier la fonction Rétablir (Ctrl+Y).

## 4. Interface utilisateur
- Vérifier les raccourcis clavier.
- Depuis l'accueil, parcourir « Créer une présentation » et « Ouvrir un projet » avec les flèches, puis activer le choix avec Entrée ou Espace; vérifier l'annonce du choix par le lecteur d'écran.
- Vérifier que le focus se place sur l'accueil à l'ouverture, sur l'indicateur de chargement pendant la préparation, puis sur le canevas après création/ouverture réussie; le lecteur d'écran annonce aussi les jalons de progression.
- Parcourir la barre d'outils avec Tab et Maj+Tab; vérifier que les commandes Nouveau, Ouvrir, Enregistrer, Annuler, Rétablir, diapositive, insertion et zoom annoncent leur action.
- Vérifier l'échelle de l'interface à 100 %, 150 % et sur deux écrans de facteurs DPI différents; confirmer que les panneaux restent accessibles et que le canevas conserve ses proportions.
- Vérifier le contraste des contenus personnalisés avec des combinaisons de couleurs représentatives; les couleurs de texte et de fond des diapositives sont librement choisies par l'utilisateur.
- Depuis le canevas, vérifier Ctrl+A, les flèches, Maj+flèches, Suppr et Échap.
- Parcourir les diapositives, les éléments et les propriétés avec Tab, Maj+Tab et les flèches; vérifier que le lecteur d'écran annonce leur nom et leur fonction.
- Garder le focus sur le canevas et vérifier que le lecteur d'écran annonce les changements de diapositive et de sélection, sans perdre les indications clavier.
- Vérifier que le texte des boutons d'action et des menus reste lisible au repos, au survol et à l'activation (contraste ≥ 4,5:1).
- Vérifier les messages d'erreur.
- Vérifier la fermeture sans enregistrer.

## 5. Diaporama
- Depuis la fenêtre principale, choisir plusieurs fichiers image et lancer le diaporama.
- Vérifier le passage à la diapositive suivante.
- Vérifier le retour à la diapositive précédente.
- Vérifier l'écran noir avec B.
- Vérifier la fermeture avec Échap.

## 6. Exportation
- Vérifier l'exportation en PNG.
- Vérifier l'exportation en PDF avec une page par diapositive, un PDF valide et la confirmation avant remplacement.

## 7. Démonstration finale
- Créer une présentation, ajouter du texte, une forme et une image, puis modifier et annuler une transformation au canevas.
- Enregistrer, fermer, rouvrir le fichier `.slds` et vérifier la fidélité des diapositives et des médias.
- Parcourir le diaporama, puis exporter la présentation en PNG et en PDF.
- Refaire le parcours principal au clavier et vérifier les annonces du lecteur d'écran.
