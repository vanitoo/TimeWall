# Wallpaper Manager

Менеджер автоматической смены обоев для Windows.

## Возможности

- ⏱️ Автоматическая смена обоев по таймеру (1-24 часа)
- 🖼️ Поддержка локальных папок с изображениями
- 🌐 Онлайн-источник через Unsplash API
- 🔍 Поиск по категории или ключевым словам
- 💾 Предзагрузка 5 следующих изображений
- 🗄️ Кэширование онлайн-изображений (до 500MB)
- 🔔 Системный трей с меню
- 👁️ Предпросмотр текущего изображения
- ⚙️ Сохранение настроек в JSON
- 🔄 Fallback между онлайн и локальным источником

## Требования

- .NET 8.0 SDK
- Windows 10/11

## Установка и запуск в VS Code

### Предварительные требования

1. Установите [VS Code](https://code.visualstudio.com/)
2. Установите расширение **C# Dev Kit** или **C#** от Microsoft
3. Установите .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### Запуск

```powershell
# Откройте терминал в папке проекта
cd WallpaperManager

# Восстановление зависимостей
dotnet restore

# Запуск в режиме разработки
dotnet run --project WallpaperManager
```

### Публикация

```powershell
# Создание Release сборки
dotnet publish WallpaperManager -c Release -o ./publish

# Запуск опубликованного приложения
./publish/WallpaperManager.exe
```

## Конфигурация

### Настройка Unsplash API (для онлайн-источника)

1. Зарегистрируйтесь на [Unsplash Developers](https://unsplash.com/developers)
2. Создайте новое приложение
3. Скопируйте Access Key
4. В приложении перейдите в "Настройки" и введите ключ

### Файл настроек

Настройки сохраняются в: `%APPDATA%\WallpaperManager\settings.json`

```json
{
  "timerIntervalHours": 2,
  "currentSource": "Online",
  "localFolders": ["C:\\Wallpapers"],
  "onlineCategory": "nature",
  "onlineQuery": "landscape",
  "startWithWindows": false,
  "minimizeToTray": true,
  "unsplashAccessKey": "your-key-here",
  "onlinePreloadCount": 5
}
```

### Кэш изображений

Кэш хранится в: `%APPDATA%\WallpaperManager\Cache\`

- Максимальный размер: 500MB
- Автоматическая очистка при превышении лимита
- LRU (Least Recently Used) алгоритм вытеснения

### Логи

Логи хранятся в: `%APPDATA%\WallpaperManager\logs\`

## Использование

### Системный трей

- **Следующее фото** - немедленная смена обоев
- **Предпросмотр** - показать главное окно
- **Настройки** - открыть окно настроек
- **Выход** - закрыть приложение

### Горячие клавиши

- Закрытие окна -> сворачивает в трей (если включено в настройках)

## Архитектура

```
WallpaperManager/
├── Models/          # Модели данных
├── Services/        # Бизнес-логика
│   ├── Interfaces/  # Контракты сервисов
│   └── Реализации
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF Views
├── Converters/      # Конвертеры данных
└── Helpers/         # Вспомогательные классы
```

### Паттерны

- **MVVM** - через CommunityToolkit.Mvvm
- **Singleton** - для сервисов
- **Repository** - для настроек

## Технологический стек

- .NET 8.0
- WPF
- CommunityToolkit.Mvvm
- Serilog (логирование)
- Hardcodet.NotifyIcon.Wpf (системный трей)

## Лицензия

MIT License

cd WallpaperManager
dotnet restore
dotnet run --project WallpaperManager