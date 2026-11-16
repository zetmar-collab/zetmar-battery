# Historia zmian

## 1.3.2 — 2026-11-16
- Włączenie autostartu tworzy skrót w shell:startup; wyłączenie go usuwa.
- Naprawa skrótu ustawia bieżący EXE, --startup, katalog roboczy i ikonę; usuwa stary wpis Run dopiero po zapisaniu skrótu.
- Przycisk otwierania folderu autostartu; rzeczywiste skróty w izolowanych testach.
- 58 testów logiki i 59 testów Windows.

## 1.3.1 — 2026-11-12
- Naprawa autostartu po przeniesieniu EXE: walidacja zapisanej ścieżki i przycisk naprawy.
- Tryb --startup bez otwierania panelu i bez ponownego pokazywania go przez duplikaty.
- Status rejestracji, skrót do ustawień Windows i lokalny log uruchomienia.
- 58 testów logiki, 53 testy Windows oraz test trybu startowego.

## 1.3.0 — 2026-11-08
- Nowy panel z pięcioma sekcjami, podsumowaniem baterii i podglądem widżetu.
- Spójne motywy, kontrolki, fokus, karty i belka okna Windows.
- Historia z legendą, dymkami i tabelą odczytów.
- 102 testów logiki/Windows oraz testy obu motywów i kompaktowego okna.
- Zachowanie konfiguracji 1.2 bez migracji.

## 1.2.0 — 2026-11-07
- Poprawki B1–B7 z audytu: izolacja testów, alarmy i sesje po błędach, niezawodny zapis, synchronizacja i edycja UI.
- Monitor i kotwiczenie, profile wyglądu, postęp estymacji, alarm ładowania i skróty.
- Eksport/import, poprzedni zapis i reset ustawień z walidacją schematu.
- Opcjonalna historia lokalna i odczyt kondycji baterii na żądanie.
- Centralne tłumaczenia, czysty staging, sumy per architektura i runtime .NET 10.0.14.
- 58 testów logiki, 30 testów Windows i izolowane testy GUI.

## 1.1.0 — 2026-10-21
- Dowolne kolory tła oraz tekstu i baterii: paleta RGB, kod HEX, zapis i reset ustawień.
- Automatyczny kontrast tekstu po wybraniu własnego tła.
- Wektorowa ikona baterii z rzeczywistym wypełnieniem i symbolem ładowania.
- Naprawa skryptu aktualizacji tłumaczeń: wielokrotne uruchomienie nie duplikuje konfiguracji projektu.

## 1.0.0 — 2026-10-21
- Widżet baterii i procent w zasobniku Windows 11.
- Odczyt GetSystemPowerStatus, opcjonalna estymacja, alarm, PL/EN i motywy.
- Ustawienia lokalne, opcjonalny autostart, własna ikona i portable x64.
