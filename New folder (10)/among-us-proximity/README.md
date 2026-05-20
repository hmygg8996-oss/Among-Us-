# Among Us - Proximity Voice Chat Mod

מוד שמאפשר לשמוע שחקנים **רק כשאתה קרוב אליהם** במשחק.
ככל שאתה קרוב יותר - הווליום גבוה יותר. ככל שאתה רחוק - הווליום יורד עד שלא שומעים כלום.
בפגישה (meeting) כולם שומעים את כולם. מתים לא שומעים חיים.

---

## תוכן עניינים

1. [איך זה עובד](#איך-זה-עובד)
2. [מה צריך להתקין](#מה-צריך-להתקין)
3. [שלב 1 - התקנת Node.js](#שלב-1---התקנת-nodejs)
4. [שלב 2 - התקנת NET 6 SDK](#שלב-2---התקנת-net-6-sdk)
5. [שלב 3 - התקנת BepInEx](#שלב-3---התקנת-bepinex)
6. [שלב 4 - הגדרת הנתיבים](#שלב-4---הגדרת-הנתיבים)
7. [שלב 5 - בניית המוד](#שלב-5---בניית-המוד)
8. [שלב 6 - התקנת המוד](#שלב-6---התקנת-המוד)
9. [שלב 7 - הרצת השרת](#שלב-7---הרצת-השרת)
10. [שלב 8 - משחק עם חברים](#שלב-8---משחק-עם-חברים)
11. [הגדרות מתקדמות](#הגדרות-מתקדמות)
12. [פתרון בעיות](#פתרון-בעיות)

---

## איך זה עובד

```
[Among Us] --מיקום--> [שרת WebSocket] --ווליום--> [Among Us]
```

1. כל שחקן שמריץ את המוד מתחבר לשרת WebSocket
2. כל 100ms המוד שולח את מיקום הדמות לשרת
3. השרת מחשב את המרחק בין כל השחקנים
4. אם מרחק < 2.5 יחידות → השחקנים שומעים אחד את השני
5. הווליום יורד בהדרגה ככל שהמרחק גדל
6. בפגישה (meeting) → כולם שומעים את כולם בווליום מלא
7. שחקן שמת → לא שומע חיים ולא נשמע על ידם

---

## מה צריך להתקין

לפני שמתחילים, צריך 3 דברים:

| תוכנה | למה צריך | קישור |
|-------|----------|-------|
| Node.js 18+ | להריץ את השרת | https://nodejs.org |
| .NET 6 SDK | לבנות את המוד | https://dotnet.microsoft.com/download/dotnet/6.0 |
| BepInEx IL2CPP | mod loader ל-Among Us | https://github.com/BepInEx/BepInEx/releases |

---

## שלב 1 - התקנת Node.js

1. כנס ל-https://nodejs.org
2. הורד את הגרסה **LTS** (הכפתור הירוק הגדול)
3. הרץ את ה-installer ולחץ Next בכל השלבים
4. לאחר ההתקנה, פתח **Command Prompt** (cmd) והקלד:
   ```
   node --version
   ```
   אם מופיע משהו כמו `v20.x.x` - ההתקנה הצליחה.

---

## שלב 2 - התקנת .NET 6 SDK

1. כנס ל-https://dotnet.microsoft.com/download/dotnet/6.0
2. תחת **SDK** (לא Runtime) - הורד את גרסת **Windows x64**
3. הרץ את ה-installer
4. לאחר ההתקנה, פתח **Command Prompt** חדש והקלד:
   ```
   dotnet --version
   ```
   אם מופיע `6.x.x` - ההתקנה הצליחה.

---

## שלב 3 - התקנת BepInEx

BepInEx הוא ה-mod loader שמאפשר למודים לרוץ בתוך Among Us.
**בלי זה המוד לא יעבוד בכלל.**

### מציאת תיקיית Among Us

לפני הכל, צריך לדעת איפה Among Us מותקן:
1. פתח **Steam**
2. לחץ ימני על **Among Us** בספרייה
3. בחר **Manage → Browse local files**
4. תיפתח תיקייה - זו תיקיית Among Us שלך
5. **שמור את הנתיב הזה** - תצטרך אותו בהמשך
   - לדוגמה: `C:\Program Files (x86)\Steam\steamapps\common\Among Us`

### הורדת BepInEx

1. כנס ל-https://github.com/BepInEx/BepInEx/releases
2. מצא את הגרסה האחרונה (הראשונה ברשימה)
3. תחת **Assets** הורד את הקובץ:
   ```
   BepInEx_Unity_IL2CPP_win_x64_X.X.X.X.zip
   ```
   ⚠️ חשוב: **IL2CPP** ו-**x64** - לא גרסאות אחרות!

### התקנת BepInEx

1. פתח את קובץ ה-ZIP שהורדת
2. **חלץ את כל התוכן ישירות לתיקיית Among Us**
   - כלומר, הקבצים `winhttp.dll`, `doorstop_config.ini` וכו' צריכים להיות **באותה תיקייה** כמו `Among Us.exe`
   - לא בתת-תיקייה!
3. הרץ את **Among Us** פעם אחת דרך Steam
4. המתן שהמשחק ייטען לתפריט הראשי
5. סגור את המשחק
6. עכשיו בתיקיית Among Us אמורה להיות תיקייה חדשה בשם `BepInEx`
7. בתוכה אמורות להיות תיקיות: `core`, `plugins`, `config`, `interop`

אם התיקיות האלה קיימות - BepInEx הותקן בהצלחה ✅

---

## שלב 4 - הגדרת הנתיבים

לפני שבונים את המוד, צריך לעדכן שני קבצים:

### עדכון נתיב Among Us

פתח את הקובץ `mod/ProximityChat.csproj` בעורך טקסט (Notepad, VS Code וכו').

מצא את השורה:
```xml
<AmongUsPath>C:\Program Files (x86)\Steam\steamapps\common\Among Us</AmongUsPath>
```

החלף את הנתיב בנתיב האמיתי של Among Us אצלך.

לדוגמה, אם Among Us נמצא ב-`D:\Games\Among Us`:
```xml
<AmongUsPath>D:\Games\Among Us</AmongUsPath>
```

### עדכון כתובת השרת במוד

פתח את הקובץ `mod/ProximityChatManager.cs`.

מצא את השורה:
```csharp
private const string SERVER_URL = "ws://localhost:3000";
```

- אם **כולם משחקים על אותו מחשב** (לא ריאלי אבל אפשרי) - השאר כמו שזה.
- אם **אתה מארח את השרת** ואחרים מתחברים אליך מרחוק:
  - מצא את ה-IP הציבורי שלך ב-https://whatismyip.com
  - שנה ל: `ws://YOUR_IP:3000`
  - לדוגמה: `ws://82.102.15.44:3000`
- אם **מישהו אחר מארח את השרת**:
  - קבל ממנו את ה-IP שלו
  - שנה ל: `ws://HIS_IP:3000`

---

## שלב 5 - בניית המוד

1. פתח **Command Prompt** (cmd)
2. נווט לתיקיית `mod` של הפרויקט:
   ```
   cd "C:\Users\hemig\Downloads\New folder (10)\among-us-proximity\mod"
   ```
3. הרץ את הפקודה:
   ```
   dotnet build -c Release
   ```
4. המתן שהבנייה תסתיים. בסוף אמור להופיע:
   ```
   Build succeeded.
   ```
5. הקובץ שנוצר נמצא ב:
   ```
   mod\bin\Release\net6.0\ProximityChat.dll
   ```

אם יש שגיאות בנייה - ראה [פתרון בעיות](#פתרון-בעיות).

---

## שלב 6 - התקנת המוד

1. פתח את תיקיית Among Us שלך
2. כנס לתיקייה `BepInEx\plugins`
   - אם התיקייה לא קיימת - צור אותה ידנית
3. העתק את הקובץ `ProximityChat.dll` לתוך תיקיית `plugins`

המבנה הסופי אמור להיראות כך:
```
Among Us/
├── Among Us.exe
├── winhttp.dll          ← של BepInEx
├── BepInEx/
│   ├── plugins/
│   │   └── ProximityChat.dll   ← זה מה שהעתקת
│   ├── core/
│   └── ...
```

---

## שלב 7 - הרצת השרת

השרת חייב לרוץ **לפני** שנכנסים למשחק, ולהישאר פעיל כל הזמן שמשחקים.

1. פתח **Command Prompt**
2. נווט לתיקיית `server`:
   ```
   cd "C:\Users\hemig\Downloads\New folder (10)\among-us-proximity\server"
   ```
3. התקן את החבילות (רק בפעם הראשונה):
   ```
   npm install
   ```
4. הרץ את השרת:
   ```
   npm start
   ```
5. אמור להופיע:
   ```
   Proximity server running on port 3000
   ```

**השאר את החלון הזה פתוח!** אם תסגור אותו - השרת ייכבה.

### אם חברים מתחברים מרחוק

צריך לפתוח פורט 3000 ב-router:
1. כנס לממשק ה-router שלך (בדרך כלל `192.168.1.1` בדפדפן)
2. מצא את ההגדרה **Port Forwarding**
3. הוסף חוק: TCP פורט 3000 → ה-IP המקומי של המחשב שלך
4. שמור

---

## שלב 8 - משחק עם חברים

### כל שחקן שרוצה להשתמש במוד צריך:
1. להתקין BepInEx (שלב 3)
2. לבנות ולהתקין את המוד (שלבים 4-6)
3. לעדכן את `SERVER_URL` לכתובת השרת של המארח

### סדר הפעולות לפני כל משחק:
1. המארח מריץ את השרת (`npm start`)
2. כולם מריצים את Among Us
3. נכנסים ללובי ביחד
4. המוד מתחבר אוטומטית לשרת ברגע שנכנסים ללובי

### בדיקה שהמוד עובד:
- פתח את תיקיית `Among Us\BepInEx\LogOutput.log`
- חפש שורות כמו:
  ```
  [Info] ProximityChat mod loaded!
  [Info] Connected to proximity server!
  [Info] Joined room: XXXXXX as YourName
  ```

---

## הגדרות מתקדמות

### שינוי מרחק השמיעה

ב-`mod/ProximityChatManager.cs`:
```csharp
private const float PROXIMITY_DISTANCE = 2.5f;
```
- הגדל את המספר → שומעים מרחוק יותר
- הקטן את המספר → צריך להיות קרוב מאוד

**חשוב:** שנה גם ב-`server/index.js`:
```js
const PROXIMITY_DISTANCE = 2.5;
```
שני הערכים חייבים להיות זהים.

### שינוי תדירות עדכון המיקום

ב-`mod/ProximityChatManager.cs`:
```csharp
private const float POSITION_SEND_INTERVAL = 0.1f; // שניות
```
- `0.1` = 10 עדכונים בשנייה (ברירת מחדל)
- `0.05` = 20 עדכונים בשנייה (חלק יותר, יותר עומס)
- `0.2` = 5 עדכונים בשנייה (פחות חלק, פחות עומס)

### שינוי פורט השרת

ב-`server/index.js`:
```js
const PORT = process.env.PORT || 3000;
```
שנה את `3000` לפורט אחר אם צריך.

---

## פתרון בעיות

### "Build succeeded" לא מופיע בבנייה
- ודא שהנתיב ב-`ProximityChat.csproj` נכון
- ודא ש-BepInEx הותקן ושהרצת את המשחק לפחות פעם אחת
- ודא שהתיקיות `BepInEx\core` ו-`BepInEx\interop` קיימות

### המוד לא מופיע ב-LogOutput.log
- ודא שהעתקת את `ProximityChat.dll` לתיקיית `BepInEx\plugins`
- ודא שהרצת את המשחק לאחר ההעתקה

### "Connected to proximity server" לא מופיע
- ודא שהשרת רץ (`npm start`)
- ודא שה-`SERVER_URL` במוד נכון
- אם מתחברים מרחוק - ודא שפורט 3000 פתוח ב-router

### שגיאת "Access denied" בפורט
- הרץ את cmd כ-Administrator
- או שנה את הפורט ל-8080 (פחות מוגבל)

### המשחק קורס עם המוד
- ודא שגרסת BepInEx היא IL2CPP x64 (לא Mono)
- בדוק את `BepInEx\LogOutput.log` לשגיאות ספציפיות

### חברים לא שומעים אחד את השני
- ודא שכולם מחוברים לאותו שרת (אותו IP ופורט)
- בדוק ב-LogOutput.log שמופיע `Joined room: XXXXXX` עם אותו קוד חדר
- ודא שהמרחק ביניכם קטן מ-2.5 יחידות

---

## מבנה הפרויקט

```
among-us-proximity/
├── README.md                        ← המדריך הזה
├── server/
│   ├── index.js                     ← שרת WebSocket + proximity logic
│   └── package.json                 ← חבילות Node.js
└── mod/
    ├── ProximityChat.csproj         ← הגדרות פרויקט C#
    ├── MyPluginInfo.cs              ← מידע על המוד (שם, גרסה)
    ├── Plugin.cs                    ← נקודת כניסה BepInEx
    ├── ProximityChatManager.cs      ← לוגיקה ראשית + WebSocket client
    ├── WebRTCManager.cs             ← ניהול חיבורי אודיו WebRTC
    └── Patches.cs                   ← Harmony patches (יירוט אירועי משחק)
```
