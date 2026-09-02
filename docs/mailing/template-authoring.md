# Podręcznik tworzenia maili i snippetów — GenoDev.BusinessTracker

Ten dokument jest samodzielnym kontekstem dla osoby lub czatu AI tworzącego wiadomości w GenoDev.BusinessTracker. Można przekazać cały plik wraz z poleceniem napisania lub poprawienia maila. Nie trzeba udostępniać kodu aplikacji. Opis dotyczy rzeczywistych możliwości edytora, a nie ogólnej składni Handlebars, Liquid czy innych silników.

## 1. Instrukcja dla AI korzystającego z tego pliku

- Twórz treści po polsku, chyba że użytkownik zamówi inny język.
- Ustal z polecenia, czy wynikiem ma być **szablon**, **snippet**, czy **gotowy mail do okna wysyłki**. Gdy użytkownik prosi o wielokrotnego użytku mail ze zmiennymi, przygotuj szablon.
- Dla szablonu zwróć osobno nazwę, temat jako zwykły tekst i treść jako HTML do wklejenia. Dla snippetu zwróć nazwę, unikalny klucz i HTML. Nie wklejaj ogrodzeń Markdown do edytora.
- Korzystaj wyłącznie ze zmiennych i konstrukcji wymienionych tutaj. Nie dopisuj helperów, filtrów, `else`, porównań ani zagnieżdżonych bloków tego samego rodzaju.
- Nie zakładaj, że przykładowy snippet już istnieje. Jeśli proponujesz snippety, dostarcz ich kompletne definicje oraz kolejność zapisania zależności. Jeśli masz użyć istniejących, potrzebujesz ich kluczy i treści od użytkownika.
- Nie wymyślaj danych firmy, konta bankowego, terminów, linków, logo ani informacji o płatności lub dostawie. Brakujące dane stałe wskaż użytkownikowi poza HTML; nie twórz dla nich fikcyjnych zmiennych.
- Zachowuj przekazane obrazy Base64 bez zmian. Nie generuj fikcyjnego Base64 ani identyfikatorów `cid:`. Jeśli potrzebne jest nowe logo, wskaż miejsce do użycia przycisku „Wstaw obraz”.
- Dla gotowego maila do okna wysyłki używaj już konkretnych danych, bez składni szablonu. To okno nie podstawia zmiennych przy wysyłce.

## 2. Co jest czym i kiedy podstawiane są dane

| Element | Zawartość i działanie |
| --- | --- |
| Szablon | Nazwa, temat, HTML, opcjonalnie przypisane konto nadawcze i zwykłe załączniki. Może zawierać zmienne, a w HTML także warunki, pętle i odwołania do snippetów. |
| Snippet | Fragment HTML wielokrotnego użytku, np. stopka lub tabela produktów. Ma nazwę, klucz, opcjonalny opis i oznaczenie aktywności. Nie ma własnego tematu ani listy załączników. |
| Podgląd z zamówieniem | Podstawia dane wybranego zamówienia do aktualnie edytowanego HTML. Odwołania do snippetów pobiera z zapisanych, aktywnych snippetów. |
| Podgląd „Bez zamówienia — surowy HTML” | Wyświetla HTML bez rozwijania zmiennych, warunków, pętli i snippetów. |
| Okno tworzenia wiadomości dla zamówienia | Zastosowanie szablonu rozwija jego temat i HTML na podstawie zamówienia oraz konta SMTP i kopiuje załączniki. Dalej edytujesz wynik. |
| Wysyłka / kolejka | Zapisuje aktualny temat, HTML i załączniki z okna wysyłki. Nie uruchamia ponownie silnika szablonów. |
| Ponowna wysyłka z historii | Otwiera zapisany temat i HTML starej wiadomości. Nie stosuje ponownie szablonu ani aktualnych snippetów. |

Zmiana szablonu lub snippetu wpływa na kolejne zastosowania szablonu. Nie aktualizuje automatycznie otwartego maila, wiadomości w kolejce ani historii. Ponowne zastosowanie szablonu w nowej wiadomości zastępuje treść i listę załączników oraz przywraca dane odbiorcy i wybrane przez szablon konto. Może nadpisać ręczne poprawki.

