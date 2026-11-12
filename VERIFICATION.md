# Autostart 1.3.1 — 12 listopada 2026

Przyczyna wykryta na komputerze: wpis HKCU Run wskazywał nieistniejący EXE w starej lokalizacji, podczas gdy aplikacja działała z katalogu ZETMAR-Battery-win-x64-portable. Stary getter sprawdzał tylko istnienie wartości rejestru.

- 58 testów logiki i 53 testy Windows; nowe przypadki: brak wpisu, poprawna ścieżka ze spacjami, brak EXE, inna lokalizacja, błędna komenda, przycisk naprawy i izolowane włączanie/wyłączanie.
- Smoke-Startup: uruchomienie widżetu bez panelu, cichy duplikat --startup, ręczne pokazanie panelu i rejestracja bieżącego EXE przyciskiem naprawy w odrębnym profilu.
- Wszystkie sześć skryptów GUI przeszło na końcowym EXE portable 1.3.1: autostart, podstawowa obsługa, kolory, historia i kondycja, izolacja awarii oraz wygląd w obydwu motywach.
- Zaktualizowano używaną kopię portable i wpis Run; poprzedni EXE, wpis i dodatkowy skrót zachowano w lokalnej kopii. Suma ustawień użytkownika pozostała niezmieniona. Uruchomienie rzeczywistej komendy startowej otworzyło widżet bez panelu, a ręczne uruchomienie pokazało panel.
- Nie wykonywano wylogowania/restartu komputera. Próba polecenia autostartu w działającej sesji nie jest przedstawiana jako rzeczywisty test po zalogowaniu.

---

# Weryfikacja UI/UX 1.3.0 — 8 listopada 2026

- Kompilacja Release: bez błędów i ostrzeżeń.
- 58 testów logiki i 44 testy Windows; pomiary kontrastu >=4,5 dla tekstu i >=3 dla granic pól w jasnym i ciemnym motywie.
- Pięć stron nawigacji, zachowanie wybranej strony i szkicu HEX, obecność stylu fokusu oraz dopasowanie edytora koloru do małego okna.
- Smoke-Design: obydwa motywy i wszystkie strony, okno 900×820/640×600/680×400, natywna semantyka dropdownu i zapis wybranej czcionki 22 DIP.
- Testy GUI palety, restartu, historii i kondycji zostały dopasowane do nowej nawigacji. Historia na zrzutach jest syntetyczna; odczyt kondycji jest rzeczywisty.
- Zrzuty przeglądu, edycji, pozostałych stron i wykresu sprawdzono wizualnie.

Nie wykonano przełączenia systemowego DPI i wysokiego kontrastu ani pełnego testu czytnika ekranu. Kod respektuje te ustawienia, lecz nie przedstawiamy tego jako przeprowadzonego testu sprzętowego. Szczegóły: UI_UX_1.3.md.

---

# Weryfikacja wydania 1.2.0 — 7 listopada 2026

- Kompilacja Release bez błędów i ostrzeżeń.
- 57/58 testów czystej logiki: mapowanie, formaty, estymacja, sesje, alarmy, geometria monitorów i retencja historii.
- 30/30 testów Windows: atomowy zapis, kopie i uszkodzenie konfiguracji, zablokowany plik, schemat importu, UI i szkice HEX, profile, grafika baterii, historia na dysku, XML i zakaz zewnętrznych encji.
- Izolowany test kolorów GUI: paleta RGB, HEX, odrzucenie błędu, zapis, restart i reset.
- Test celowej awarii GUI: sprzątanie procesu i niezmieniona suma konfiguracji użytkownika.
- Test funkcji GUI: rzeczywisty odczyt kondycji oraz wykres historii testowej. Wynik sprzętowy: pojemność projektowa 70000 mWh, pełna 64070 mWh, 170 cykli. Dane dostępne na tym komputerze, nie gwarancja dla innych modeli.
- Zrzuty panelu i wykresu sprawdzone wizualnie; historia na zrzucie pochodzi z syntetycznych próbek testowych.
- Testy używają osobnego katalogu i mutexa; ustawienia użytkownika pozostają nienaruszone.

Nie wykonano rzeczywistego tygodniowego pomiaru, ARM64, fizycznego odłączenia monitora, pełnego zestawu DPI/auto-hide, restartu Explorer i wylogowania dla autostartu. Te przypadki wymagają ręcznej weryfikacji na docelowym Windows. Zgodność .NET nie zastępuje testów sprzętowych. EXE pozostaje bez podpisu Authenticode.

Końcowy samodzielny EXE x64 1.2.0.0 z runtime Microsoft.NETCore.App i WindowsDesktop.App 10.0.14 przeszedł również wszystkie cztery skrypty GUI na artefakcie portable. Sprawdzono ponowne uruchomienie bez zapisu historii przez drugą instancję. Próbka z otwartym panelem przez 10 s: private memory 111,3 MiB, working set 181,8 MiB, przyrost CPU 0,03125 s. To krótka próbka, nie test długoterminowy.

Mapowanie wdrożenia: WDROZENIE_AUDYTU.md.

---

## Historyczne wyniki poprzednich wydań

