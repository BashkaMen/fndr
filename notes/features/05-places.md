# Места (боковая панель)

Статус: ✅

## Что делает

Быстрый переход: Домой, Документы, Загрузки, Музыка, Картинки, Видео.
Переход идёт через обычный `Open` — пишется в историю.

## Как устроено

- `MainViewModel.Places` — `IReadOnlyList<Place>`, `record Place(Title, Path)`.
- Пути — `Environment.GetFolderPath(SpecialFolder...)`; «Загрузки» —
  `UserProfile\Downloads` (в `SpecialFolder` её нет).
- XAML: `ItemsControl` + `DataTemplate` с кнопкой, один обработчик `OnPlace`
  берёт `Place` из `DataContext`.

## Ограничения

- «Загрузки» захардкожены: перенесённая Known Folder не найдётся.
- На Linux часть `SpecialFolder` пустая → клик перечитывает текущую папку.
- Нет подсветки активного места.

## Идеи

- Диски (`DriveInfo.GetDrives()`), избранное с добавлением по drag&drop.
- Реальный путь Downloads через `SHGetKnownFolderPath` / `xdg-user-dir`.
