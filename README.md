<div align="center">

# ⚓ MT RINA (IMO: 9152820) Cargo Plan

**Грузовой план танкера / Tanker Cargo Plan**

WPF-приложение для составления грузового плана танкера MT RINA:
расчёт загрузки грузовых и балластных танков, сводка по грузам и печать плана.

*A WPF application for creating the cargo plan of the MT RINA tanker:
cargo & ballast tank loading calculation, grade summary and plan printing.*

<br>

🇬🇧 **[English](#-english)**  |  🇷🇺 **[Русский](#-русский)**

<br>

[![Download for Windows x64](https://img.shields.io/badge/⬇%20Download_for_Windows_x64-2EA44F?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/woodyshats/Cargo-Plan-MT-Rina/releases/tag/Win_x64_v1_01)

<br>

![.NET](https://img.shields.io/badge/.NET-9.0--windows-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-WPF%20%2B%20WPF--UI%20(Fluent)-0078D4)
![JSON](https://img.shields.io/badge/Data-JSON-f5c518?logo=json&logoColor=black)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows&logoColor=white)

</div>

---

## 🇬🇧 English

### 📖 Overview

**MT RINA Cargo Plan** helps a ship's Chief Officer prepare a cargo plan for a product tanker:
enter cargo grades, weights and temperatures per tank — the app automatically calculates
volumes, filling percentages and actual densities, shows a visual tank plan and prints a report.
Built with WPF and WPF-UI for a native Windows 11 (Fluent) look.

### ✨ Features

- **Cargo tanks table** — 16 tanks (COT 1–7P/S, SLOP P/S): grade selection, weight (VAC, mt), temperature; volume, fill percentage and density at temperature are calculated automatically.
- **VCF calculation** — volume correction factor by ASTM-style exponential formula from density at 15 °C and temperature.
- **Ballast tanks** — water volumes for 13 ballast tanks (FPK, WBT 1–5 P/S, WWT P/S).
- **Grade summary** — grouping by cargo grade with total weight, volume and weighted average temperature, plus a TOTAL row.
- **Visual plan section** — graphical tank layout with color-coded grades and voyage details (plan type, date, voyage no., ports, terminal, draft, water density).
- **Product management** — catalog of cargoes (name, density at 15 °C, color coding), stored in `settings.json`.
- **Tank capacities** — set maximum tank volumes used for fill-percentage calculations.
- **Printing** — print preview of the cargo plan (voyage header, tank scheme, summary table).
- **File operations** — New / Open / Save / Save As — plans are stored as JSON files.
- **Native Windows 11 UI** — Fluent design (WPF-UI) with Mica backdrop and sidebar navigation; spreadsheet-like cell navigation (arrows / Tab / Enter).

### 🛠 Technology

| Component | Version |
|---|---|
| .NET (WPF) | net9.0-windows |
| WPF-UI (Fluent) | 4.3.0 |
| Newtonsoft.Json | 13.0.3 |

Architecture — MVVM:

```
CargoPlanApp/
├── Models/       # Data models (Tank, BallastTank, Product, CargoPlanData)
├── ViewModels/   # MainViewModel — business logic and commands
├── Views/        # Windows and controls (FluentWindow dialogs, CargoPlanView)
├── Services/     # JsonDataService — JSON save/load
├── Behaviors/    # ExcelNavigationBehavior — spreadsheet-like cell navigation
├── Converters/   # WPF converters (color → brush, string ↔ number)
├── Styles/       # FluentDataGridStyles — shared DataGrid style
├── MainWindow    # FluentWindow + NavigationView sidebar
└── App.xaml      # Entry point, WPF-UI theme and resources
```

### 🔨 Build & Run

Requires Windows 10/11 and the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
(.NET Desktop Runtime 9.0 is enough to run).

```bash
git clone https://github.com/woodyshats/Cargo-Plan-MT-Rina.git
cd Cargo-Plan-MT-Rina

dotnet build
dotnet run
```

Or open `CargoPlanApp.sln` in Visual Studio 2022 and press F5.

### 🚀 Usage

1. **Products** — fill in the cargo catalog (name, density at 15 °C, color coding).
2. **Tanks' capacities** — enter maximum tank volumes in m³.
3. On the **Table** section pick a cargo for each tank and enter weight (VAC, mt) and temperature — volume, fill and density are recalculated automatically. Set ballast volumes on the left.
4. On the **Cargo Plan** section fill in voyage details and review the visual plan.
5. **Save / Save As** — save the plan to a JSON file; **Open** — load a previously saved plan; **New** — clear the current plan.
6. **Print** — preview and print the cargo plan.

Sections and commands (Products, Tanks' capacities, New/Open/Save/Save As/Print) are opened from the left sidebar.

If you close the app with unsaved changes, you will be asked to save them.

### 📄 Plan File Format (JSON)

```jsonc
{
  "Products": [ { "Name": "...", "Density15": 0.0, "Color": "..." } ],
  "TankMaxVolumes": { "COT 1P": 0.0 },
  "TankStates": [ { "TankNo": "COT 1P", "Grade": "...", "Weight": 0.0, "Temperature": 0.0 } ],
  "BallastStates": [ { "TankNo": "FPK", "WaterVolume": 0.0 } ],
  "VesselInfo": {
    "Date": "2026-01-01", "Voyage": "...", "PortOfLoading": "...",
    "Terminal": "...", "PortOfDischarge": "...", "PlanType": "...",
    "Draft": 0.0, "WaterDensity": 0.0
  }
}
```

---

## 🇷🇺 Русский

### 📖 О проекте

**MT RINA Cargo Plan** помогает Старшему Помощнику подготовить грузовой план танкера:
укажите сорта груза, вес и температуру по танкам — приложение автоматически рассчитает
объёмы, проценты заполнения и фактическую плотность, отобразит визуальный план танков
и распечатает отчёт. Построено на WPF и WPF-UI — нативный вид Windows 11 (Fluent).

### ✨ Возможности

- **Таблица грузовых танков** — 16 танков (COT 1–7P/S, SLOP P/S) с указанием груза, веса, температуры; объём, процент заполнения и фактическая плотность рассчитываются автоматически.
- **Расчёт VCF** — коэффициент пересчёта объёма по ASTM (экспоненциальная формула от плотности при 15 °C и температуры).
- **Балластные танки** — учёт объёмов воды в 13 балластных танках (FPK, WBT 1–5 P/S, WWT P/S).
- **Сводка по грузам** — группировка по сортам груза с суммарным весом, объёмом и средневзвешенной температурой, итоговая строка TOTAL.
- **Визуальный план** — раздел «Cargo Plan» с графическим представлением танков, цветовой маркировкой грузов, данными о рейсе (тип плана, дата, номер рейса, порты, терминал, осадка, плотность воды).
- **Управление продуктами** — справочник грузов (наименование, плотность при 15 °C, цветовая кодировка), сохраняется в `settings.json`.
- **Ёмкости танков** — задание максимальных объёмов танков для расчёта процента заполнения.
- **Печать** — предпросмотр печатной формы грузового плана (заголовок рейса, схема танков, сводная таблица).
- **Работа с файлами** — New / Open / Save / Save As — планы хранятся в формате JSON.
- **Нативный UI Windows 11** — Fluent-дизайн (WPF-UI) с Mica-подложкой и боковым меню-навигацией; навигация по ячейкам как в таблице (стрелки / Tab / Enter).

### 🛠 Технологии

| Компонент | Версия |
|---|---|
| .NET (WPF) | net9.0-windows |
| WPF-UI (Fluent) | 4.3.0 |
| Newtonsoft.Json | 13.0.3 |

Архитектура — MVVM:

```
CargoPlanApp/
├── Models/       # Модели данных (Tank, BallastTank, Product, CargoPlanData)
├── ViewModels/   # MainViewModel — бизнес-логика и команды
├── Views/        # Окна и элементы управления (FluentWindow-диалоги, CargoPlanView)
├── Services/     # JsonDataService — сохранение/загрузка JSON
├── Behaviors/    # ExcelNavigationBehavior — навигация по ячейкам как в таблице
├── Converters/   # Конвертеры WPF (цвет → кисть, строка ↔ число)
├── Styles/       # FluentDataGridStyles — общий стиль таблиц
├── MainWindow    # FluentWindow + боковое меню (NavigationView)
└── App.xaml      # Точка входа, тема и ресурсы WPF-UI
```

### 🔨 Сборка и запуск

Требуется Windows 10/11 и [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
(для запуска достаточно .NET Desktop Runtime 9.0).

```bash
git clone https://github.com/woodyshats/Cargo-Plan-MT-Rina.git
cd Cargo-Plan-MT-Rina

dotnet build
dotnet run
```

Или откройте `CargoPlanApp.sln` в Visual Studio 2022 и нажмите F5.

### 🚀 Использование

1. **Products** — заполните справочник грузов (название, плотность при 15 °C, цветовая кодировка).
2. **Tanks' capacities** — задайте максимальные объёмы танков в м³.
3. В разделе **Table** выберите груз в каждом танке, введите вес (VAC, mt) и температуру — объём, заполнение и плотность пересчитаются автоматически. Слева укажите балласт.
4. В разделе **Cargo Plan** заполните данные рейса и посмотрите визуальный план.
5. **Save / Save As** — сохранение плана в JSON-файл; **Open** — загрузка ранее сохранённого плана; **New** — очистка текущего плана.
6. **Print** — предпросмотр и печать грузового плана.

Разделы и команды (Products, Tanks' capacities, New/Open/Save/Save As/Print) открываются из бокового меню слева.

При закрытии приложения с несохранёнными изменениями будет предложено их сохранить.

### 📄 Формат файла плана (JSON)

```jsonc
{
  "Products": [ { "Name": "...", "Density15": 0.0, "Color": "..." } ],
  "TankMaxVolumes": { "COT 1P": 0.0 },
  "TankStates": [ { "TankNo": "COT 1P", "Grade": "...", "Weight": 0.0, "Temperature": 0.0 } ],
  "BallastStates": [ { "TankNo": "FPK", "WaterVolume": 0.0 } ],
  "VesselInfo": {
    "Date": "2026-01-01", "Voyage": "...", "PortOfLoading": "...",
    "Terminal": "...", "PortOfDischarge": "...", "PlanType": "...",
    "Draft": 0.0, "WaterDensity": 0.0
  }
}
```

---

<div align="center">

Made for seafarers · Сделано для моряков

</div>
