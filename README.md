# Moonshare WIP ALPHA!

![Moonshare Logo oder Screenshot](https://wallpapercrafter.com/th800/172178-moon-girl-luna-anime-manga-morncolour-pink-blue-luminos.jpg)  
*Ein Plugin für Dalamud, das Dateitransfer und Verbindungsfunktionen für FINAL FANTASY XIV ermöglicht.*

---

## Übersicht

**Moonshare** ist ein Dalamud-Plugin für FINAL FANTASY XIV, das es Spielern ermöglicht, sich untereinander mit eindeutigen UserIDs zu verbinden und Dateien sicher auszutauschen. Es bietet eine einfache Benutzeroberfläche zur Verwaltung der Verbindung und des Transfers, inklusive:

- Eindeutige lokale UserID zur Identifikation
- Verbindungsaufbau zu anderen Spielern via deren UserID
- Statusanzeige der Verbindung und Möglichkeit zur Trennung
- Vorbereitung und Verwaltung von Dateiübertragungen

---

## Features

- Intuitive grafische Oberfläche mit ImGui-Integration
- Einfacher Slash-Befehl zur schnellen Anzeige des Plugins: `/moonshare`
- Speicherung der Konfiguration automatisch im Plugin-Ordner
- Saubere Verwaltung von Verbindungen zwischen Spielern
- Erweiterbar und quelloffen für individuelle Anpassungen

---

## Installation

1. Stelle sicher, dass du FINAL FANTASY XIV mit Dalamud und XIVLauncher installiert und mindestens einmal gestartet hast.  
2. Lade die aktuelle Version des Moonshare Plugins von [deinem Release-Archiv] herunter.  
3. Füge die Plugin-DLL in Dalamuds Entwicklungs-Plugin-Verzeichnis ein (über `/xlsettings` → Experimental → Dev Plugin Locations).  
4. Aktiviere das Plugin über `/xlplugins` → Dev Tools → Installed Dev Plugins → Moonshare.  
5. Nutze `/moonshare`, um das Pluginfenster zu öffnen.

---

## Nutzung

- Öffne das Pluginfenster mit `/moonshare`.  
- Deine lokale UserID wird oben angezeigt — teile sie mit anderen Spielern, um eine Verbindung aufzubauen.  
- Gib die UserID eines anderen Spielers in das Eingabefeld ein und klicke „Verbinden“.  
- Verbundene Spieler können Dateien austauschen (je nach Plugin-Implementierung).  
- Über den Button „Verbindung trennen“ kannst du die Verbindung jederzeit beenden.

---

## Entwicklung & Anpassung

Das Plugin ist in C# mit Dalamud API und ImGuiNET geschrieben. Der Quellcode ist modular aufgebaut:

- **Plugin.cs**: Hauptklasse mit Lifecycle und Eventhandling  
- **UserSessionManager.cs**: Verwaltung von UserIDs, Verbindungen und Transfers  
- **Windows/**: ImGui UI-Fenster, z.B. MainWindow.cs für die GUI  

Die Konfigurationsdatei wird automatisch im Pluginordner gespeichert und geladen (`Moonshare/Moonshare.json`).

---

## Voraussetzungen

- XIVLauncher & FINAL FANTASY XIV mit aktiviertem Dalamud-Framework  
- .NET 8 SDK für Entwicklung (nicht zwingend zum Nutzen nötig)  
- Internetverbindung zum Austausch von Dateien (je nach Use-Case)  

---

## Support & Mitmachen

Bei Fragen, Fehlern oder Verbesserungsvorschlägen besuche unseren [Discord-Server](https://discord.gg/holdshift) oder öffne Issues auf GitHub.

---

## Lizenz

Moonshare ist unter der MIT-Lizenz lizenziert. Siehe LICENSE-Datei im Repository.

---

> **Hinweis:** Dieses Plugin ist kein offizielles Square Enix Produkt und steht in keiner Verbindung mit den Entwicklern von FINAL FANTASY XIV.

---

### Danke fürs Nutzen und viel Spaß beim sicheren Teilen mit Moonshare! 🎉
