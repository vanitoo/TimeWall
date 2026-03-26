# WallpaperManager - Спецификация

## 1. Архитектурный план

### Слои приложения
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Views     │  │  ViewModels │  │   Converters        │  │
│  │  MainWindow │  │ MainVM      │  │   BoolConverters    │  │
│  │  Settings   │  │ SettingsVM  │  │   ImageConverters   │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Service Layer                             │
│  ┌─────────────────┐  ┌────────────────┐  ┌──────────────┐  │
│  │ WallpaperService│  │ ImageService   │  │ CacheService │  │
│  │ (смена обоев)   │  │ (источники)    │  │ (кэш)        │  │
│  └─────────────────┘  └────────────────┘  └──────────────┘  │
│  ┌─────────────────┐  ┌────────────────┐  ┌──────────────┐  │
│  │ TimerService    │  │ SettingsService│  │ LogService   │  │
│  │ (таймер)        │  │ (JSON настройки)│  │ (Serilog)   │  │
│  └─────────────────┘  └────────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Data Layer                                │
│  ┌─────────────────┐  ┌────────────────┐  ┌──────────────┐  │
│  │ LocalSource     │  │ OnlineSource   │  │ SettingsRepo │  │
│  │ (файлы)         │  │ (Unsplash API) │  │ (JSON файл)  │  │
│  └─────────────────┘  └────────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Паттерны
- **MVVM** - через CommunityToolkit.Mvvm
- **Singleton** - для сервисов (LogService, SettingsService)
- **Factory** - для источников изображений
- **Repository** - для настроек

### Потоки данных
```
Таймер → WallpaperService → ImageService (fallback) → Set Windows Wallpaper
                        ↓
              ImageService.GetNextImage()
                        ↓
              [OnlineSource] → [LocalSource] (fallback)
                        ↓
              CacheService.Preload(5)
```

## 2. Дерево проекта

```
WallpaperManager/
├── WallpaperManager.sln
├── WallpaperManager/
│   ├── WallpaperManager.csproj
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── appsettings.json
│   ├── Assets/
│   │   └── icon.ico
│   ├── Models/
│   │   ├── WallpaperSettings.cs
│   │   ├── ImageSource.cs
│   │   ├── ImageInfo.cs
│   │   └── SourceType.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IWallpaperService.cs
│   │   │   ├── IImageService.cs
│   │   │   ├── IImageSource.cs
│   │   │   ├── ISettingsService.cs
│   │   │   ├── ICacheService.cs
│   │   │   └── ITimerService.cs
│   │   ├── WallpaperService.cs
│   │   ├── ImageService.cs
│   │   ├── LocalImageSource.cs
│   │   ├── OnlineImageSource.cs
│   │   ├── SettingsService.cs
│   │   ├── CacheService.cs
│   │   ├── TimerService.cs
│   │   └── TrayService.cs
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── SettingsWindow.xaml
│   │   └── SettingsWindow.xaml.cs
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   └── EnumToBoolConverter.cs
│   └── Helpers/
│       ├── Win32Interop.cs
│       └── AsyncLock.cs
├── SPEC.md
└── README.md
```

## 3. Технологический стек

- **.NET 8.0** - целевая платформа
- **CommunityToolkit.Mvvm 8.x** - MVVM фреймворк
- **Serilog 3.x** - логирование
- **Serilog.Sinks.File** - файловый лог
- **System.Text.Json** - JSON сериализация
- **Hardcodet.NotifyIcon.Wpf 1.x** - системный трей
- **Windows API** - SetWallpaper через P/Invoke

## 4. Конфигурация

### appsettings.json
```json
{
  "Unsplash": {
    "AccessKey": "",
    "ApiUrl": "https://api.unsplash.com"
  },
  "Cache": {
    "MaxSizeMB": 500,
    "PreloadCount": 5
  },
  "Logging": {
    "LogPath": "logs/wallpaper-.log",
    "Level": "Information"
  }
}
```

## 5. Ключевые функции

### Таймер
- Интервал: 1-24 часа (настраивается)
- Точность: 1 минута
- При выходе из сна/гибернации - перезапуск

### Источники изображений
1. **Локальные папки** - любые директории с изображениями
2. **Unsplash API** - поиск по категории/запросу

### Fallback логика
```
Попытка 1: Online Source (если включён)
    ↓ (если ошибка)
Попытка 2: Local Source (если настроен)
    ↓ (если ошибка)
Ошибка: Логирование + уведомление
```

### Кэширование
- Папка: %APPDATA%\WallpaperManager\Cache
- Предзагрузка: 5 изображений
- Максимальный размер: 500MB (LRU вытеснение)

### Системный трей
- Иконка в трее
- Меню: Следующее фото | Предпросмотр | Настройки | Выход
- Двойной клик: предпросмотр

### Настройки (JSON)
```json
{
  "TimerIntervalHours": 2,
  "CurrentSource": "Online",
  "LocalFolders": ["C:\\Wallpapers"],
  "OnlineCategory": "nature",
  "OnlineQuery": "landscape",
  "LastWallpaperPath": "",
  "StartWithWindows": false,
  "MinimizeToTray": true
}
```
