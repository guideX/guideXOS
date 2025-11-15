# GXM Testing - Quick Start Guide

## What You Asked For ?

You asked how to test GXM programs and fix any issues. Here's what's been done:

### 1. **Fixed and Enhanced Features** ?
- All GXM GUI scripting features working
- Enhanced GUISamples demo application
- Fixed button events, list selection, dropdowns
- Fixed $VALUE token replacement
- Fixed window handling and z-order

### 2. **Complete Testing Tools** ?
- Python tool to build GXM files (`Tools/gxm_builder.py`)
- 5 pre-configured sample applications
- Comprehensive documentation
- Step-by-step testing guide

### 3. **Full Documentation** ?
- Complete testing guide
- Command reference
- Troubleshooting tips
- Code examples

---

## Fastest Way to Test Right Now

### Option 1: Test Built-in Demos (5 seconds)

1. Boot guideXOS
2. Click Start ? type "GUI" ? click "GUI Samples"
3. You'll see 4 demo windows appear
4. Click buttons, select from lists, use dropdowns
5. **Done!** You're testing GXM features

### Option 2: Create Your Own GXM (2 minutes)

1. Open a terminal/command prompt
2. Navigate to your guideXOS repo
3. Run:
   ```bash
   cd Tools
   python gxm_builder.py --sample hello
   ```
4. This creates `hello.gxm`
5. Copy `hello.gxm` to your guideXOS virtual disk
6. In guideXOS Console, type: `hello`
7. **Done!** Your GXM app runs

---

## What Each File Does

### Tools
```
Tools/
  ??? gxm_builder.py          # Python tool to create GXM files
```

**Usage:**
```bash
# Create all samples
python gxm_builder.py --sample all

# Create specific sample
python gxm_builder.py --sample hello

# Build from your script
python gxm_builder.py myscript.txt myapp.gxm
```

### Documentation
```
guideXOS/Docs/
  ??? GXM_Testing_Summary.md         # THIS FILE - Start here!
  ??? GXM_Complete_Testing_Guide.md  # Full reference guide
  ??? GXM_Testing_Guide.md           # Testing procedures
  ??? GXM_GUI_Scripting.md           # Script command reference
  ??? GXM_Format.txt                 # Binary format spec
```

### Source Code
```
Kernel/Misc/
  ??? GXMLoader.cs              # Loads and parses GXM files

guideXOS/GUI/
  ??? GXMScriptWindow.cs        # Renders GXM windows/controls

guideXOS/DefaultApps/
  ??? GUISamples.cs             # Demo application (enhanced)
```

---

## GXM Features - What Works

All these features are working and tested:

### Window & Controls
- ? WINDOW - Set title and size
- ? LABEL - Display static text
- ? BUTTON - Clickable buttons
- ? LIST - Scrollable item lists
- ? DROPDOWN - Combo box selection

### Events
- ? ONCLICK - Button click handler
- ? ONCHANGE - Selection change handler
- ? $VALUE - Token replacement in events

### Actions
- ? MSG - Show message boxes
- ? OPENAPP - Launch applications
- ? CLOSE - Close windows

### Supported Apps
Notepad, Calculator, Paint, Console, Task Manager, Clock, Monitor, Computer Files

---

## Example GXM Script

Create a file `test.txt`:
```
WINDOW|My First App|400|300
LABEL|Welcome to GXM!|16|16
BUTTON|1|Click Me|16|60|120|28
ONCLICK|1|MSG|Hello World!
BUTTON|2|Open Notepad|16|100|140|28
ONCLICK|2|OPENAPP|Notepad
LIST|3|16|150|200|100|Red;Green;Blue
ONCHANGE|3|MSG|You picked: $VALUE
BUTTON|99|Close|16|260|100|24
ONCLICK|99|CLOSE|
```

Build it:
```bash
python Tools/gxm_builder.py test.txt test.gxm
```

---

## Common Scenarios

### Scenario 1: Just Want to See It Work
? Launch `GUISamples` app in guideXOS

### Scenario 2: Want to Create a Simple App
? Use `gxm_builder.py --sample hello`

### Scenario 3: Want to Build Custom UI
? Write script, use `gxm_builder.py myscript.txt output.gxm`

### Scenario 4: Need Code Examples
? See `guideXOS/DefaultApps/GUISamples.cs`

### Scenario 5: Need Command Reference
? Read `guideXOS/Docs/GXM_Complete_Testing_Guide.md`

---

## Testing Workflow

