# SteamEcho

SteamEcho est une application Windows qui gère et affiche les succès de tous vos jeux Steam, qu'ils aient été acheté sur la plateforme ou non. 

## Avertissement important

Ce projet a été développé pour un usage personnel. Son but n'est **aucunement** de promouvoir ou de faciliter le piratage de jeux vidéo.

La plupart des jeux, surtout ceux des studios indépendants, sont le fruit d'un travail acharné et passionné de développeurs qui méritent d'être rémunérés pour leur créativité. Je vous encourage vivement à **acheter les jeux** que vous aimez et à soutenir financièrement leurs créateurs. C'est grâce à votre soutien que l'industrie du jeu vidéo peut continuer à nous offrir des expériences mémorables.

## Fonctionnement

Le projet utilise une approche de proxy DLL pour intercepter les appels à l'API Steam.

1.  **Proxy DLL (`SteamApiProxy`)**: Une DLL C++ remplace celle d'origine dans le dossier d'un jeu. Le projet fournit deux versions de la DLL pour s'adapter à l'architecture du jeu : `steam_api.dll` pour les jeux 32-bit et `steam_api64.dll` pour les jeux 64-bit. Lorsque le jeu déverrouille un succès, notre DLL intercepte l'appel.
2.  **Communication IPC**: La DLL proxy envoie l'identifiant du succès déverrouillé à l'application principale via un canal de communication nommé (Named Pipe).
3.  **Application WPF (`SteamEcho.App`)**: L'application de bureau écoute les messages provenant de la DLL. Lorsqu'un succès est reçu, elle récupére les détails (titre, description, icône) depuis la base de données et affiche une notification à l'écran.

## Comment l'utiliser ?

1.  **Lancer SteamEcho**: Démarrez l'application `SteamEcho.App.exe`.
2.  **Configurer un jeu**: Ajoutez un jeu à l'application pour qu'elle puisse en suivre les succès.
3.  **Installer le Proxy**:
    *   Allez dans le répertoire d'installation de votre jeu Steam (par exemple : `C:\Program Files (x86)\Steam\steamapps\common\MonJeu`).
    *   Trouvez le fichier `steam_api.dll` (jeux 32-bit) ou `steam_api64.dll` (jeux 64-bit).
    *   **Important :** Renommez le fichier original en `steam_api_original.dll` ou `steam_api64_original.dll` respectivement.
    *   Copiez la DLL correspondante depuis le dossier `SteamApiProxy` de ce projet et collez-la dans le dossier du jeu.
4.  **Jouer**: Lancez votre jeu depuis Steam. Lorsque vous déverrouillerez un succès, une notification apparaîtra.

## Aperçu des notifications

Voici un exemple de l'affichage des notifications de succès en temps réel :

*(Une capture d'écran ou un GIF sera ajouté ici pour montrer une notification en action)*

## Structure du Projet

*   **`SteamEcho.App`**: Le projet principal. C'est une application WPF (.NET) qui contient l'interface utilisateur, les services pour écouter les succès et afficher les notifications.
*   **`SteamEcho.Core`**: Une bibliothèque de classes .NET qui définit les modèles de données (Jeu, Succès) et les interfaces des services.
*   **`SteamApiProxy`**: Un projet C++ qui produit la DLL proxy pour intercepter les appels de l'API Steam.

## Compilation des DLL Proxy (`SteamApiProxy`)

Pour compiler les deux DLL proxy (`steam_api.dll` pour 32 bits et `steam_api64.dll` pour 64 bits) :

1. **Ouvrez un terminal développeur Visual Studio** (x86 pour 32 bits, x64 pour 64 bits) dans le dossier `SteamApiProxy`.
2. **Générez les fichiers .def** (déjà fait avec `dumpbin`, voir les fichiers `exports32.def` et `exports64.def`).
3. **Créez les fichiers .lib à partir des .def** :

   - Pour 32 bits :
     ```cmd
     lib /def:exports32.def /out:steam_api_proxy32.lib /machine:x86
     ```
   - Pour 64 bits :
     ```cmd
     lib /def:exports64.def /out:steam_api_proxy64.lib /machine:x64
     ```

4. **Compilez la DLL proxy** :

   - Pour 32 bits :
     ```cmd
     cl /LD dllmain.cpp steam_api_proxy32.lib /Fe:steam_api.dll
     ```
   - Pour 64 bits :
     ```cmd
     cl /LD dllmain.cpp steam_api_proxy64.lib /Fe:steam_api64.dll
     ```

Les DLL générées (`steam_api.dll` et `steam_api64.dll`) sont prêtes à être utilisées comme proxy dans le dossier du jeu.

## Stack Technique

*   **Proxy DLL**: C++ pour l'interception des appels bas niveau de l'API Steam.
*   **Application de bureau**: C# avec WPF pour l'interface utilisateur et .NET pour la logique applicative.
*   **Base de données**: SQLite pour stocker les informations sur les jeux et les succès localement.
*   **Architecture**: Le projet suit une architecture MVVM (Model-View-ViewModel).

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
