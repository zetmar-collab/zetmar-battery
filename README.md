# ZETMAR Battery 1.3.1

Natywna aplikacja C# / WPF dla Windows 11 x64, .NET 10 LTS. Działa lokalnie, bez konta, serwera i telemetrii. Samodzielny EXE zawiera runtime .NET 10.0.14; nie wymaga instalowania .NET ani uprawnień administratora. Wersja ARM64 może być zbudowana, lecz nie została sprawdzona na urządzeniu ARM.

## Uruchomienie i podstawowa obsługa

Gotowy program: [pobierz najnowsze wydanie](https://github.com/zetmar-collab/zetmar-battery/releases/latest). ZIP portable zawiera EXE, dokumentację i licencje; dostępny jest również sam EXE. Kod źródłowy: [zetmar-collab/zetmar-battery](https://github.com/zetmar-collab/zetmar-battery).

Uruchom `dist/ZETMAR-Battery-win-x64/ZETMAR Battery.exe` lub rozpakuj ZIP portable i uruchom EXE. Dwuklik widżetu lub kliknięcie ikony zasobnika otwiera szczegóły i ustawienia. Panel można zamknąć; aplikacja pozostaje w zasobniku. Polecenie „Zakończ” usuwa ikonę i kończy proces.

Widżet jest własnym oknem nad paskiem, a nie natywnym rozszerzeniem paska zadań. Procent jest również prezentowany jako liczba w zasobniku; czas i pełny stan są w dymku. Aplikacja nie modyfikuje Explorer ani powłoki. Jeśli Windows ukrył ikonę: Ustawienia Windows → Personalizacja → Pasek zadań → Inne ikony zasobnika systemowego lub przeciągnij ją z menu `^` przy zegarze. Windows decyduje o jej widoczności.

EXE nie ma podpisu Authenticode. Windows może pokazać informację o nieznanym wydawcy. Sumy SHA-256 znajdują się w `dist/SHA256SUMS-win-x64.txt` i zbiorczym `SHA256SUMS.txt`.

## Nowy interfejs 1.3

Panel ma pięć sekcji: **Przegląd**, **Widżet**, **Powiadomienia**, **Dane baterii** i **Ustawienia**. Po otwarciu widzisz najważniejsze dane baterii. Wygląd i kolory zmienisz w sekcji Widżet, gdzie dostępny jest również podgląd. Preferencje języka, motywu i uruchamiania przeniesiono do Ustawień. Widok historii ma legendę, dymki i tabelę pomiarów.

Zmiany opisuje `UI_UX_1.3.md`, a reguły wyglądu `design-system/zetmar-battery/MASTER.md`.

## Wygląd i położenie

- Polski/angielski, motyw jasny/ciemny/systemowy i obsługa wysokiego kontrastu.
- Dowolny kolor tła oraz tekstu/baterii: „Wybierz…” otwiera paletę RGB; można też wpisać `#RRGGBB` i kliknąć „Zastosuj”. Puste pole przywraca kolor automatyczny. Gdy wybierzesz tylko tło, tekst automatycznie dobiera czarny lub biały kolor. Wysoki kontrast Windows ma pierwszeństwo.
- Bateria wektorowa pokazuje aktualne wypełnienie; błyskawica oznacza ładowanie, kropki nieznany procent, krzyżyk brak baterii lub brak danych.
- Widoki: bateria + procent + czas, bateria + procent albo sama bateria z pełnym dymkiem.
- Profile wbudowane i do 10 własnych profili wyglądu. Wpisz nazwę i zapisz bieżący wygląd; ta sama nazwa zastępuje zapisany własny profil.
- W sekcji „Położenie i monitor” wybierz ekran i narożnik. W trybie swobodnym można przeciągać widżet i włączyć przyciąganie do krawędzi. Blokada uniemożliwia przeciąganie. Odłączenie monitora powoduje umieszczenie okna na dostępnym ekranie.
- Pozycjonowanie uwzględnia obszar roboczy i odczyt autoukrywanego paska dla danego monitora. Nie rezerwuje dodatkowego pasa ekranu. Pełny ekran na monitorze widżetu ukrywa go w ciągu około 2 s. Próby wielomonitorowe i różne DPI nadal wymagają sprawdzenia na docelowym sprzęcie.

## Dane i szacowanie

Odczyt `GetSystemPowerStatus` co około 30 s i po zdarzeniach zasilania. Dane są zbiorcze dla baterii widocznych w Windows. Nieznany procent (`255`), nieznany czas (`0xFFFFFFFF`), flaga stanu `255`, brak baterii i błąd API są rozróżniane. Sam zasilacz nie oznacza ładowania. `BatteryFullLifeTime` nie jest traktowany jako czas do pełna.

Pierwszeństwo ma przybliżony czas Windows. Opcjonalny szacunek aplikacji potrzebuje co najmniej 10 minut obserwacji i spadku 2 punktów procentowych. Wykorzystuje regresję z okna do 45 minut, odrzuca skoki i przerwy; resetuje próbki po wznowieniu, zmianie zasilania lub błędzie danych. Panel pokazuje postęp obserwacji oraz źródło szacunku. `~` oznacza przybliżenie zależne od obciążenia. Przy wyłączonym własnym szacunku i braku czasu Windows pokazuje „Czas niedostępny”.

Czas sesji oznacza zaobserwowany okres od pracy na baterii podczas działania aplikacji, a nie czas pozostały. Chwilowy błąd odczytu nie zeruje sesji ani zabezpieczenia alarmu. Wznowienie rozpoczyna nową sesję pomiarową.

## Alarmy, skróty i autostart

Alarm niskiego poziomu i alarm osiągnięcia wybranego poziomu ładowania są osobno konfigurowane, domyślnie wyłączone w nowej konfiguracji. Histereza zapobiega ciągłym powtórzeniom. Alarm ładowania jest przypomnieniem; nie steruje sprzętowym limitem ładowania. Windows może wyciszać powiadomienia.

Opcjonalne skróty: wybrany zestaw Ctrl+Alt / Ctrl+Shift / Alt+Shift wraz z B otwiera panel, z W przełącza widżet. Jeśli kombinacja jest zajęta, panel pokazuje informację; aplikacja nie przejmuje cudzej rejestracji. Skróty są domyślnie wyłączone i zwalniane przy zamknięciu.

Autostart jest domyślnie wyłączony. Jawne zaznaczenie przełącznika zapisuje bieżącą ścieżkę EXE w `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ZetmarBattery`. Odznaczenie usuwa wpis. Po przeniesieniu EXE użyj „Napraw autostart” w Ustawieniach. Panel wykrywa brak pliku, inną lokalizację i nieprawidłową komendę, zamiast uznawać sam wpis rejestru za działający autostart. Przycisk naprawy zapisuje ścieżkę aktualnie uruchomionego EXE z argumentem `--startup`. W tym trybie program pokazuje widżet bez panelu, a druga instancja startowa kończy się bez otwierania okna. Autostart jest uruchomieniem po zalogowaniu użytkownika, a nie usługą przed logowaniem. Windows może opóźniać uruchomienie; jego osobny przełącznik można otworzyć przyciskiem „Autostart w ustawieniach Windows…”. Import i reset ustawień aplikacji nie zmieniają wpisu autostartu Windows.

## Ustawienia i kopie

`%LOCALAPPDATA%\ZetmarBattery\settings.json`; ostatni poprawny zapis w `settings.json.bak`. Zapis odbywa się przez plik tymczasowy i atomową podmianę. Uszkodzony plik główny powoduje próbę odczytu kopii. Przy błędzie zapisu wyświetlana jest informacja, a poprzedni plik pozostaje nienaruszony; można ponowić zapis w panelu.

Sekcja „Kopia i przenoszenie ustawień”: eksport/import JSON, przywrócenie poprzedniego zapisu i wartości domyślnych. Import waliduje schemat, zakresy i kolory; plik większy niż 1 MiB lub z przyszłą wersją schematu jest odrzucany. Starsza konfiguracja 1.1 jest obsługiwana bez migracji bazy danych.

Log startów (tryb i ścieżka EXE) oraz błędów: `app.log` i jedna kopia `.old`; rotacja po przekroczeniu 256 KiB przy kolejnym wpisie. Bez telemetrii. Ustawienia nie są zapisywane przy każdym odczycie baterii.

## Historia i kondycja

Historia jest domyślnie wyłączona. Po włączeniu zbiera próbki co około 30 s, zapisuje asynchronicznie co około 5 min i przy zakończeniu. Maksymalnie 7 dni / 20 500 próbek w lokalnym `history.json`, bez bazy i bez chmury. Wyłączenie zatrzymuje zbieranie. Wykres 24 h / 7 dni oraz podsumowanie obejmują tylko zaobserwowane okresy; nie odtwarzają zużycia, gdy aplikacja nie działała. Przerwy i brak danych nie są łączone w ciągły pomiar.

„Odczytaj kondycję baterii” uruchamia na żądanie systemowy `powercfg /batteryreport /xml`, bez zmiany planów zasilania. Raport `battery-health.xml` pozostaje lokalnie. Panel pokazuje pojemność projektową, pojemność przy pełnym naładowaniu i cykle, jeśli podaje je sterownik. Kondycja to stosunek tych pojemności, nie aktualne naładowanie. Brakujące dane są oznaczane jako niedostępne; nie są zgadywane.

## Architektura

`WindowsBatterySource + TimeProvider → BatteryMapper / BatterySession / DischargeEstimator → App → Widget / Details / Tray`

- `src/Zetmar.Core`: stany, alarmy i sesje, formatowanie, geometria, historia, wspólny katalog tłumaczeń `Resources/pl.json` i `en.json`.
- `src/Zetmar.Battery`: integracja Windows, WPF, ustawienia z kopiami, historia na dysku i odczyt kondycji.
- `tests/Zetmar.Tests`: testy czystej logiki.
- `tests/Zetmar.WindowsTests`: testy WPF, plików, renderowania i odczytu XML w osobnym profilu.
- `scripts`: izolowane testy GUI, walidacja tłumaczeń, generowanie oryginalnej ikony i publikacja.

Zmiany wyglądu nie odczytują ponownie baterii ani nie dopisują próbek estymacji. Źródło baterii i zegar można podmieniać w konstruktorze aplikacji. Nie dodano zewnętrznych zależności NuGet.

## Budowanie i testy

Windows, SDK .NET 10.0.401 lub nowszy SDK 10. Internet jest potrzebny do pierwszego pobrania runtime przy publikacji; aplikacja działa offline.

```powershell
dotnet build src/Zetmar.Battery -c Release
dotnet run --project tests/Zetmar.Tests -c Release
dotnet run --project tests/Zetmar.WindowsTests -c Release
python scripts/Extract-Translations.py
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Smoke-Test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Smoke-Colors.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Isolation.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Smoke-Features.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Smoke-Design.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Smoke-Startup.ps1
```

Testy GUI tworzą własny katalog w `artifacts/test-runs`, używają `--data-dir` i odrębnego mutexa. Nie zmieniają profilu ani autostartu użytkownika. Nawet przy awarii zatrzymują wyłącznie swój proces. `Smoke-Features` używa przykładowej historii, ale odczytuje rzeczywistą kondycję sprzętu. Brak takiego odczytu może spowodować niepowodzenie tego testu na komputerze bez odpowiedniego sprzętu.

Publikacja z nowego staging, testami, dokumentacją, ZIP i sumami per architektura:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Publish.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Publish.ps1 -Runtime win-arm64
```

Ręczna publikacja samego EXE:

```powershell
dotnet publish src/Zetmar.Battery -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=10.0.14 -o dist/ZETMAR-Battery-win-x64
```

Nowe wydania samodzielnego EXE powinny zawierać aktualną poprawkę .NET 10. Zestawy są nieskompresowane wewnątrz EXE, aby ograniczyć pamięć potrzebną do dekompresji; ZIP zapewnia kompresję do przenoszenia. .NET może wypakować biblioteki natywne do katalogu tymczasowego.

## Licencja i weryfikacja

Kod i oryginalna ikona: MIT (`LICENSE`). .NET/WPF/Windows Forms: MIT i dołączone informacje o komponentach. Czcionka Segoe UI pochodzi z Windows i nie jest dołączana.

Mapowanie zmian do audytu: `WDROZENIE_AUDYTU.md`. Wyniki testów i ograniczenia: `VERIFICATION.md`.

Oficjalne źródła: [GetSystemPowerStatus](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getsystempowerstatus), [SYSTEM_POWER_STATUS](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-system_power_status), [obszar powiadomień](https://learn.microsoft.com/en-us/windows/win32/shell/notification-area), [autoukrywanie na monitorze](https://learn.microsoft.com/en-us/windows/win32/shell/abm-getautohidebarex), [powercfg](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options), [.NET LTS](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
