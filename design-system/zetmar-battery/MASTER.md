# ZETMAR Battery — system wizualny

8 listopada 2026. Interfejs WPF, Windows 11, lokalne narzędzie użytkowe. Projekt korzysta ze skilla ui-ux-pro-max: zweryfikowane dopasowanie stylu Minimalism & Swiss Style oraz wskazówki WPF dotyczące klawiatury, zasobów i wysokiego kontrastu. Generator wskazał także układ marketingowy, który pominięto. W bazie nie znaleziono dedykowanego profilu produktu dla narzędzia systemowego; układ aplikacji jest adaptacją zasad natywnych, a nie wzorcem landing page.

## Układ

- Stała nawigacja: Przegląd, Widżet, Powiadomienia, Dane baterii, Ustawienia.
- Jedna aktywna strona i jeden obszar przewijania; nawigacja i zamykanie aplikacji pozostają dostępne.
- Przegląd: duży procent, stan, pozostały czas i sesja; jeden główny przycisk zmiany wyglądu.
- Widżet: podgląd rzeczywistych kolorów i danych, edycja HEX/paleta, zawartość i położenie. Podgląd skaluje się w dół w małym oknie.
- Funkcje zaawansowane w nazwanych kartach rozwijanych; zachowanie ich stanu, szkiców koloru i pozycji przewijania.
- Okno domyślne do 900×820 DIP, minimum 640×400 lub mniej na ekranie, którego obszar roboczy jest mniejszy. Kontrola zrzutów również przy 640×600 i 680×400. Bez wdrażania mobilnych wzorców nawigacji.

## Semantyczne kolory

| Token | Jasny | Ciemny |
|---|---|---|
| Canvas | #F3F6F8 | #141B24 |
| Card | #FFFFFF | #1D2834 |
| Text | #152B37 | #F0F6FA |
| Secondary text | #536879 | #B4C4CF |
| Accent | #006B52 | #80E5C5 |
| Accent soft | #E0F3EC | #203F37 |
| On accent | #FFFFFF | #102A22 |
| Input | #F5F7F9 | #263440 |
| Input outline | #7B8E9B | #6C8192 |

Kolory widżetu pozostają ustawieniami użytkownika. Przy niskim kontraście podgląd pokazuje tekstową wskazówkę; nie blokuje wyboru. W wysokim kontraście Windows kolory systemowe mają pierwszeństwo; zaznaczenia używają pary Highlight/HighlightText.

## Typografia i przestrzeń

Segoe UI z Windows, bez pobierania fontów. Procent 58 DIP, tytuł strony 26, karta 16, treść i pola 14, podpisy 12–13. Linie tekstu około 1,45 wysokości fontu. Odstępy 8/12/16/20/24 DIP. Karty: promień 12, kontrolki 8, podgląd widżetu 10. Kontrolki edycji i przyciski minimum 40 DIP; checkbox i nawigacja około 44 DIP. Jest to interfejs desktopowy, nie deklaracja zgodności z mobilnymi jednostkami pt/dp.

## Interakcja i dostępność

- Zachowane natywne typy kontrolek WPF, role Automation, zaznaczenia i obsługa klawiatury.
- Widoczny fokus bez przesuwania układu; stany hover/pressed/disabled zmieniają wygląd, nie rozmiar.
- Bez własnych animacji i bez animacji popupów; nie trzeba odtwarzać ruchu przy reduced motion.
- Etykiety nad polami, opis błędu przy HEX i HelpText. Stan baterii opisany tekstem, nie samym kolorem.
- Wektorowe ikony nawigacji o jednolitej grubości; brak emoji i nowych bibliotek.
- Wykres: linia ciągła dla baterii, przerywana dla zasilacza, legenda tekstowa i tabela ostatnich 100 pomiarów.
- Standardowe przyciski okna, zmiana rozmiaru i zamykanie pozostają obsługiwane przez Windows. Paleta belki zmieniana oficjalnymi atrybutami DWM.

## Weryfikacja

Smoke-Design: pięć stron w obu motywach, szerokość 640 i 900, rzeczywiste rozwijane menu rozmiaru tekstu, zapis wyboru 22 DIP i podgląd. Testy Windows mierzą kontrast tekstu >=4,5:1 i obramowań pól >=3:1 w obu motywach oraz sprawdzają nawigację i zachowanie edycji. Zrzuty są w artifacts/design-*.png. Nie deklarujemy pełnego audytu czytnikiem ekranu ani fizycznego przełączenia DPI lub wysokiego kontrastu.
