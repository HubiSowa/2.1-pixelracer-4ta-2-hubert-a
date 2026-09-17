# Pixel Auto Race — decyzje techniczne

- Projekt używa wyłącznie C#/.NET 8/WPF i bibliotek wbudowanych w .NET. Nie dodano pakietów NuGet, ponieważ renderowanie 2D na `Canvas`, animacja przez `DispatcherTimer` i zapis JSON nie wymagają zewnętrznego silnika.
- Postęp jest zapisywany jako JSON w `%LocalAppData%/PixelAutoRace/save.json`. Lokalizacja jest prywatna dla użytkownika i nie wymaga uprawnień administratora.
- Oprawa pixel-art jest zbudowana z prostych, ostrych kształtów WPF, warstw tła, neonowych akcentów i cząsteczek generowanych w trakcie wyścigu. Dzięki temu aplikacja nie ma zewnętrznych assetów do instalowania.