Zmiana konta SMTP lub adresu odbiorcy w już przygotowanym mailu nie przelicza tekstu zawierającego wcześniej podstawione dane. `client.email` pochodzi z zamówienia, a nie z ręcznie zmienionego pola odbiorcy. Przy tworzeniu bez szablonu domyślne powitanie może nadal zawierać `{{ client.name }}` — zastąp je konkretnym tekstem przed wysłaniem.

## 3. HTML i temat

W polu HTML wpisujesz kod, np. `<p>Dzień dobry,</p>`, a nie Markdown. Można użyć fragmentu HTML; przykłady poniżej nie wymagają opakowania w cały dokument. Zwykły Enter w kodzie służy czytelności źródła; akapity i łamanie wierszy twórz przez `<p>`, `<div>` lub `<br>`.

Do projektowania maili przyjmij prosty układ: czytelne akapity, tabele dla zestawień i style bezpośrednio w atrybucie `style`. Podgląd aplikacji nie jest gwarancją identycznego wyglądu u odbiorcy. Nie opieraj szablonu na JavaScript, formularzach, zewnętrznych arkuszach CSS ani funkcjach aplikacji internetowej. Silnik podstawia tekst w HTML; nie kompiluje MJML, Markdown ani kodu programistycznego.

Temat jest zwykłym tekstem z opcjonalnymi **zmiennymi globalnymi**:

```text
Zamówienie {{ order.identifier }} — potwierdzenie
```

Temat nie obsługuje HTML, snippetów, warunków, pętli ani zmiennych pojedynczego produktu/materiału. Jest wymagany i ma limit 998 znaków, również po przygotowaniu wiadomości. Nazwa szablonu jest wymagana, unikalna i ma najwyżej 150 znaków. HTML nie może być pusty.

## 4. Zmienne — składnia i formatowanie

```html
<p>Dzień dobry {{ client.name }},</p>
<p>Wartość zamówienia: {{ order.totalGrossPrice }}</p>
```

- Spacje przy nazwie są opcjonalne: `{{client.name}}` i `{{ client.name }}` działają tak samo.
- Nazwy zmiennych i kolekcji są rozpoznawane bez uwzględniania wielkości liter, ale używaj pisowni z tego dokumentu. Słowa sterujące `#if`, `/if`, `#each`, `/each` zapisuj małymi literami.
- Kropki są częścią gotowego klucza. Nie dają dostępu do dowolnych właściwości, metod, indeksów ani pól obiektów.
- Zmienna istniejąca, ale bez wartości, daje pusty tekst. Nieznana zmienna o poprawnej składni powoduje błąd renderowania.
- W HTML wartości są kodowane jako tekst: np. `A&B` staje się `A&amp;B`, a `<b>` nie tworzy pogrubienia. W temacie kodowanie HTML nie jest stosowane.
- Opisy klienta i zamówienia są tekstem, nie surowym HTML. Znaki nowej linii w danych nie są zamieniane na `<br>`.
- Nie ma potrójnych klamer, funkcji „raw”, filtrów dat/kwot, obliczeń, wartości domyślnych ani przypisywania zmiennych.
- Daty mają format `dd.MM.yyyy`, np. `02.09.2026`. Kwoty mają polski format z dwoma miejscami po przecinku, separatorem tysięcy i dopiskiem ` zł`. Nie dopisuj drugiego `zł` i nie próbuj wykonywać działań na tych tekstach.
- W atrybutach HTML używaj cudzysłowów, np. `href="{{ order.trackingUrl }}"`. Kodowanie HTML nie jest kodowaniem parametrów URL; silnik nie ma filtra kodowania URL.

### Wszystkie zmienne globalne

Są dostępne w temacie oraz w HTML **poza ciałem pętli**.

