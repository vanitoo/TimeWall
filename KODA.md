# KODA.md — Инструкция для работы с проектом WallpaperManager

## 1. Обзор проекта

### Назначение

**WallpaperManager** — это WPF-приложение для Windows, предназначенное для автоматической смены обоев рабочего стола. Приложение позволяет использовать как локальные папки с изображениями, так и онлайн-источники (Unsplash API) для получения новых обоев.

### Тип проекта

Это программный проект с кодом на C# (WPF/.NET 8.0).

---

## 2. Структура проекта

```
C:\project\TimeWall\
├── WallpaperManager.sln          # Файл решения Visual Studio
├── WallpaperManager.csproj       # Файл проекта (SDK-style)
├── App.xaml / App.xaml.cs        # Точка входа приложения
├── appsettings.json              # Конфигурация приложения
├── README.md                     # Документация
├── SPEC.md                       # Спецификация проекта
├── LICENSE                       # MIT лицензия
├── launch.ps1                    # Скрипт запуска с проверкой .NET
├── build.ps1 / build.bat         # Скрипты сборки
├── Assets/
│   └── icon.ico                  # Иконка приложения
├── Models/                       # Модели данных
│   ├── ImageInfo.cs
│   └── WallpaperSettings.cs
├── Services/                     # Бизнес-логика
│   ├── Interfaces/               # Контракты сервисов
│   ├── WallpaperService.cs       # Основной сервис смены обоев
│   ├── ImageService.cs           # Управление источниками изображений
│   ├── CacheService.cs           # Кэширование загруженных изображений
│   ├── TimerService.cs           # Таймер для автосмены
│   ├── TrayService.cs            # Системный трей
│   ├── LocalImageSource.cs       # Локальные папки
│   └── OnlineImageSource.cs      # Unsplash API
├── ViewModels/                   # MVVM ViewModels
│   ├── ViewModelBase.cs
│   ├── MainViewModel.cs
│   └── SettingsViewModel.cs
├── Views/                        # WPF представления
│   ├── MainWindow.xaml(.cs)
│   └── SettingsWindow.xaml(.cs)
├── Converters/                   # Конвертеры данных
│   ├── BoolToVisibilityConverter.cs
│   └── EnumToBoolConverter.cs
├── Helpers/                      # Вспомогательные классы
│   ├── Win32Interop.cs           # P/Invoke для SetWallpaper
│   └── AsyncLock.cs
├── .github/
│   └── workflows/
│       └── build.yml             # GitHub Actions CI/CD
└── WallpaperManager/             # Вложенная папка (артефакт, не используется)
```

### Примечание о структуре

Проект был создан с изначальной вложенной папкой `WallpaperManager/`, но основные исходные файлы находятся в корневой директории `C:\project\TimeWall\`. Файл решения `WallpaperManager.sln` ссылается на проект в корне.

---

## 3. Технологический стек

| Компонент | Технология | Версия |
|-----------|------------|--------|
| Платформа | .NET | 8.0 |
| UI-фреймворк | WPF | — |
| MVVM | CommunityToolkit.Mvvm | 8.2.2 |
| Логирование | Serilog | 3.1.1 |
| Системный трей | Hardcodet.NotifyIcon.Wpf | 1.1.0 |
| Целевая ОС | Windows | 10/11 |

---

## 4. Сборка и запуск

### Предварительные требования

- .NET 8.0 SDK (или Desktop Runtime для запуска)
- Windows 10/11
- Visual Studio 2022 или VS Code с расширением C#

### Команды для сборки

```powershell
# Восстановление зависимостей
dotnet restore

# Сборка в режиме Debug
dotnet build

# Сборка в режиме Release
dotnet build -c Release

# Запуск в режиме разработки
dotnet run
```

### Публикация

```powershell
# Framework-dependent (требует .NET 8.0 на ПК)
dotnet publish -c Release -o ./publish