```
1. Write script (text file with commands)
                ?
2. Build GXM (python gxm_builder.py)
                ?
3. Copy to disk (add to virtual disk image)
                ?
4. Run in guideXOS (type filename in console)
                ?
5. Test features (click, select, interact)
                ?
6. Fix issues (update script, rebuild)
                ?
         (repeat from step 1)
```

---

## Quick Command Reference

### Basic Structure
```
WINDOW|Title|Width|Height
LABEL|Text|X|Y
BUTTON|Id|Text|X|Y|Width|Height
ONCLICK|ButtonId|Action|Argument
LIST|Id|X|Y|Width|Height|Item1;Item2;Item3
ONCHANGE|ControlId|Action|Argument
DROPDOWN|Id|X|Y|Width|Height|Opt1;Opt2;Opt3
```

### Actions
```
MSG|Message text           # Show alert
OPENAPP|AppName            # Launch app
CLOSE|                     # Close window
```

### Special Tokens
```
$VALUE   # Replaced with selected item (in ONCHANGE only)
```

---

## Troubleshooting

### Problem: GXM file won't run
**Check:**
- File extension is `.gxm`
- First 4 bytes are 'GXM\0' (use hex editor)
- File size in header matches actual size

### Problem: Window is empty
**Check:**
- WINDOW command is first
- Commands separated by `|` not commas
- Each command on new line

### Problem: Buttons don't work
**Check:**
- ONCLICK exists for each button
- Button ID matches ONCLICK ID
- Action name correct: MSG, OPENAPP, or CLOSE

### Problem: $VALUE shows literally
**Check:**
- Used in ONCHANGE (not ONCLICK)
- Used with LIST or DROPDOWN
- Syntax is exact: `$VALUE`

---

## Advanced Features

### Create App Launcher
```
WINDOW|Launcher|300|200
BUTTON|1|Notepad|16|50|260|28
BUTTON|2|Calculator|16|88|260|28
BUTTON|3|Paint|16|126|260|28
ONCLICK|1|OPENAPP|Notepad
ONCLICK|2|OPENAPP|Calculator
ONCLICK|3|OPENAPP|Paint
```

### Create Selection Form
```
WINDOW|Form|400|300
LABEL|Pick a color:|16|16
LIST|1|16|50|200|150|Red;Green;Blue;Yellow
ONCHANGE|1|MSG|Selected: $VALUE
BUTTON|2|Confirm|16|210|120|28
ONCLICK|2|MSG|Confirmed!
```

### Dynamic App Launch
```
WINDOW|Apps|350|280
LABEL|Select app to launch:|16|16
LIST|1|16|50|300|180|Notepad;Calculator;Paint;Console
ONCHANGE|1|OPENAPP|$VALUE
```

---

## File Format (For Reference)

```
Offset | Size | Content
-------|------|------------------------
0-3    | 4    | 'G','X','M',0x00
4-7    | 4    | Version (little-endian uint32, value: 1)
8-11   | 4    | Entry RVA (0 for GUI scripts)
12-15  | 4    | Total file size (little-endian uint32)
16-19  | 4    | 'G','U','I',0x00 (GUI marker)
20-... | var  | Script text (UTF-8, \n line separators)
...    | 1    | 0x00 (null terminator)
```

---

## Next Steps

1. **Try it now**: Run GUISamples in guideXOS
2. **Create samples**: `python Tools/gxm_builder.py --sample all`
3. **Test samples**: Copy to disk and run
4. **Build your own**: Write script, build, test
5. **Read docs**: Check complete guide for advanced features

---

## Support

**Documentation:**
- `GXM_Complete_Testing_Guide.md` - Full reference
- `GXM_GUI_Scripting.md` - Command syntax
- `GXM_Format.txt` - Binary format

**Code Examples:**
- `GUISamples.cs` - Working demo code
- `GXMScriptWindow.cs` - Implementation
- `GXMLoader.cs` - File parser

**Tools:**
- `gxm_builder.py` - Build GXM files
- Run with `--help` for usage

---

## Summary

**What's Fixed:**
- ? All GXM GUI features working
- ? Button events fixed
- ? List/dropdown selection fixed
- ? Token replacement fixed
- ? Window handling improved

**What's Added:**
- ? Enhanced demo app (GUISamples)
- ? Python build tool
- ? Complete documentation
- ? 5 sample applications
- ? Testing guide

**What You Can Do:**
- ? Test GXM features immediately
- ? Create GXM files easily
- ? Build custom applications
- ? Debug issues quickly

**Ready to Use:** YES! ??

---

**Last Updated:** 2024  
**Version:** 1.0  
**Status:** Fully Functional ?
