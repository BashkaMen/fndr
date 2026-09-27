# Копирование / вставка / drag&drop файлов

Дата: 2026-07-10, ревью 2026-09-28. Статус: план, не реализовано. Актуально для Avalonia 12.

## Суть

При Ctrl+C **ничего не копируется** — только список полных путей к выбранным файлам
(+ флаг cut). Настоящее копирование байтов происходит при вставке.

Все три канала (внутренний буфер, OS-буфер, DnD) несут один и тот же payload —
список файлов. Ядро одно, сверху три транспорта.

## Ядро (не зависит от транспорта)

- Буфер в VM: `private (IReadOnlyList<string> paths, bool cut)? _clip;`
  Пути полные (`Path.Combine(Path, entry.Name)` из `ListBox.SelectedItems`,
  `SelectionMode="Multiple"` уже включён).
- Paste: `dest = Path.Combine(Path, Path.GetFileName(Path.TrimEndingDirectorySeparator(src)))`.
  Без trim путь со слэшем на конце даёт пустое имя → `dest` = текущая папка.
  - cut → Move, иначе Copy (в `IFileSystem` методы `Copy` / `Move`,
    папка — рекурсивно, `Directory.CreateDirectory` + дети).
  - Коллизия: не перезаписывать (потеря данных) — `имя (1).ext`, `папка (1)`.
  - Файл мог исчезнуть после Ctrl+C: проверять существование, пропускать,
    статус «3 из 5 вставлено».
  - Папку в саму себя или в свою подпапку (`C:` → `C:`) — запрещать:
    рекурсивное копирование уйдёт в бесконечность, `Directory.Move` бросит.
  - Cut + paste в ту же папку — no-op (иначе коллизия даст `имя (1)`).
  - После вставки вырезанного — очищать `_clip` (как Explorer), иначе
    повторная вставка = «0 из N».
  - `Directory.Move` между дисками не работает → fallback copy + delete.
    (`File.Move` между дисками умеет сам.)
  - Рекурсивное копирование: junction/симлинки не обходить
    (`FileAttributes.ReparsePoint`) — могут зациклиться.
  - `UnauthorizedAccessException` → в Status (паттерн как в `TryList`).
  - После paste — перечитать текущую папку. `Open(Path)` в back-stack **не**
    пушит (проверка `Path != dir`), но чище завести `Refresh()` = `TryList` + `Apply`.
  - Выделить вставленное: в VM нет состояния выделения → вернуть имена в
    code-behind или биндить `SelectedItems`. `Entry` — record, поиск по значению работает.
- Большой файл: пока синхронный `File.Copy`; потом `CopyToAsync` по чанкам
  с прогрессом в Status.

## Транспорт 1: внутренний буфер (Ctrl+C / X / V)

- Code-behind: KeyDown Ctrl+C/X/V, `list.SelectedItems` → VM.
  Хоткеи не хардкодить: `TopLevel.PlatformSettings.HotkeyConfiguration`
  (Copy/Cut/Paste) — на macOS это Cmd.
- Флаг `cut` влияет **только** на вставку внутри fndr (Move vs Copy).
- Отдельный ClipboardService не нужен — поле в VM.

## Транспорт 2: OS-буфер (между приложениями)

Avalonia 12: `IClipboard` через `window.Clipboard` (старые `DataObject`/
`SetDataObject` — obsolete, не брать из старых туториалов).

Запись (Ctrl+C). `IStorageItem` из пути — через `StorageProvider`
(типов `StorageFolderFolder`/`StorageFileFile` нет, `FromFilePath` — internal):
```csharp
var sp = window.StorageProvider;
var items = new List<IStorageItem>();
foreach (var p in paths)
{
    IStorageItem? item = Directory.Exists(p)
        ? await sp.TryGetFolderFromPathAsync(p)
        : await sp.TryGetFileFromPathAsync(p);
    if (item is not null) items.Add(item);
}
await clipboard.SetFilesAsync(items);   // на Windows = CF_HDROP
await clipboard.FlushAsync();           // ОБЯЗАТЕЛЬНО, см. грабли
// опционально: + текстовый формат (пути по строке) для терминалов
```

Чтение (вставка извне, если внутренний буфер пуст):
```csharp
var files = await clipboard.TryGetFilesAsync(); // IStorageItem[]
var paths = files?.Select(f => f.TryGetLocalPath()).OfType<string>().ToList(); // Path — это Uri, не строка
```

Нюанс «вырезано»: на Windows есть формат `Preferred DropEffect`
(4 байта, `DROPEFFECT_MOVE = 2`) — Explorer по нему делает перенос при вставке
и сам ставит его при Ctrl+X. Можно писать рядом с файлами и читать при вставке
извне. На других ОС стандарта нет — там вставка извне = копия.

## Транспорт 3: Drag & Drop

Не через буфер: `DataTransfer` отдаётся целевому приложению прямо во время
перетаскивания.

Приём (Explorer → fndr, самое ценное, ~15 строк):
```xml
<ListBox ... DragDrop.AllowDrop="True"
          DragDrop.DragOver="OnDragOver" DragDrop.Drop="OnDrop">
```
```csharp
void OnDragOver(object? s, DragEventArgs e)
    => e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
        ? DragDropEffects.Copy : DragDropEffects.None;

void OnDrop(object? s, DragEventArgs e)
{
    var files = e.DataTransfer.TryGetFiles(); // → paths → Paste-логика
}
```
Цель: если бросили на папку в списке — в неё, иначе в текущую
(`(e.Source as Control)?.DataContext as Entry`). Бросили из fndr в ту же папку — no-op.

Отдача (fndr → вне): в PointerPressed запомнить точку, drag стартовать в
PointerMoved после порога смещения (иначе ломается выделение кликом):
```csharp
var data = new DataTransfer();
foreach (var item in items)
    data.Add(DataTransferItem.Create(DataFormat.File, item));
var result = await DragDrop.DoDragDropAsync(e, data,
    DragDropEffects.Copy | DragDropEffects.Move);
if (result == DragDropEffects.Move) Refresh(); // переносит ЦЕЛЬ, сами не трогаем
```
Copy/Move выбирает цель (в Explorer: Ctrl — копировать, Shift — перенести).

## Грабли

- **`[STAThread]`**: на Windows OLE-буфер и DnD требуют STA-поток (в
  `Avalonia.Win32` есть проверка apartment state). Top-level statements в
  `Program.cs` дают MTA → транспорты 2 и 3 молча не работают. Перед шагом 2
  перейти на `static class Program { [STAThread] static void Main(string[] args) }`.

- `FlushAsync()` после записи в буфер: данные отдаются лениво, без flush
  после закрытия fndr они пропадают (Windows/X11).
- `DataTransfer` в `SetDataAsync` не dispose'ить — владение забирает Avalonia.
- `IAsyncDataTransfer` из `TryGetDataAsync()` — только `using`, читать сразу,
  не хранить (буфер могли поменять).
- X11: file DnD ограничен. Windows — ок.

## Порядок доставки (когда решим делать)

0. `[STAThread]` в `Program.cs`; `Refresh()` в VM; фикс DoubleTapped (см. ../bugs.md).
1. Внутренний буфер + Ctrl+C/X/V (ядро + коллизии + refresh).
2. Приём drop'а из Explorer.
3. Запись в OS-буфер (+ чтение извне в Paste).
4. Отдача drag'ом.

Доки: docs.avaloniaui.net/docs/input-interaction/drag-and-drop,
docs.avaloniaui.net/docs/services/clipboard