| Zmienna | Znaczenie / format |
| --- | --- |
| `order.id` | Techniczny identyfikator GUID zamówienia. |
| `order.identifier` | Czytelny numer/identyfikator zamówienia. |
| `order.paymentIdentifier` | Identyfikator płatności zapisany w zamówieniu; nie potwierdza opłacenia. |
| `order.orderDate` | Data zamówienia, `dd.MM.yyyy`. |
| `order.status` | Techniczna nazwa statusu: `New`, `Processing`, `Shipped` albo `Delivered`. Bez automatycznego tłumaczenia na polski. |
| `order.source` | Źródło zamówienia. |
| `order.description` | Opis zamówienia jako zwykły tekst. |
| `order.trackingNumber` | Numer przesyłki, może być pusty. |
| `order.trackingUrl` | Link śledzenia wyliczony z przewoźnika i numeru. Obecnie dla `InPost`: `https://inpost.pl/sledzenie-przesylek?number=…`. Pusty bez numeru/przewoźnika lub bez obsługi linku dla danego przewoźnika; obecnie `Ups` nie ma takiego linku. |
| `order.carrier` | Techniczna nazwa przewoźnika: `InPost` albo `Ups`; może być pusta. |
| `order.totalNetPrice` | Suma: cena jednostkowa netto × zamówiona ilość każdego produktu + koszt wysyłki netto dla klienta. Nie dolicza materiałów pakowych. |
| `order.totalGrossPrice` | Analogiczna suma brutto produktów i kosztu wysyłki brutto dla klienta. Nie dolicza materiałów pakowych. |
| `order.shippingNetClientPrice` | Koszt wysyłki netto obciążający klienta, kwota z `zł`. |
| `order.shippingGrossClientPrice` | Koszt wysyłki brutto obciążający klienta, kwota z `zł`. |
| `client.name` | Imię i nazwisko lub nazwa klienta z danych zamówienia. |
| `client.email` | E-mail klienta z danych zamówienia. |
| `client.phone` | Telefon klienta. |
| `client.street` | Ulica i numer klienta. |
| `client.postCode` | Kod pocztowy klienta. |
| `client.city` | Miasto klienta. |
| `client.description` | Opis klienta jako zwykły tekst. |
| `client.isCompany` | Tekst `True` albo `False`, zgodnie z polem „Firma” w zamówieniu. Zwykle używany w warunku. |
| `client.isNotCompany` | Odwrotność `client.isCompany`, również `True` albo `False`. |
| `sender.name` | Nazwa nadawcy (`FromName`) z konta SMTP użytego do renderowania. |
| `sender.email` | Adres nadawcy (`FromAddress`) z tego konta; nie adres Reply-To. |

To pełna lista: 25 zmiennych globalnych. Nie ma m.in. NIP-u, numeru rachunku, terminu płatności, flagi „opłacone”, linku płatności, zdjęć produktów, adresu firmy nadawcy ani danych faktury. Takie treści wymagają danych stałych od użytkownika lub zmiany możliwości aplikacji.

## 5. Warunki

Jedyna konstrukcja warunkowa to:

```html
{{#if order.trackingUrl}}
<p><a href="{{ order.trackingUrl }}">Śledź przesyłkę</a></p>
{{/if}}
```

Warunek przyjmuje **jeden klucz zmiennej globalnej**. Blok znika, gdy wartość jest pusta, składa się z białych znaków albo jest tekstem `False` (bez rozróżniania wielkości liter). Każda inna wartość jest prawdziwa. `0`, `0,00 zł` oraz dowolny niepusty status także są prawdziwe. Nie jest to porównanie liczb ani sprawdzenie biznesowego statusu.

Warunek może używać każdej zmiennej globalnej z tabeli, nie tylko pozycji dostępnych w panelu „Warunki”. Panel zawiera gotowce dla:

| Klucz | Kiedy blok jest wyświetlany |
| --- | --- |
| `client.isCompany` | W zamówieniu zaznaczono „Firma”. |
| `client.isNotCompany` | W zamówieniu nie zaznaczono „Firma”. |
| `order.trackingNumber` | Jest numer przesyłki. Nie gwarantuje istnienia linku śledzenia. |
| `order.carrier` | Wybrano przewoźnika. |
| `order.description` | Zamówienie ma niepusty opis inny niż tekst `False`. |
| `client.email` | Jest e-mail klienta. |
| `client.phone` | Jest telefon klienta. |
| `client.description` | Klient ma niepusty opis inny niż tekst `False`. |