# Self-contained (всё включено, работает без .NET)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-standalone
```

### Использование скрипта запуска

Для удобства запуска можно использовать `launch.ps1`, который проверяет наличие .NET 8.0 и предлагает установить его при необходимости:

```powershell
.\launch.ps1
```

---

## 5. Конфигурация

### Файл настроек

Настройки приложения сохраняются в JSON-файле:
```
%APPDATA%\WallpaperManager\settings.json
```

Пример настроек:
```json
{
  "timerIntervalHours": 2,
  "currentSource": "Online",
  "localFolders": ["C:\\Wallpapers"],
  "onlineCategory": "nature",
  "onlineQuery": "landscape",
  "startWithWindows": false,
  "minimizeToTray": true,
  "unsplashAccessKey": "",
  "onlinePreloadCount": 5
}
```

### Кэш изображений

Кэш хранится в: `%APPDATA%\WallpaperManager\Cache\`
- Максимальный размер: 500MB
- Автоматическая очистка при превышении лимита (LRU)

### Логи

Логи хранятся в: `%APPDATA%\WallpaperManager\logs\`

---

## 6. Архитектура

### Слои приложения

1. **Presentation Layer** (Views + ViewModels)
   - WPF представления (MainWindow, SettingsWindow)
   - MVVM через CommunityToolkit.Mvvm

2. **Service Layer**
   - WallpaperService — основной сервис смены обоев
   - ImageService — управление источниками с fallback
   - CacheService — кэширование
   - TimerService — таймер автосмены
   - TrayService — системный трей

3. **Data Layer**
   - LocalImageSource — чтение из локальных папок
   - OnlineImageSource — Unsplash API
   - SettingsService — сохранение настроек в JSON

### Паттерны

- **MVVM** — через CommunityToolkit.Mvvm с атрибутами [ObservableProperty] и [RelayCommand]
- **Dependency Injection** — ручное внедрение зависимостей в App.xaml.cs
- **Repository** — для работы с настройками (SettingsService)
- **P/Invoke** — для вызова Win32 API (SetWallpaper)

---

## 7. Ключевые функции

- ✅ Автоматическая смена обоев по таймеру (1-24 часа)
- ✅ Поддержка локальных папок с изображениями
- ✅ Онлайн-источник через Unsplash API
- ✅ Поиск по категории или ключевым словам
- ✅ Предзагрузка 5 следующих изображений
- ✅ Кэширование онлайн-изображений (до 500MB)
- ✅ Системный трей с меню
- ✅ Предпросмотр текущего изображения
- ✅ Сохранение настроек в JSON
- ✅ Fallback между онлайн и локальным источником
- ✅ Запуск при старте Windows (опционально)
- ✅ Сворачивание в трей при закрытии

---

## 8. CI/CD

Проект использует GitHub Actions для автоматической сборки и публикации релизов.

**Workflow**: `.github/workflows/build.yml`

Триггер: пуш тега вида `v*` (например `v1.0.0`)

Процесс:
1. Сборка .NET 8.0
2. Публикация Framework-dependent и Self-contained
3. Создание GitHub Release с ассетами

---

## 9. Правила разработки

### Стиль кодирования

- Использовать **nullable reference types** (`<Nullable>enable</Nullable>`)
- Все сервисы должны иметь интерфейсы в `Services/Interfaces/`
- Использовать асинхронные методы для I/O операций
- Логирование через Serilog для всех значимых операций

### Тестирование

Тесты не реализованы (TODO: добавить unit-тесты)

### Внесение изменений

1. Создать ветку от `master`
2. Внести изменения
3. Создать Pull Request
4. После merge тегировать релиз: `git tag v1.0.0 && git push origin v1.0.0`

---

## 10. Известные проблемы

- При запуске приложение может быть заблокировано антивирусом (требуется добавить в исключения)
- Для работы онлайн-источника требуется API ключ Unsplash
- Self-contained сборка имеет большой размер (~80MB)

---

## 11. Лицензия

Проект распространяется под **MIT License** — см. файл `LICENSE`.
