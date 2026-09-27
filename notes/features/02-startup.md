# Запуск и стартовая папка

Статус: ✅

## Что делает

- `fndr` — открывается домашняя папка (`UserProfile`).
- `fndr <путь>` — открывается указанная папка; относительный путь
  разворачивается от текущей директории процесса.
- Путь не существует / пустой → открывается домашняя, в статусе
  «нет такой папки: <путь>, открыта домашняя».

## Как устроено

- `Program.cs`: top-level statements, DI-контейнер
  (`IFileSystem` → `FileSystem`, `StartDir`, `MainViewModel` — синглтоны).
- Стартовый путь обёрнут в `record StartDir(string Path)` и внедряется в VM.
- `ClassicDesktopStyleApplicationLifetime` создаётся вручную,
  `ShutdownMode.OnLastWindowClose`.

## Ограничения

- Нет `[STAThread]` → на Windows не будут работать OLE-буфер и DnD
  (нужно до 01-copy-paste-dnd, шаг 0).