Dla linku śledzenia stosuj `#if order.trackingUrl`, nawet jeśli tego gotowca nie ma w panelu.

Nie ma `else`, `else if`, `unless`, `not`, `!`, `==`, `!=`, `<`, `>`, `and`, `or`, `&&`, `||` ani helperów takich jak `eq`. Nie można sprawdzić `order.status == "Shipped"` ani przetłumaczyć wszystkich statusów zestawem porównań. Nie można testować kolekcji (`#if order.products`) ani zmiennych produktu/materiału.

Dwie gałęzie dla firmy/osoby można zapisać jako **dwa sąsiadujące bloki**, korzystając z gotowej odwrotności:

```html
{{#if client.isCompany}}
<p>Dziękujemy Państwu za zamówienie.</p>
{{/if}}
{{#if client.isNotCompany}}
<p>Dziękujemy za Twoje zamówienie.</p>
{{/if}}
```

Dla innych pól nie ma ogólnej konstrukcji „jeżeli brak”. **Nie zagnieżdżaj `if` w `if`**, również poprzez dołączenie snippetu. Parser nie dopasowuje zagnieżdżonych bloków tego samego rodzaju.

## 6. Pętle i zakres zmiennych

Są dokładnie dwie kolekcje. Pętla powtarza swój HTML dla każdego elementu; dla pustej kolekcji daje pusty tekst. Obie listy są porządkowane rosnąco po nazwie produktu/materiału. Nie ma sterowania sortowaniem, filtrowania, limitów, indeksu, licznika, `first`, `last`, `this`, aliasów ani dostępu przez `../` czy `@root`.

### Produkty zamówienia

```html
<table cellpadding="8" cellspacing="0" border="1" style="border-collapse:collapse;">
  <tr><th>Produkt</th><th>Ilość</th><th>Cena brutto</th><th>Wartość brutto</th></tr>
  {{#each order.products}}
  <tr>
    <td>{{ product.name }}</td>
    <td>{{ product.orderedAmount }}</td>
    <td>{{ product.unitGrossPrice }}</td>
    <td>{{ product.totalGrossPrice }}</td>
  </tr>
  {{/each}}
</table>
```

| Zmienna tylko wewnątrz `order.products` | Znaczenie |
| --- | --- |
| `product.name` | Nazwa produktu. |
| `product.identifier` | Identyfikator produktu. |
| `product.orderedAmount` | Zamówiona ilość w polskim formacie liczbowym. |
| `product.assignedAmount` | Przypisana ilość w polskim formacie liczbowym; nie utożsamiaj jej automatycznie z ilością wysłaną. |
| `product.unitNetPrice` | Cena jednostkowa netto z pozycji zamówienia, kwota z `zł`. |
| `product.unitGrossPrice` | Cena jednostkowa brutto z pozycji zamówienia, kwota z `zł`. |
| `product.totalNetPrice` | Cena jednostkowa netto × zamówiona ilość, kwota z `zł`. |
| `product.totalGrossPrice` | Cena jednostkowa brutto × zamówiona ilość, kwota z `zł`. |

### Materiały pakowe zamówienia

```html
<ul>
  {{#each order.packingMaterials}}
  <li>{{ packingMaterial.name }} — {{ packingMaterial.amount }} {{ packingMaterial.unit }}</li>
  {{/each}}
</ul>
```

| Zmienna tylko wewnątrz `order.packingMaterials` | Znaczenie |
| --- | --- |
| `packingMaterial.name` | Nazwa materiału pakowego. |
| `packingMaterial.amount` | Ilość w polskim formacie z dwoma miejscami po przecinku. |
| `packingMaterial.unit` | Jednostka materiału. |

### Ograniczenia łączenia bloków

