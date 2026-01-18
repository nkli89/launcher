# Floating App Bar (MVP)

Windows-first floating app bar built with Avalonia.

## Run
```powershell
cd G:\launcher\FloatingAppBar
dotnet run
```

## Manifest
האפליקציה קוראת **כל** קבצי ה‑JSON מתוך תיקייה ייעודית `Manifests` שנמצאת ליד קובץ ההרצה.
כל קובץ כזה יכול להכיל רשימת `items` משלו, והכול מתאחד לסרגל.

### מדריך מפורט
- `manifestVersion` מספר גרסה של הפורמט (כרגע 1).
- `items[]` רשימת פריטים לסרגל לפי הסדר.

פריט (`items[]`) כולל:
- `id` מזהה ייחודי.
- `title` שם לתצוגה וטול-טיפ.
- `iconPath` נתיב לאייקון (יחסי לתיקיית ההרצה או נתיב מלא).
- `platform` ערך: `windows` / `linux` / `any`.
- `showInBar` אופציונלי: האם הפריט מוצג בסרגל הראשי (ברירת מחדל `true`).
- `actionsManifestPath` אופציונלי: נתיב לקובץ פעולות חיצוני (Sidecar).
- `actions` אובייקט פעולות. כרגע נתמך:
  - `run`: הרצת פקודה/קובץ.
    - `command` חובה.
    - `args` אופציונלי.
    - `workingDirectory` אופציונלי.
  - `openUrl`: פתיחת כתובת URL בדפדפן ברירת המחדל.
    - `url` חובה.
  - `menu`: פעולות שיופיעו בקליק ימני (רשימה של פעולות).

כללים:
- חובה לפחות פעולה אחת (`run` או `openUrl`).
- אייקונים נתמכים: SVG/PNG/ICO.
- נתיבים יחסיים מחושבים מתיקיית ההרצה של האפליקציה.
- אם לא הוגדר `actionsManifestPath`, המערכת תחפש אוטומטית קובץ Sidecar בשם
  `<שם_הקובץ>.actions.json` ליד קובץ ה־`run`.

### דוגמה מלאה
```json
{
  "manifestVersion": 1,
  "items": [
    {
      "id": "edge",
      "title": "Edge",
      "iconPath": "Assets/icons/app.svg",
      "platform": "windows",
      "actions": {
        "run": { "command": "msedge.exe" },
        "menu": [
          { "id": "edge-docs", "title": "Open Edge Docs", "openUrl": { "url": "https://learn.microsoft.com" } }
        ]
      }
    },
    {
      "id": "hello",
      "title": "Hello App",
      "iconPath": "Assets/icons/app.svg",
      "platform": "windows",
      "actionsManifestPath": "Samples/HelloApp/hello.actions.json",
      "actions": {
        "run": { "command": "Samples/HelloApp/hello.cmd" }
      }
    },
    {
      "id": "docs",
      "title": "Docs",
      "iconPath": "Assets/icons/web.svg",
      "platform": "any",
      "actions": {
        "openUrl": { "url": "https://example.com" }
      }
    }
  ]
}
```

### כמה מניפסטים נפרדים (manifest_edge, manifest_hello, manifest_urls)
בתיקייה `Manifests` אפשר לשים כמה קבצים במקביל. לדוגמה:
- `Manifests/manifest_edge.json`
- `Manifests/manifest_hello.json`
- `Manifests/manifest_urls.json`

### דוגמה לקיצורי דרך ל‑URL
קובץ לדוגמה: `Manifests/manifest_urls.json`
```json
{
  "manifestVersion": 1,
  "items": [
    {
      "id": "docs",
      "title": "Docs",
      "iconPath": "Assets/icons/web.svg",
      "platform": "any",
      "actions": {
        "openUrl": { "url": "https://example.com" }
      }
    }
  ]
}
```

### אפליקציה הכי פשוטה לדוגמה
נוצרה דוגמת “אפליקציה” מינימלית בתיקייה `Samples/HelloApp/hello.cmd`. היא יוצרת קובץ טקסט זמני ופותחת אותו ב‑Notepad. 
כדי לראות אותה בסרגל, הפריט כבר מוגדר ב‑`manifest.json` תחת `id: "hello"`.

### קובץ פעולות חיצוני (Sidecar)
קובץ לדוגמה: `Samples/HelloApp/hello.actions.json`
```json
{
  "actions": [
    { "id": "open-folder", "title": "Open Sample Folder", "run": { "command": "explorer.exe", "args": "." } },
    { "id": "open-docs", "title": "Open Docs URL", "openUrl": { "url": "https://example.com" } }
  ]
}
```

### שימוש בקליק ימני
על כל אייקון בסרגל אפשר לבצע קליק ימני ולקבל את רשימת הפעולות מה־`menu` ומה־Sidecar.

## תצוגת כל האפליקציות
כפתור **All apps** מציג רשימה מלאה של כל הפריטים מכל המניפסטים. קליק ימני מאפשר לבחור
אם האפליקציה תופיע בסרגל הראשי (Show/Remove).

## הגדרות סרגל
קובץ `settings.json` ליד קובץ ההרצה:
```json
{
  "barShape": "rounded",
  "cornerRadius": 10
}
```
- `barShape`: `rounded` או `square`.
- `cornerRadius`: רדיוס פינות כאשר `barShape` הוא `rounded`.

## Notes
- App bar docks to the left of the primary screen on startup.
- SVG/PNG/ICO icons are supported.
