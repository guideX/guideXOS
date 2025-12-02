# GXM GUI Scripting (guideXOS)

Embed a simple GUI in a GXM executable by placing a GUI header after the standard 16?byte GXM header:

Signature: `G X M \0`
Header layout:
```
0..3  magic (GXM or MUE)
4..7  version (u32)
8..11 entry RVA (u32)
12..15 image size (u32)
16..19 optional 'G','U','I','\0' to enable script parsing
20..    script text (lines separated by \n, terminated by \0)
```
If 'GUI\0' is present the loader parses the script and does NOT enter user mode; it builds a window.

## Commands
Each line uses `|` to separate fields.

### WINDOW
`WINDOW|Title|Width|Height`
Sets window title and size. Size is clamped to minimum 160x120.

### Window Properties
Control window appearance and behavior:

#### RESIZABLE
`RESIZABLE|true` or `RESIZABLE|false`
Controls whether the window can be resized by the user. Default: true

#### TASKBAR
`TASKBAR|true` or `TASKBAR|false`
Controls whether the window appears in the taskbar. Default: true

#### MAXIMIZE
`MAXIMIZE|true` or `MAXIMIZE|false`
Controls whether the window shows a maximize button. Default: true

#### MINIMIZE
`MINIMIZE|true` or `MINIMIZE|false`
Controls whether the window shows a minimize button. Default: true

#### TOMBSTONE
`TOMBSTONE|true` or `TOMBSTONE|false`
Controls whether the window shows a tombstone button. Default: true

#### STARTMENU
`STARTMENU|true` or `STARTMENU|false`
Controls whether the window appears in the start menu. Default: true

### LABEL
`LABEL|Text|X|Y`
Draws multiline capable text (width constrained to window minus padding).

### TEXTBOX
`TEXTBOX|Id|X|Y|W|H|InitialText`
Creates a multi-line editable text box. The InitialText parameter is optional.
- Click to focus the textbox (blue border indicates focus)
- Type to enter text
- Supports: Backspace, Enter, Tab, all printable characters
- Word wrap enabled by default

Example:
```
TEXTBOX|1|10|40|680|340|
```

### BUTTON
`BUTTON|Id|Text|X|Y|W|H`
Creates a button.

### LIST
`LIST|Id|X|Y|W|H|item1;item2;item3`
Creates a vertical list view with selectable rows.

### DROPDOWN
`DROPDOWN|Id|X|Y|W|H|item1;item2;item3`
Creates a dropdown (combo) control.

## Events
Define callbacks:
- `ONCLICK|Id|Action|Arg` for button click events
- `ONCHANGE|Id|Action|Arg` for list selection and dropdown change
- `ONTEXTCHANGE|Id|Action|Arg` for textbox text change events

Supported actions:
- `MSG` shows a message box. `Arg` can include `$VALUE` token which is replaced with selection text or textbox content.
- `OPENAPP` opens a built-in app by name (e.g., Notepad, Calculator).
- `CLOSE` closes the script window.
- `SAVETEXT` saves textbox content to a file. `Arg` is the filename (fixed).
- `LOADTEXT` loads file content into textbox. `Arg` is the filename (fixed).
- `SAVEDIALOG` opens a Save As dialog for textbox content. `Arg` is the default filename.
- `OPENDIALOG` opens an Open dialog to load a file into textbox. `Arg` is ignored.

Example:
```
WINDOW|Demo|480|320
RESIZABLE|false
TASKBAR|true
MAXIMIZE|false
MINIMIZE|true
TOMBSTONE|false
STARTMENU|false
LABEL|Pick a color|16|16
DROPDOWN|1|16|46|140|24|Red;Green;Blue
ONCHANGE|1|MSG|Selected $VALUE
BUTTON|2|Open Notepad|16|80|140|28
ONCLICK|2|OPENAPP|Notepad
LIST|3|200|46|200|140|Alpha;Beta;Gamma
ONCHANGE|3|MSG|List: $VALUE
BUTTON|4|Close|360|280|100|28
ONCLICK|4|CLOSE|
```

Example Notepad with Dialogs:
```
WINDOW|GXM Notepad|700|460
RESIZABLE|true
TEXTBOX|1|10|40|680|340|
BUTTON|1|Save As...|10|390|88|28
BUTTON|2|Open...|108|390|72|28
ONCLICK|1|SAVEDIALOG|notes.txt
ONCLICK|2|OPENDIALOG|
```

Example Notepad with Fixed Files:
```
WINDOW|GXM Notepad|700|460
RESIZABLE|true
TEXTBOX|1|10|40|680|340|
BUTTON|1|Save|10|390|72|28
BUTTON|2|Load|92|390|72|28
ONCLICK|1|SAVETEXT|notes.txt
ONCLICK|2|LOADTEXT|notes.txt
```

## Packaging
1. Build binary header (16 bytes GXM) with image size including script region.
2. Append `GUI\0` and script text, end with `\0`.

## Roadmap
- CheckBox, Radio, Slider, Scrollable list.
- File picker dialogs for Save As / Open
- Status bars