W ciele pętli zwykłe `{{ zmienna }}` są rozwiązywane **wyłącznie ze słownika elementu**, bez zmiennych globalnych. Zatem `{{ client.name }}`, `{{ sender.name }}` lub `{{ order.identifier }}` wewnątrz pętli produktów spowodują błąd. Umieszczaj je przed pętlą albo po niej. Poza pętlą nie można użyć `{{ product.name }}` ani `{{ packingMaterial.name }}`.

Kolejność działania silnika jest stała:

1. Rozwinięcie wszystkich snippetów, także wewnątrz warunków i pętli.
2. Rozwinięcie pętli i podstawienie zmiennych elementów.
3. Ocena warunków na zmiennych globalnych.
4. Podstawienie pozostałych zmiennych globalnych.

Nie zagnieżdżaj pętli w pętli. Pętlę można objąć pojedynczym warunkiem globalnym, ale pętla i tak zostanie przetworzona przed sprawdzeniem warunku. `if` wewnątrz pętli sprawdza dane globalne, nigdy dane bieżącego produktu; zwykłe zmienne w jego treści nadal podlegają ograniczeniom pętli. Dla przejrzystości umieszczaj warunki globalne poza pętlami.

Fałszywy warunek nie zabezpiecza przed brakującym snippetem, nieznaną kolekcją ani błędnymi zmiennymi w ciele pętli. Te operacje wykonują się wcześniej. Nie próbuj ukrywać w nim nieobsługiwanej składni.

## 7. Snippety

Odwołanie w HTML:

```html
{{> stopka }}
```

`stopka` jest **kluczem**, nie nazwą wyświetlaną na liście. Nowy klucz musi zaczynać się małą literą łacińską lub cyfrą; dalej może zawierać małe litery łacińskie, cyfry, kropki, myślniki i podkreślenia. Ma maksymalnie 80 znaków i musi być unikalny. Nazwa również musi być unikalna, jest wymagana i ma maksymalnie 150 znaków. HTML jest wymagany. W odwołaniach wielkość liter klucza nie ma znaczenia.

Snippety to zapisane w aplikacji fragmenty użytkownika, a nie zestaw wbudowanych słów kluczowych. Ten podręcznik nie jest wykazem aktualnych snippetów w konkretnej bazie.

Snippet jest wstawiany jako HTML, bez kodowania jego znaczników. Może zawierać zmienne, warunki, pętle, obrazy oraz odwołania do innych snippetów. Nie przyjmuje parametrów, nie tworzy własnego zakresu i nie omija ograniczeń parsera. Snippet wstawiony do pętli musi pasować do zakresu tej pętli. Fragment z `product.name` trzeba podglądać w szablonie, który obejmuje go pętlą, nie samodzielnie dla zamówienia.

Do renderowania dostępne są tylko zapisane, **aktywne** snippety. Brakujący lub nieaktywny snippet powoduje błąd, także gdy odwołanie znajduje się w fałszywym warunku. Nie wolno tworzyć cykli, np. `stopka → kontakt → stopka`, ani przekraczać 32 poziomów zagnieżdżenia. Zapis snippetu sprawdza jego zależności i cykle; samo zapisanie szablonu nie potwierdza poprawności wszystkich konstrukcji ani dostępności aktywnych zależności.

Przykładowa **nowa definicja** (trzeba ją utworzyć i zapisać):

Nazwa: `Podpis nadawcy`  
Klucz: `podpis_nadawcy`

```html
<p style="margin-top:24px;">
  Pozdrawiamy,<br>
  {{ sender.name }}<br>
  <a href="mailto:{{ sender.email }}">{{ sender.email }}</a>
</p>
```

Użycie poza pętlą: `{{> podpis_nadawcy }}`. Jeśli snippet A odwołuje się do B, najpierw zapisz B. Niezapisane zmiany B nie trafią do podglądu A ani szablonu używającego B.

## 8. Obrazy i załączniki

„Wstaw obraz” działa w edytorze szablonu, snippetu oraz oknie tworzenia/ponawiania wiadomości. Wybiera plik PNG, JPG/JPEG lub GIF i wstawia `<img>` w miejscu kursora, zastępując zaznaczony fragment.

