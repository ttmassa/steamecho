# SteamEcho

SteamEcho est une application Windows qui gère et affiche les succès de tous vos jeux Steam, qu'ils aient été acheté sur la plateforme ou non. 

## Avertissement important

Ce projet a été développé pour un usage personnel. Son but n'est **aucunement** de promouvoir ou de faciliter le piratage de jeux vidéo.

La plupart des jeux, surtout ceux des studios indépendants, sont le fruit d'un travail acharné et passionné de développeurs qui méritent d'être rémunérés pour leur créativité. Je vous encourage vivement à **acheter les jeux** que vous aimez et à soutenir financièrement leurs créateurs. C'est grâce à votre soutien que l'industrie du jeu vidéo peut continuer à nous offrir des expériences mémorables.

## Fonctionnement

Le projet utilise une approche de proxy DLL pour intercepter les appels à l'API Steam.

1.  **Proxy DLL (`SteamApiProxy`)**: Une DLL C++ remplace celle d'origine dans le dossier d'un jeu. Le projet fournit deux versions de la DLL pour s'adapter à l'architecture du jeu : `steam_api.dll` pour les jeux 32-bit et `steam_api64.dll` pour les jeux 64-bit. Lorsque le jeu déverrouille un succès, notre DLL intercepte l'appel.
2.  **Communication IPC**: La DLL proxy envoie l'identifiant du succès déverrouillé à l'application principale via un canal de communication nommé (Named Pipe).
3.  **Application WPF (`SteamEcho.App`)**: L'application de bureau écoute les messages provenant de la DLL. Lorsqu'un succès est reçu, elle récupère les détails (titre, description, icône) et affiche une notification à l'écran. L'application permet également de gérer votre bibliothèque de jeux et de visualiser les succès.

## Comment l'utiliser ?

1.  **Lancer SteamEcho**: Démarrez l'application `SteamEcho.App.exe`.
2.  **Ajouter un jeu**:
    *   Cliquez sur l'icône "+".
    *   Sélectionnez l'exécutable du jeu. L'application proposera automatiquement une liste de jeux en fonction du nom du fichier.
3.  **Installer le Proxy**:
    *   Dans votre librairie, cliquez sur le jeu que vous venez d'ajouter.
    *   Cliquez sur le bouton **"Installer le proxy"**.
    *   Si besoin, il faudra ajouter le chemin vers l'exécutable en effectuant un clique-droit, puis en cliquant sur "Sélectionner l'exécutable"
    *   L'application s'occupera de sauvegarder la DLL originale et de copier le proxy nécessaire (32 ou 64-bit).
4.  **Jouer**: Lancez votre jeu. Lorsque vous déverrouillerez un succès, une notification apparaîtra.

## Aperçu

Voici un exemple de l'interface et des notifications de succès :

*(Une capture d'écran de l'application principale et d'une notification sera ajoutée ici)*

## Structure du Projet

*   **`SteamEcho.App`**: Le projet principal. C'est une application WPF (.NET) qui contient l'interface utilisateur, les services pour écouter les succès et afficher les notifications.
*   **`SteamEcho.Core`**: Une bibliothèque de classes .NET qui définit les modèles de données (Jeu, Succès) et les interfaces des services.
*   **`SteamApiProxy`**: Un projet C++ qui produit la DLL proxy pour intercepter les appels de l'API Steam.

## Compilation des DLL Proxy (`SteamApiProxy`)

Le projet `SteamApiProxy` est configuré pour être compilé directement avec Visual Studio.

1.  Ouvrez la solution `SteamEcho.sln` dans Visual Studio.
2.  Faites un clic droit sur le projet `SteamApiProxy` dans l'Explorateur de solutions.
3.  Choisissez **Générer**.

Visual Studio compilera automatiquement les deux versions de la DLL :
*   `steam_api.dll` (32-bit) sera générée dans le dossier `Debug` ou `Release`.
*   `steam_api64.dll` (64-bit) sera générée dans le dossier `x64\Debug` ou `x64\Release`.

Les DLL générées sont prêtes à être utilisées par l'application `SteamEcho.App`.

## Stack Technique

*   **Proxy DLL**: C++ pour l'interception des appels bas niveau de l'API Steam.
*   **Application de bureau**: C# avec WPF pour l'interface utilisateur et .NET pour la logique applicative.
*   **Base de données**: SQLite pour stocker les informations sur les jeux et les succès localement.
*   **Communication**: Named Pipes pour la communication inter-processus (IPC) entre la DLL et l'application WPF.

## Contribuer

Ce projet est open source et toutes les contributions sont les bienvenues ! Pour participer, veuillez suivre ces étapes :

1.  **Forker le projet**: Créez une copie du dépôt sur votre propre compte GitHub.
2.  **Créer une branche**: `git checkout -b feature/NouvelleFonctionnalite`
3.  **Commit vos changements**: `git commit -m 'Ajout de ma super fonctionnalité'`
4.  **Push sur votre branche**: `git push origin feature/NouvelleFonctionnalite`
5.  **Ouvrir une Pull Request**: Soumettez une demande de fusion de votre branche vers le dépôt principal.

Chaque contribution sera examinée avant d'être intégrée. Merci de documenter votre code et de respecter le style existant.

Le projet est encore en cours de développement. N'hésitez pas à proposer de nouvelles fonctionnalités ou à signaler les problèmes que vous rencontrez en ouvrant une "Issue" sur GitHub. Votre aide est précieuse !

## Licence

Ce projet est distribué sous la licence MIT. Voir le fichier `LICENSE` pour plus de détails.
1.  **Forker le projet**: Créez une copie du dépôt sur votre propre compte GitHub.
2.  **Créer une branche**: `git checkout -b feature/NouvelleFonctionnalite`
3.  **Commit vos changements**: `git commit -m 'Ajout de ma super fonctionnalité'`
4.  **Push sur votre branche**: `git push origin feature/NouvelleFonctionnalite`
5.  **Ouvrir une Pull Request**: Soumettez une demande de fusion de votre branche vers le dépôt principal.

Chaque contribution sera examinée avant d'être intégrée. Merci de documenter votre code et de respecter le style existant.

Le projet est encore en cours de développement. N'hésitez pas à proposer de nouvelles fonctionnalités ou à signaler les problèmes que vous rencontrez en ouvrant une "Issue" sur GitHub. Votre aide est précieuse !

## Licence

Ce projet est distribué sous la licence MIT. Voir le fichier `LICENSE` pour plus de détails.
