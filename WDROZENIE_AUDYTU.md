# Wdrożenie audytu — ZETMAR Battery 1.2.0

7 listopada 2026. Kolejność: zabezpieczenie testów → logika i pliki → UI → utrzymanie/publikacja → funkcje.

## Poprawki

| Audyt | Zmiana | Weryfikacja |
|---|---|---|
| B1 | Testy używają oddzielnego `--data-dir` i mutexa. Nie zapisują ani nie odtwarzają pliku użytkownika. Sprzątanie ma awaryjne zatrzymanie tylko własnego procesu. | Test izolacji z wymuszonym wyjątkiem, porównanie sumy konfiguracji użytkownika, sprawdzenie braku pozostawionego procesu. |
| B2 | Osobna klasa BatterySession zachowuje sesję i zabezpieczenie alarmu podczas Unknown/Unavailable. | Testy API error/recovery, histerezy, zasilacza, wznowienia i ładowania. |
| B3 | Wynik zapisu, komunikat i ponowienie; atomowa podmiana, kopia ostatniego poprawnego pliku i odczyt awaryjny. | Zablokowany plik, nienaruszona poprzednia konfiguracja, odzyskanie kopii, skuteczne ponowienie. |
| B4 | Automatyczne katalogi testów i wyników oraz wspólne sprzątanie. | Uruchomienia izolowane i scenariusz celowej awarii. |
| B5 | Panel synchronizuje checkboxy po zmianach z innych miejsc. | Zmiana blokady poza panelem i odświeżenie kontrolek. |
| B6 | Zachowanie szkiców HEX, przewijania, fokusu i rozwiniętych sekcji przy przebudowie. | Test zachowania HEX i sekcji; fokus/przewijanie implementowane, wymagają też prób klawiaturą. |
| B7 | Sukces usuwa błąd i przywraca dymek. Widoczny opis walidacji i HelpText dla dostępności. | Błędny HEX, poprawienie i sprawdzenie komunikatu. |
| R1 | Odczyt autoukrywanego paska dla monitora i bezpieczne pozycjonowanie. | Testy czystej geometrii; fizyczna zmiana auto-hide, DPI i monitorów pozostaje ręczna. |

## Utrzymanie

- Wstrzykiwane źródło baterii i TimeProvider, oddzielone odczyty i renderowanie.
- Usunięte podwójne zapisy/odczyty przy zmianie estymacji.
- Centralne zasoby PL/EN w Zetmar.Core, wspólne dla UI, stanów i zasobnika. Stare pliki zasobów w katalogu aplikacji nie są już osadzane ani używane.
- Nowy katalog staging dla każdego wydania; ZIP zawiera wyłącznie pliki tego wydania.
- Oddzielne sumy SHA-256 x64/ARM64 oraz manifest zbiorczy.
- Historia jest przycinana również po wyłączeniu zbierania; druga instancja nie zapisuje historii podczas kończenia.
- Wersja 1.2.0 zachowuje zgodność z konfiguracją 1.1 i dodaje walidację schematu.
- Runtime publikacji .NET 10.0.14 zgodnie z aktualną sprawdzoną polityką Microsoft.

## Funkcje

Wdrożono wszystkie osiem propozycji z raportu:
1. Wybór monitora, zakotwiczenie w narożniku i przyciąganie do krawędzi.
2. Profile wbudowane i własne oraz warianty zawartości widżetu.
3. Postęp obserwacji estymatora: minuty i spadek procentu.
4. Alarm osiągnięcia poziomu ładowania, niezależny od alarmu rozładowania.
5. Eksport, walidowany import, przywracanie poprzedniego zapisu i reset.
6. Opcjonalne skróty B/W z wyborem modyfikatorów i wykrywaniem zajętej kombinacji.
7. Opcjonalna lokalna historia 7 dni z wykresem 24 h/7 dni i podsumowaniem obserwacji. Używa ograniczonego JSON; nie wprowadzono bazy ani migracji.
8. Kondycja na żądanie z XML raportu Windows; obsługuje brak danych i kilka baterii. Nie zgaduje pojemności ani cykli.

## Testy i granice

58 testów logiki i 30 testów Windows. Testy GUI: kolory i restart, pojedyncza instancja i zakończenie, wymuszona awaria z zachowaniem danych użytkownika, rzeczywisty odczyt kondycji oraz wykres przykładowej historii. Zrzuty panelu i wykresu sprawdzono wizualnie. Pełne wyniki wydania w VERIFICATION.md.

Wykres z testu GUI korzysta z danych syntetycznych. Wynik odczytu kondycji pochodził z fizycznej baterii tego komputera. Nie wykonano pełnego siedmiodniowego pomiaru, prób na ARM64, fizycznego odłączania monitorów, przełączania DPI ani restartu Explorer. Rejestracja skrótów i konflikt z inną aplikacją wymagają także ręcznej próby na docelowej konfiguracji. Brak tych prób nie jest przedstawiany jako sukces.

Źródła sprzed zmian zarchiwizowano lokalnie w artifacts/backups/source-before-audit-fixes.zip. Nie usuwano źródeł ani nie wykonywano migracji bazy danych. Autostart i nowe opcje nie są włączane samoczynnie w profilu użytkownika.