- Obraz musi być niepusty i mieć maksymalnie 5 MiB (w komunikatach: 5 MB). Przycisk odrzuca też obraz powyżej 40 milionów pikseli.
- Początkowa szerokość wynosi 240 px albo naturalną szerokość obrazu, jeśli jest mniejsza. `alt` pochodzi z nazwy pliku bez rozszerzenia. Po wstawieniu można poprawić `alt`, `width` i style w HTML.
- W zapisywanym HTML obraz ma pełne źródło `data:image/png;base64,...`, `data:image/jpeg;base64,...` albo `data:image/gif;base64,...`. To dane rzeczywistego obrazu, nie ścieżka do pliku.
- Edytor pokazuje zamiast długich danych skróty typu `cid:obraz-1`. Są lokalne dla danej sesji edytora. **Nie przepisuj ich ręcznie do innego maila ani do odpowiedzi AI.** Zwykłe kopiowanie kodu przez schowek rozwija je z powrotem do pełnego Base64; można w ten sposób przenieść kod do innego edytora lub przekazać AI.
- Przy wysyłce aplikacja sama zamienia Base64 na zasoby MIME powiązane przez CID. Nie trzeba tworzyć CID samemu. Jawne `cid:`, ścieżki dyskowe, `file:` i ścieżki sieciowe w utrwalonym `<img src>` są odrzucane.
- Maksymalnie 20 różnych osadzonych obrazów. Powtórzenia tych samych bajtów obrazu współdzielą zasób. Łączny limit unikalnych obrazów i zwykłych załączników to 20 MiB; liczone są bajty plików, nie długość Base64. Limit dotyczy także wynikowej wiadomości po rozwinięciu snippetów.
- Osadzane obrazy SVG, WebP oraz obrazy w CSS `background-image` nie korzystają z tego mechanizmu. Zewnętrzny adres HTTPS w `<img src>` pozostaje zewnętrznym adresem: aplikacja go nie pobiera ani nie osadza.

Zwykłe załączniki dodaje się oddzielnie przez interfejs, nie przez HTML czy snippet. Szablon może je dostarczyć przy zastosowaniu; można też dodać je w oknie wysyłki. Limit to 20 plików, maksymalnie 20 MiB na plik i łącznie 20 MiB wraz z osadzonymi obrazami. Nie ma zmiennych generujących załączniki ani automatycznie dołączających fakturę/PDF. Link `<a>` nie jest załącznikiem.

Przy ponownej wysyłce aplikacja pokazuje różnice lub niedostępne załączniki wymagające rozstrzygnięcia. Treść HTML i zawarte w niej obrazy pochodzą z historii. Zwykłe pliki wysłanych wiadomości są przechowywane przez siedem dni od wysłania; obrazy osadzone pozostają razem z historycznym HTML.

## 9. Praca w edytorze i sprawdzenie wyniku

1. W module mailingu utwórz potrzebne snippety i zapisz je jako aktywne, zaczynając od zależności.
2. Utwórz szablon, nadaj nazwę, wklej temat i HTML do oddzielnych pól. Wybierz konto nadawcze, jeśli ma być stałe. Bez przypisanego konta przygotowanie maila wybiera włączone konto domyślne, a następnie pierwsze według nazwy.
3. Panel zmiennych/warunków/pętli/snippetów wstawia składnię dwuklikiem do ostatnio aktywnego pola. Najpierw ustaw kursor w docelowym polu. Panel nie zastępuje znajomości zakresu zmiennych; np. zmienna produktu nie będzie poprawna w temacie.
4. Edytor kodu obsługuje wcięcia przez Tab, numery wierszy oraz przenoszenie bieżącego/zaznaczonych wierszy przez Alt+strzałka w górę/dół.
5. Wybierz zamówienie do podglądu. HTML aktualizuje się podczas edycji bez potrzeby zapisu bieżącego szablonu/snippetu. Zależne snippety nadal pochodzą z bazy.
6. Sprawdź warianty istotne dla treści: firma/osoba, dane opcjonalne obecne/nieobecne, wiele produktów i długie nazwy, przesyłka z linkiem i bez linku. Wybranie zamówienia zmienia dane, nie logikę szablonu.
7. Zapisz szablon jako aktywny. Otwórz tworzenie maila dla zamówienia i zastosuj szablon, aby sprawdzić również temat, rzeczywiste konto nadawcze i komplet załączników.
8. Przed wysłaniem sprawdź gotową treść i brak nierozwiniętych `{{ ... }}`. Zmiany wykonane już w oknie wysyłki powinny być gotowym HTML i zwykłym tematem.

