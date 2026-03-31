# AI generovaný prostor vytvořený v rámci 3D hry

  Dungeon shooter ve first-person perspektivě, kde je každá mapa vygenerována umělou inteligencí pomocí GPT-4o-mini.

  Požadavky

  - Unity 2022.3.x — stáhnout přes https://unity.com/download
  - OpenAI API klíč — získat na https://platform.openai.com/api-keys (nutný pro generování map)

  Instalace

  1. Naklonuj repozitář:
  git clone https://github.com/Lemon6226/AIGeneratedMapping.git
  2. Otevři Unity Hub, klikni na Add project from disk a vyber naklonovanou složku.
  3. Počkej, než Unity naimportuje všechny soubory (může trvat několik minut).
  4. V Project okně přejdi do Assets/Resources/ a vytvoř konfigurační soubor:
    - Pravý klik -> Create -> OpenAI -> OpenAI Configuration
    - Do pole Api Key vlož svůj OpenAI API klíč
  5. Otevři scénu Assets/Scenes/MainMenu.unity a stiskni Play.

  Ovládání

  - WASD — pohyb
  - Shift — sprint
  - Myš — rozhlížení
  - Levé tlačítko myši — střelba
  - R — přebití
  - E — sebrat předmět
