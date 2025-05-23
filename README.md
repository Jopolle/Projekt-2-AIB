# Repozytorium Projektu 2 Automatyki Inteligentnych Budynków

## Paweł Bałbatun, Jakub Bulski    Grupa 6

W tym repozytorium znajduje się **(prawie)** kompletny kod naszego projektu AiB.
Jedynym brakiem jest wymóg zlinkowania aplikacji testowej do programu lokalnie na maszynie po sklonowaniu.

Aby poprawnie uruchomić program należy skopiować zawartość foldera Debug pod ścieżkę *\Projekt-2-AIB\ahuRegulator\bin\Debug*, a następnie we właściwościach programu ahuRegulator zmienić program startu na *ahuSim.exe*

W realizacji projektu zawarto:
 - Kaskadową regulację temperatury z możliwością ustalenia ograniczenia dolnego i górnego temperatury nawiewu z poziomu stałych
 - Zabezpieczenie p-zamr. nagrzewnicy wodnej
 - Zabezpieczenie p-zamr. układu odzysku ciepła
 - Możliwość opóźnionego załączania wentylatora.
 - Antywindup regulatora PI
 - Możliwość strojenia regulatorów i zmiany czasu opóźnień załączenia nagrzewnicy oraz wyłączenia wentylatorów z poziomu formularza fmParametry



