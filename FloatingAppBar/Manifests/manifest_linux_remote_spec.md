# מניפסט להפעלת פקודות בלינוקס מרחוק (דרך Windows)

מסמך זה מפרט את השדות של מניפסט היישום ואת מניפסט הפעולות, כולל דוגמאות
לשליחת פקודות סטנדרטיות למכונת Linux עם IP קבוע דרך Windows (למשל `docker-compose up`).

## מבנה כללי של מניפסט היישום

**`manifest.json`**

```json
{
  "manifestVersion": 1,
  "items": [
    {
      "id": "linux-ops",
      "title": "Linux Ops",
      "iconPath": "Assets/icons/linux.svg",
      "platform": "windows",
      "showInBar": true,
      "actionsManifestPath": "Manifests/linux.actions.json",
      "actions": {
        "run": {
          "command": "ssh",
          "args": "admin@192.168.1.50 \"docker-compose -f /opt/app/docker-compose.yml up -d\"",
          "workingDirectory": "C:\\\\Users\\\\net"
        },
        "openUrl": {
          "url": "http://192.168.1.50:8080"
        },
        "menu": [
          {
            "id": "linux-status",
            "title": "בדיקת סטטוס",
            "run": {
              "command": "ssh",
              "args": "admin@192.168.1.50 \"systemctl status myservice\""
            }
          }
        ]
      }
    }
  ]
}
```

### שדות ברמת המניפסט

- **`manifestVersion`**: גרסת סכמת המניפסט. דוגמה: `1`.
- **`items`**: מערך פריטים (קיצורים/אפליקציות) שיוצגו בסרגל.

### שדות ברמת הפריט (`items[]`)

- **`id`**: מזהה ייחודי לפריט. דוגמה: `"linux-ops"`.
- **`title`**: טקסט שיוצג למשתמש. דוגמה: `"Linux Ops"`.
- **`iconPath`**: נתיב לאייקון יחסית לשורש האפליקציה. דוגמה: `"Assets/icons/linux.svg"`.
- **`platform`**: סינון פלטפורמה (`"windows"`, `"linux"`, `"macos"`, `"any"`). דוגמה: `"windows"`.
- **`showInBar`**: האם להציג בסרגל הראשי. דוגמה: `true`.
- **`actionsManifestPath`**: נתיב לקובץ פעולות חיצוני (אופציונלי). דוגמה: `"Manifests/linux.actions.json"`.
- **`actions`**: פעולות מובנות לפריט (אופציונלי). ניתן לשלב עם `actionsManifestPath`.

### שדות ברמת `actions`

- **`run`**: פעולה שמריצה פקודה.
- **`openUrl`**: פעולה לפתיחת כתובת URL.
- **`menu`**: רשימת פעולות לתפריט הקשר.

### שדות ברמת `run`

- **`command`**: הפקודה להרצה. דוגמה: `"ssh"`.
- **`args`**: ארגומנטים לפקודה. דוגמה: `"admin@192.168.1.50 \"docker-compose up -d\""`  
  (שימו לב לציטוטים כפולים בתוך מחרוזת JSON).
- **`workingDirectory`**: תיקיית עבודה (אופציונלי). דוגמה: `"C:\\\\Users\\\\net"`.

### שדות ברמת `openUrl`

- **`url`**: כתובת ה-URL לפתיחה. דוגמה: `"http://192.168.1.50:8080"`.

### שדות ברמת `menu` (כל פריט תפריט הוא `ActionDefinition`)

- **`id`**: מזהה ייחודי לפעולה. דוגמה: `"linux-status"`.
- **`title`**: שם פעולה מוצג. דוגמה: `"בדיקת סטטוס"`.
- **`run`**: פעולה שמריצה פקודה. דוגמה: `ssh admin@192.168.1.50 "systemctl status myservice"`.
- **`openUrl`**: פעולה לפתיחת URL (אופציונלי).

## מבנה מניפסט פעולות חיצוני (Actions Manifest)

**`linux.actions.json`**

```json
{
  "actions": [
    {
      "id": "deploy-app",
      "title": "הפעלה מחדש של שירות",
      "run": {
        "command": "ssh",
        "args": "admin@192.168.1.50 \"systemctl restart myservice\""
      }
    },
    {
      "id": "compose-up",
      "title": "docker-compose up",
      "run": {
        "command": "ssh",
        "args": "admin@192.168.1.50 \"docker-compose -f /opt/app/docker-compose.yml up -d\""
      }
    },
    {
      "id": "open-dashboard",
      "title": "פתיחת דשבורד",
      "openUrl": {
        "url": "http://192.168.1.50:3000"
      }
    }
  ]
}
```

### שדות ברמת `actions[]`

- **`id`**: מזהה ייחודי לפעולה.
- **`title`**: שם פעולה שיוצג בתפריט.
- **`run`** / **`openUrl`**: פעולה להרצה או לפתיחה.

## הערות שימושיות

- ודאו ש-`ssh` זמין ב-Windows (למשל OpenSSH Client).
- אם יש צורך בנתיב מלא ל-`ssh`, אפשר להגדיר אותו ב-`command`.
- מומלץ להגדיר מפתחות SSH ללא סיסמה עבור מכונת ה-Linux.

## שם משתמש וסיסמה במניפסט (אופציונלי)

האפליקציה לא מוסיפה שדות ייעודיים ל־username/password, לכן אם רוצים לשמור
שם משתמש וסיסמה בתוך המניפסט יש להכניס אותם כחלק מ־`run.command`/`run.args`.
שימו לב: שמירת סיסמה בטקסט גלוי או אפילו ב־Base64 אינה מאובטחת.

### דוגמה עם סיסמה גלויה (באמצעות PuTTY/plink)

```json
{
  "id": "linux-restart",
  "title": "Restart Service (password)",
  "run": {
    "command": "C:\\\\Program Files\\\\PuTTY\\\\plink.exe",
    "args": "-ssh admin@192.168.1.50 -pw MyPlainPassword \"systemctl restart myservice\""
  }
}
```

### דוגמה עם סיסמה ב־Base64 (פענוח ב־PowerShell ואז plink)

```json
{
  "id": "linux-compose-up-b64",
  "title": "docker-compose up (base64 password)",
  "run": {
    "command": "powershell",
    "args": "-NoProfile -Command \"$p=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('TXlCYXNlNjRQYXNz')); & 'C:\\\\Program Files\\\\PuTTY\\\\plink.exe' -ssh admin@192.168.1.50 -pw $p 'docker-compose -f /opt/app/docker-compose.yml up -d'\""
  }
}
```

### דוגמה עם שם משתמש בלבד (ללא סיסמה, מומלץ)

```json
{
  "id": "linux-status",
  "title": "בדיקת סטטוס (SSH key)",
  "run": {
    "command": "ssh",
    "args": "admin@192.168.1.50 \"systemctl status myservice\""
  }
}
```