# Weryfikacja wydania 1.1.0

## Aktualizacja 1.1.0

- Kompilacja Release: bez ostrzeżeń i błędów.
- Test działającego panelu (`scripts/Smoke-Colors.ps1`): otwarcie i anulowanie natywnej palety RGB, zmiana tła i tekstu/baterii kodem HEX, normalizacja kodu, odrzucenie nieprawidłowego koloru bez nadpisania poprawnego, ponowne uruchomienie i odczyt zapisanych kolorów, reset do automatycznych kolorów. Ustawienia użytkownika są przywracane po teście.
- Sprawdzono wizualnie zrzut rzeczywistego widżetu: granatowe tło, żółty tekst, bateria wypełniona zgodnie z odczytem 73%, czytelny procent i czas. Zrzut: `artifacts/widget-colors.png`.
- Własna bateria jest wektorowa, skaluje się z rozmiarem tekstu; stany ładowania i braku danych mają dodatkowe symbole. Wysoki kontrast korzysta z systemowych kolorów.
- Test ustawień kolorów wykonano także na końcowym EXE portable 1.1.0. Zmiana DPI i próba ładowania na sprzęcie pozostają do osobnej weryfikacji.

## Weryfikacja bazowa 1.0.0

Środowisko: Windows 11 (build 26200), x64; SDK 10.0.401. Data: 21.10.2026.

## Wykonano

- Kompilacja Release: 0 błędów, 0 ostrzeżeń.
- 32 automatyczne testy logiki: 100%, 1%, brak baterii, 255, błędy API, flagi bitowe, zasilacz bez ładowania, ładowanie, nieznany czas, formaty PL/EN, minuty, estymacja, stały procent, skoki, przerwy i reset po wznowieniu. Wszystkie przeszły (`dotnet run --project tests/Zetmar.Tests -c Release`).
- Uruchomienie aplikacji WPF na rzeczywistym laptopie; odczyt 80%, praca na baterii, czas Windows około 4 godzin. Wynik zmienia się wraz z obciążeniem.
- Kontrola UI Automation: okno widżetu i panel z rzeczywistym procentem, źródłem czasu i etykietami ustawień. Oryginalna ikona widoczna w nagłówku panelu. Zrzut panelu sprawdzony wizualnie (czytelne wartości, etykiety, przewijanie).
- Uruchomienie drugiej instancji: po zamknięciu panelu drugi proces kończy się poprawnie i ponownie otwiera panel pierwszej instancji.
- Zakończenie przyciskiem panelu: proces kończy się poprawnie.
- Pomiar końcowego, samodzielnego EXE x64 z otwartym panelem w spoczynku przez 10 s: private memory 83,8 MiB, working set 151,8 MiB, 0,015625 s CPU w 10 s. Także końcowy EXE przeszedł test panelu, ponownego uruchomienia i zakończenia. Wyłączono kompresję wewnątrz pojedynczego EXE, aby ograniczyć pamięć potrzebną do dekompresji zestawów; ZIP zapewnia kompresję na czas przenoszenia. To krótka próbka na tym laptopie, nie gwarancja typowego zużycia, nie pomiar po długim działaniu ani test całkowitej bezczynności systemu.

Skrypt odtwarzający: `scripts/Smoke-Test.ps1`. Zrzut panelu, etykiety UI Automation i próbka pomiaru są w `artifacts/` w projekcie.

## Pozostała weryfikacja ręczna

Nie wykonano pełnego testu cyklu rozładowania, fizycznego podłączania zasilacza, uśpienia/hibernacji, restartu Explorer, wylogowania dla autostartu, wizualnej kontroli menu i cyfry zasobnika, zmiany motywu Windows, przełączania języka w działającym panelu, DPI 125/150/200%, wysokiego kontrastu, dwóch monitorów i odłączania monitora, auto-hide paska, gry pełnoekranowej, alarmu na rzeczywistym progu i ARM64. Obsługa jest zaimplementowana, lecz wymaga tych prób na docelowej konfiguracji. Nie restartowano powłoki ani sesji użytkownika na potrzeby testu.

Lista kontrolna na laptopie:
1. Porównać procent i źródło czasu z Windows; podłączyć i odłączyć zasilacz.
2. Sprawdzić kliknięcie i menu zasobnika, przeciąganie, blokadę i ukrywanie widżetu.
3. Zmienić język, motyw i rozmiar; sprawdzić klawiaturę Tab i Spacja oraz wysoki kontrast.
4. Włączyć autostart jawnie, zalogować się ponownie, następnie wyłączyć.
5. Zrestartować Explorer i potwierdzić odbudowę ikony.
6. Przełączyć DPI 100/125/150/200%, monitor, rozdzielczość i odłączyć monitor.
7. Sprawdzić auto-hide paska i pełny ekran (widżet znika w ciągu ok. 2 s).
8. Wznowić po uśpieniu; potwierdzić świeży odczyt i ponowne zbieranie próbek estymacji.
9. Włączyć alarm, sprawdzić pojedyncze powiadomienie i wyłączyć go.
10. Zakończyć z menu; sprawdzić zniknięcie procesu i ikony.