Podgląd szablonu używa wskazanego konta albo konta wybranego według domyślności/nazwy; podgląd snippetu korzysta z tego drugiego sposobu. Podgląd może wybrać także konto wyłączone, podczas gdy przygotowanie wiadomości wymaga włączonego. Przy porównywaniu podpisu `sender.*` upewnij się, że oglądasz to samo konto.

Podgląd HTML nie renderuje tematu. Gdy renderowanie podglądu zawiedzie, interfejs może wrócić do surowego HTML zamiast pokazać szczegółowy błąd. Poprawny wygląd statycznej części lub udany zapis nie oznaczają, że składnia została zweryfikowana. Nieobsługiwane konstrukcje mogą pozostać dosłownym tekstem, zamiast zostać odrzucone. Podgląd w oknie wysyłki również pokazuje aktualny HTML, bez nowego podstawiania zmiennych.

## 10. Kompletny szablon bez zależności od snippetów

Nazwa: `Potwierdzenie zamówienia`

Temat:

```text
Potwierdzenie zamówienia {{ order.identifier }}
```

HTML:

```html
<div style="font-family:Arial,sans-serif; font-size:15px; line-height:1.5; color:#222222;">
  <h1 style="font-size:22px;">Potwierdzenie zamówienia</h1>
  <p>Dzień dobry {{ client.name }},</p>
  <p>Dziękujemy za zamówienie <strong>{{ order.identifier }}</strong>
    z dnia {{ order.orderDate }}.</p>

  <table cellpadding="8" cellspacing="0" border="1"
         style="width:100%; border-collapse:collapse; border-color:#dddddd;">
    <tr>
      <th align="left">Produkt</th>
      <th align="right">Ilość</th>
      <th align="right">Cena brutto</th>
      <th align="right">Wartość brutto</th>
    </tr>
    {{#each order.products}}
    <tr>
      <td>{{ product.name }}</td>
      <td align="right">{{ product.orderedAmount }}</td>
      <td align="right">{{ product.unitGrossPrice }}</td>
      <td align="right">{{ product.totalGrossPrice }}</td>
    </tr>
    {{/each}}
  </table>

  <p>Wysyłka brutto: {{ order.shippingGrossClientPrice }}<br>
    <strong>Łączna wartość brutto: {{ order.totalGrossPrice }}</strong></p>

  {{#if order.trackingNumber}}
  <p>Numer przesyłki: {{ order.trackingNumber }}</p>
  {{/if}}
  {{#if order.trackingUrl}}
  <p><a href="{{ order.trackingUrl }}">Śledź przesyłkę</a></p>
  {{/if}}

  <p>Pozdrawiamy,<br>{{ sender.name }}<br>
    <a href="mailto:{{ sender.email }}">{{ sender.email }}</a></p>
</div>
```

Ten szablon nie stwierdza, że zamówienie jest opłacone lub wysłane. Obecność numeru przesyłki steruje wyłącznie pokazaniem numeru; nie jest potwierdzeniem odbioru paczki przez przewoźnika.

## 11. Typowe problemy

