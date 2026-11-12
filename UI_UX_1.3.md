# Poprawa UI/UX — wersja 1.3.0

8 listopada 2026. Zastosowano skill ui-ux-pro-max, wskazówki WPF oraz zweryfikowany styl minimalistyczny. Dedykowanego profilu produktu dla widżetu baterii w bazie nie znaleziono; nie zastosowano marketingowego układu hero/CTA ani zewnętrznych fontów sugerowanych przez generator.

## Co wymagało poprawy

Wersja 1.2 zawierała jeden bardzo długi formularz. Informacje o baterii, wygląd, alarmy i kopie konfiguracji miały podobną hierarchię. Domyślne kontrolki WPF pozostawały częściowo jasne w ciemnym motywie. Brakowało podglądu wyglądu, spójnych kart, legendy wykresu i jasnej nawigacji.

## Zmiany

1. Pięć sekcji w stałej nawigacji: Przegląd, Widżet, Powiadomienia, Dane baterii i Ustawienia.
2. Przegląd z dużym procentem, graficzną baterią, czasem pozostałym, sesją i źródłem estymacji.
3. Podgląd widżetu, próbki kolorów i wskazówka przy niskim kontraście. Zachowano dowolny wybór kolorów, paletę, HEX i profile.
4. Wspólne karty, odstępy, obramowania, fonty i kolory semantyczne; spójne jasne/ciemne przyciski, pola, dropdowny, checkboxy i przewijanie.
5. Jednolite ikony wektorowe; widoczny fokus; brak animacji i zmiany rozmiaru kontrolek po naciśnięciu.
6. Dopasowana belka okna z zachowaniem natywnych przycisków, przeciągania i zmiany rozmiaru. Wysoki kontrast stosuje kolory Windows.
7. Historia z kartami podsumowania, legendą i różnymi liniami baterii/zasilacza, dymkami wartości i tabelą ostatnich 100 odczytów.
8. Zachowana edycja HEX, aktywna sekcja, stan rozwinięcia i przewijanie po przebudowie panelu. Naprawiono ponowne używanie kontrolek baterii, które powodowało wyjątek WPF.

System wizualny: design-system/zetmar-battery/MASTER.md. Kod: UiTheme.cs, UiStyles.xaml, DetailsWindow.cs, HistoryWindow.cs; niewielkie zmiany prezentacji w App.cs i Native.cs. Bez nowych zależności i zmian formatu ustawień.

## Sprawdzenie

- 58 testów logiki i 44 testy Windows (102 łącznie), w tym kontrast tekstu i kontrolek w obu motywach, obecność fokusu i zachowanie nawigacji/kolorów.
- Izolowane testy GUI: pięć stron w obu motywach, 640×600, 680×400 oraz 900×820, wybór rozmiaru tekstu z dropdownu i zapis po przebudowie.
- Testy palety RGB, restartu, historii, rzeczywistego odczytu kondycji i sprzątania po celowej awarii testu.
- Zrzuty jasnego i ciemnego przeglądu, edytora, pozostałych stron oraz historii sprawdzono wizualnie. Wykres w teście używa danych syntetycznych.

Nie przełączano systemowego DPI ani wysokiego kontrastu użytkownika i nie wykonano pełnego testu czytnikiem ekranu. Obsługa została zachowana/ulepszona w kodzie, lecz wymaga dodatkowych prób na konfiguracji docelowej. Ustawienia użytkownika, autostart, alarmy i sposób odczytu baterii pozostają zgodne z poprzednim wydaniem.
