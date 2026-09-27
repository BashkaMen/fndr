# fndr — файловый менеджер на Avalonia

## Контекст

Личный файловый менеджер, главная цель — замена Finder на macOS (там неудобно
копировать путь к файлу / папке и вообще всё устроено не так, как хочется).
Разрабатывается на Windows, целевые платформы — macOS, Windows, Linux.
Соло-проект: минимум церемоний, минимум зависимостей.

## Стек

- **.NET 10** / C# 14 (`field`, extension-блоки, collection expressions, primary constructors)
- **Avalonia 12** + FluentTheme (тёмная), compiled bindings (`x:DataType`)
- **Microsoft.Extensions.DependencyInjection** — сборка графа в `Program.cs`
- **Dunet** — дискриминированные юнионы (`Result`, типы ошибок)
- Без MVVM-фреймворков: `INotifyPropertyChanged` руками

## Архитектура: MVVM + Services

```
src/Fndr/
  Program.cs             # top-level, DI, ручной ClassicDesktopStyleApplicationLifetime
  Result.cs              # Result<T, E> (Dunet) + Map / Bind / OnError, Unit
  Services/              # IO и ОС за интерфейсами: IFileSystem, ILauncher
  ViewModels/            # MainViewModel — состояние и логика, без IO и процессов
  Views/                 # XAML + тонкий code-behind
notes/features/          # NN-имя.md — описание каждой фичи
```

- **VM не трогает `System.IO` / `Process` напрямую** — только через сервисы.
  Новая работа с ОС → новый метод сервиса или новый сервис за интерфейсом,
  регистрация в `Program.cs`.
- **Code-behind** — только UI-события и фокус: достаёт данные из
  `e.Source` / `DataContext` / `SelectedItems` и зовёт метод VM.
  Логика и тексты — в VM.
- Свойства VM: `public T X { get; private set => Set(ref field, value); }`;
  `Set` возвращает `bool` — побочные эффекты при изменении в сеттере.
- Данные — `record` (`Entry`, `Place`, `StartDir`); форматирование для UI —
  вычисляемые свойства record'а или хелперы VM.

## Ошибки — Result, не исключения

- Сервисы возвращают `Result<T, SomeError>`, где `SomeError` — Dunet-юнион
  (`FsError`, `LaunchError`). Исключения ловятся **только внутри сервисов**,
  конкретными типами, и превращаются в вариант ошибки.
- VM разбирает через `Match` / `Bind` / `Map` / `OnError`; текст для статуса —
  `Describe(error)` через исчерпывающий `Match` (новый вариант не скомпилируется,
  пока не обработан).
- `Ok` с интерфейсным `T` (например `IReadOnlyList<Entry>`) — явно
  `new Result<...>.Ok(x)`: неявные конверсии из интерфейсов в C# запрещены.
- `MatchError` у Dunet требует `else`-ветку — для «только ошибка» есть `OnError`.

## UI

- Тексты интерфейса — **по-русски**, строчными («нет доступа», «скрытые»).
- Палитра: фон `#202124`, панели `#2b2f34` / `#26282b`, hover и поля `#3b4045`,
  текст `#e8eaed` / `White`, вторичный текст — `Opacity 0.6`.
- Повторяющиеся сеттеры — в `Styles` (классы `place`, `topnav`, `meta`), не инлайн.
- Горячие клавиши — через `CommandModifiers`
  (`Application.Current.PlatformSettings.HotkeyConfiguration`): Ctrl на
  Windows/Linux, Cmd на macOS. Не хардкодить `KeyModifiers.Control`.
- Глобальные хоткеи — tunnel-обработчик `KeyDown` на окне.

## Заметки о фичах

- Каждая фича — `notes/features/NN-имя.md`, `NN` — порядок добавления
  (следующий номер = максимальный + 1). Оглавление — `notes/features/README.md`
  со статусом ✅ / 🚧 / 📝.
- Разделы: «Что делает» → «Как устроено» → «Ограничения» → «Идеи».
- **Меняешь поведение фичи — обновляй её заметку в том же изменении.**
  Нашёл баг / недочёт — пиши в «Ограничения» соответствующей фичи
  (отдельного файла с багами нет).
- `README.md` в корне — зачем проект и что умеет, коротко.

## Тесты

Тестового проекта пока нет. Когда понадобится: `tests/Fndr.Tests`, xUnit v3,
тесты VM на фейковых `IFileSystem` / `ILauncher` (без диска и процессов),
интеграционные — на реальный `FileSystem` во временной папке.
До этого проверять изменения VM file-based скриптом
(`#:project src/Fndr/Fndr.csproj`, `dotnet run --file`).

**Никогда не проверять UI отправкой реальных нажатий клавиш (SendKeys,
keybd_event)** — они уходят в активное окно пользователя. Для UI — только
Avalonia.Headless или ручная проверка пользователем.

## Команды

```sh
dotnet build
dotnet run --project src/Fndr               # домашняя папка
dotnet run --project src/Fndr -- <путь>     # указанная папка
dotnet outdated                             # обновления пакетов
```

## Грабли

- **Avalonia 12 ≠ туториалы под 11**: нет `InitialPressMouseButton`
  (кнопки мыши — `e.Properties.IsXButton1Pressed`), буфер обмена — `IClipboard`
  + `DataTransfer` (`DataObject` устарел), `TopLevel.PlatformSettings` из
  code-behind недоступен — брать `Application.Current.PlatformSettings`.
- **`[STAThread]`** отсутствует (top-level statements → MTA). Перед OLE-буфером
  и drag&drop на Windows перейти на `static class Program` с `[STAThread] Main`.
- Предупреждение `AVLN3001` (у окна нет публичного конструктора без параметров) —
  ожидаемое: окно получает VM через конструктор.
- Файлы в репо — CRLF; при правке скриптами сохранять окончания строк.

## MCP

`cwm-roslyn-navigator` (из плагина dotnet-claude-kit, сам находит `fndr.slnx`) —
`find_symbol`, `find_references`, `find_implementations`, `get_diagnostics`
вместо grep по C#-коду.