| Objaw | Co sprawdzić / poprawić |
| --- | --- |
| „Nieznana zmienna szablonu” | Literówka, niewymieniona zmienna albo nieprawidłowy zakres: globalna zmienna w pętli lub produkt poza pętlą. |
| „Nieznana kolekcja szablonu” | Dozwolone są tylko `order.products` i `order.packingMaterials`. |
| „Nie istnieje aktywny snippet” | Klucz zamiast nazwy, zapis i aktywność snippetu oraz wszystkich jego zależności. |
| Zapętlenie / przekroczone zagnieżdżenie snippetów | Usuń cykl wskazany w komunikacie albo spłaszcz strukturę. |
| W podglądzie zostały klamry | Wybrano surowy HTML, renderowanie się nie udało albo użyto nieobsługiwanej składni. |
| Warunek działa także dla zera | Sprawdza niepusty tekst; `0,00 zł` jest prawdziwe. Nie ma porównań liczbowych. |
| Popsute bloki lub pozostawione `{{/if}}` / `{{/each}}` | Sprawdź zamknięcia oraz brak zagnieżdżeń tego samego rodzaju, również wewnątrz snippetów. |
| Brak linku śledzenia mimo numeru | `trackingUrl` wymaga obsługi wybranego przewoźnika. Użyj osobnych bloków dla numeru i linku. |
| Zmiany snippetu nie pojawiają się w szablonie | Zapisz snippet, sprawdź aktywność, odśwież podgląd, a w nowym mailu ponownie zastosuj szablon. |
| Obraz działa tylko w jednym edytorze | Nie przenoś ręcznie `cid:obraz-N`; kopiuj kod przez schowek z edytora albo wstaw obraz ponownie. |
| Wysłany mail zawiera dosłowne zmienne | Okno wysyłki nie renderuje szablonów przy wysłaniu. Zastosuj szablon wcześniej lub zastąp zmienne konkretną treścią. |

## 12. Utrzymanie podręcznika

To dokument możliwości produktu przeznaczony także do przekazywania poza repozytorium. Musi pozostać samodzielny: nie zastępuj definicji zmiennych ani zasad odsyłaczami do kodu. Nie umieszczaj tu danych klientów, haseł SMTP ani listy prywatnych snippetów z bazy.

Agenci zmieniający składnię, dostępne dane/formaty, warunki, kolekcje, snippety, obrazy, ograniczenia edytora lub moment renderowania aktualizują odpowiednie sekcje i przykłady w ramach tej samej zmiany. Opisujemy faktycznie wdrożone zachowanie, bez dziennika zmian i obietnic przyszłych funkcji. Nie każda wewnętrzna zmiana mailingu wymaga zmiany tego pliku.

Dla agentów mających repozytorium źródłami weryfikacji są:

- `src/GenoDev.BusinessTracker.ApplicationLogic/Services/MailTemplateRenderer.cs` — składnia, kolejność i zakres podstawień.
- `src/GenoDev.BusinessTracker.ApplicationLogic/UseCases/Mailing/MailRenderContextFactory.cs` — pełna lista zmiennych, kolekcji i formatów.
- `src/GenoDev.BusinessTracker.ApplicationLogic/UseCases/Mailing/MailSnippetDependencies.cs`, `SaveMailSnippet/` i `SaveMailTemplate/` — zależności i walidacja.
- `src/GenoDev.BusinessTracker.ApplicationLogic/UseCases/Mailing/RenderMailPreview/`, `GetMailComposer/` i `QueueOutgoingEmail/` — moment renderowania.
- `src/GenoDev.BusinessTracker.Wpf/ViewModels/Sales/MailingViewModel.cs` — katalog podpowiedzi i podglądy; `MailComposerViewModel.cs` w tym samym katalogu — zastosowanie szablonu.
- `src/GenoDev.BusinessTracker.ApplicationLogic/UseCases/Mailing/MailInlineImages.cs` oraz `src/GenoDev.BusinessTracker.Wpf/Controls/MailHtmlEditor.cs`, `MailHtmlEditorDocument.cs` i `MailImageInsertControl.xaml.cs` — obrazy i przenoszenie HTML.
- `src/GenoDev.BusinessTracker.Domain/Enums/OrderStatus.cs` i `Carrier.cs` — wartości statusów, przewoźników i linki śledzenia.
- `tests/GenoDev.BusinessTracker.ApplicationLogic.Tests/UseCases/Mailing/` — istniejące testy zachowania.

Reguły architektury mailingu są w `docs/agent-guides/mailing.md`; do samego napisania szablonu wystarcza niniejszy plik.
