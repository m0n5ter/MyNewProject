# Урок 8. DataTemplate как навигация + Dependency Injection

Ветка: `lesson-08-di`. Точка «до» — коммит `ffc80fd margins`.

```
git diff ffc80fd..lesson-08-di
```

На уроке 7 `DataTemplate` подбирал **вид фигуры** по типу view model. Сегодня
тот же самый механизм подбирает **целый экран**: `ContentControl` + неявный
шаблон = навигация без единого `Visibility`, без единого конвертера и без
единого `if` в разметке.

А когда экраны начнут создаваться и уничтожаться по ходу работы, вылезет
второй вопрос: **кто их создаёт**. Ответ — не `MainViewModel`. Отсюда вторая
половина урока: контейнер зависимостей.

Одна фраза, которую стоит записать на доске в начале:

> Тип view model — это адрес экрана. `DataTemplate` — таблица маршрутизации.
> DI-контейнер — тот, кто умеет этот экран собрать.

## Тайминг (120 минут)

| Время | Часть |
|---|---|
| 0:00–0:10 | Часть 0. Что не так с текущим `MainWindow` |
| 0:10–0:35 | Часть 1. `ContentControl` + `DataTemplate` = навигация |
| 0:35–0:50 | Часть 2. Вкладки через `ItemsSource`: заголовок и содержимое |
| 0:50–1:00 | Перерыв |
| 1:00–1:20 | Часть 3. Остальное про шаблоны: словари, `TreeView`, переиспользование |
| 1:20–1:50 | Часть 4. Dependency Injection |
| 1:50–2:00 | Часть 5. Подводные камни + ДЗ |

Если группа вязнет в Частях 1–3 — DI переносим целиком на урок 9. Части 1–3
самодостаточны, «полу-DI» на пять минут делать не надо.

---

## Часть 0. Стартовая точка (10 мин)

Открываем `MainWindow.xaml` и читаем его вслух. Там сейчас так:

```xml
<Grid Visibility="{Binding IsLoginVisible, Converter={StaticResource BooleanToVisibilityConverter}}">
    <views:LoginView DataContext="{Binding Login}"/>
</Grid>

<DockPanel Visibility="{Binding IsLoggedIn, Converter={StaticResource BooleanToVisibilityConverter}}">
    ...
    <views:TodoListView DataContext="{Binding TodoList}"/>
    <views:ImageView DataContext="{Binding Image}"/>
</DockPanel>
```

Вопрос аудитории: что здесь плохо? Собрать ответы, дополнить:

1. **Оба экрана существуют всегда.** `Visibility="Collapsed"` — это невидимый,
   но живой визуальный элемент со всеми привязками. Логин-форма продолжает
   висеть в памяти всё время работы приложения.
2. **Третий экран = ещё один флаг.** Два экрана — два булевых свойства
   (`IsLoggedIn`, `IsLoginVisible`). Пять экранов — либо пять флагов, которые
   надо синхронизировать руками, либо перечисление и пять конвертеров.
   Состояние «какой экран открыт» размазано.
3. **`DataContext="{Binding Login}"` в разметке.** Родитель вручную раздаёт
   кусочки себя детям. Забыли одну строчку — привязка молча умирает
   (ровно это и было на уроке 7 со вкладкой Image).
4. **`new` в `MainViewModel`** — комментарий, который мы сами оставили:

```csharp
// Lesson 7: ... Lesson 8 replaces it with dependency injection.
public ImageViewModel Image { get; } = new();
```

5. **Состояние прошлого пользователя чистится вручную:**

```csharp
private void Logout()
{
    ...
    TodoList.Clear();   // не забыть. а если полей станет пять?
}
```

Пункты 1–3 лечит `DataTemplate`. Пункты 4–5 — DI и время жизни объектов.
Причём пятый исчезнет сам собой, и это лучший момент урока.

---

## Часть 1. `ContentControl` + `DataTemplate` = навигация (25 мин)

### 1.1 Идея за одну минуту

`ContentControl` умеет ровно одно: взять объект из `Content` и найти для него
`DataTemplate`. Ищет он **по типу времени выполнения** — тот же самый механизм,
что рисовал эллипсы и прямоугольники на уроке 7.

Значит, если во view model лежит свойство `CurrentViewModel` типа
`ViewModelBase`, то смена экрана — это одно присваивание:

```csharp
CurrentViewModel = _login;        // показан LoginView
CurrentViewModel = workspace;     // показан WorkspaceView
```

Ни одного упоминания вида. View model **не знает**, что для неё существует
`LoginView`. Она вообще не знает, что существует WPF.

### 1.2 Общий базовый класс

Нужен один тип, который может лежать и в свойстве, и в коллекции вкладок:

```csharp
// ViewModels/ViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

/// <summary>
/// Общий предок всех экранов. Ничего, кроме заголовка, не добавляет -
/// он нужен как ТИП: чтобы свойство CurrentViewModel и коллекция вкладок
/// могли хранить любой экран, а WPF подбирал шаблон по фактическому типу.
/// </summary>
internal abstract class ViewModelBase : ObservableObject
{
    public abstract string Title { get; }
}
```

Разговор на два предложения: почему `abstract class`, а не `IViewModel`?
Потому что **неявные шаблоны не ищутся по интерфейсам** — только по классу и
его базам (пункт из подводных камней урока 7, самое время его напомнить).
Плюс `ObservableObject` всё равно класс, множественного наследования нет.

Дальше `LoginViewModel`, `TodoListViewModel`, `ImageViewModel` наследуем от
`ViewModelBase` вместо `ObservableObject` и добавляем `Title`:

```csharp
internal partial class TodoListViewModel : ViewModelBase
{
    public override string Title => "Todo List";
    ...
}
```

### 1.3 Экран «после логина» становится отдельной view model

Сейчас всё, что видно после входа, живёт прямо в `MainWindow.xaml` и в
`MainViewModel`. Выделяем это в `WorkspaceViewModel`:

```csharp
// ViewModels/WorkspaceViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Рабочее пространство залогиненного пользователя: шапка и вкладки.
/// Живёт ровно один сеанс - создаётся при входе, выбрасывается при выходе.
/// </summary>
internal partial class WorkspaceViewModel : ViewModelBase
{
    public WorkspaceViewModel(string userName, TodoListViewModel todoList, ImageViewModel image)
    {
        UserName = userName;
        Tabs = [todoList, image];
        SelectedTab = todoList;
    }

    public override string Title => "Workspace";

    public string UserName { get; }

    // Коллекция ViewModelBase, а не двух конкретных типов: добавить третью
    // вкладку = добавить элемент и написать для неё DataTemplate. Больше нигде.
    public ObservableCollection<ViewModelBase> Tabs { get; }

    [ObservableProperty]
    public partial ViewModelBase SelectedTab { get; set; }

    // Ребёнок не выходит из системы сам - он сообщает, что его об этом попросили.
    // Решение принимает родитель. Ровно как LoginViewModel.LoginSucceeded.
    public event Action? LogoutRequested;

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke();
}
```

### 1.4 `MainViewModel` худеет до маршрутизатора

```csharp
internal partial class MainViewModel : ViewModelBase
{
    private readonly LoginViewModel _login;
    private readonly Func<string, WorkspaceViewModel> _workspaceFactory;

    public MainViewModel(LoginViewModel login, Func<string, WorkspaceViewModel> workspaceFactory)
    {
        _login = login;
        _workspaceFactory = workspaceFactory;
        _login.LoginSucceeded += OnLoginSucceeded;

        CurrentViewModel = _login;
    }

    public override string Title => "Todo";

    // Одно свойство вместо IsLoggedIn + IsLoginVisible + двух конвертеров.
    [ObservableProperty]
    public partial ViewModelBase CurrentViewModel { get; private set; }

    private void OnLoginSucceeded(string userName)
    {
        var workspace = _workspaceFactory(userName);
        workspace.LogoutRequested += OnLogoutRequested;
        CurrentViewModel = workspace;
    }

    private void OnLogoutRequested()
    {
        // Отписаться обязательно: иначе старый workspace останется живым
        // из-за ссылки из его же события на нас. Классическая утечка.
        if (CurrentViewModel is WorkspaceViewModel old)
            old.LogoutRequested -= OnLogoutRequested;

        _login.Reset();
        CurrentViewModel = _login;
    }
}
```

**Это ключевой момент урока.** Сравнить со старым `Logout()`:

```csharp
// было
IsLoggedIn = false;
CurrentUser = string.Empty;
Login.Reset();
TodoList.Clear();     // и не забыть добавить сюда всё будущее состояние
```

Вызова `Clear()` больше нет. Список задач следующего пользователя пуст не
потому, что мы его почистили, а потому, что **это другой объект**. Правильное
время жизни объекта убирает целый класс ошибок «забыли сбросить поле».

Вопрос аудитории: а почему `Func<string, WorkspaceViewModel>`, а не просто
`new WorkspaceViewModel(userName, ...)`? Ответ пока честный: «потому что
`MainViewModel` не должна знать, из чего собирается workspace». Как это
устроено технически — Часть 4. До неё в `App.xaml.cs` можно временно положить
лямбду руками, чтобы всё собиралось.

### 1.5 Шаблоны переезжают в отдельный словарь

```xml
<!-- Views/ViewTemplates.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:views="clr-namespace:MyNewProject.Views"
                    xmlns:viewModels="clr-namespace:MyNewProject.ViewModels">

    <!-- Таблица маршрутизации приложения: тип данных -> вид.
         Единственное место, где «логин» и «LoginView» встречаются вместе. -->

    <DataTemplate DataType="{x:Type viewModels:LoginViewModel}">
        <views:LoginView Width="240"
                         HorizontalAlignment="Center"
                         VerticalAlignment="Center"/>
    </DataTemplate>

    <DataTemplate DataType="{x:Type viewModels:WorkspaceViewModel}">
        <views:WorkspaceView/>
    </DataTemplate>

    <DataTemplate DataType="{x:Type viewModels:TodoListViewModel}">
        <views:TodoListView Margin="20"/>
    </DataTemplate>

    <DataTemplate DataType="{x:Type viewModels:ImageViewModel}">
        <views:ImageView Margin="20"/>
    </DataTemplate>

</ResourceDictionary>
```

Обратить внимание: **`DataContext` нигде не задан**. `ContentControl` ставит
его сам — содержимое шаблона получает тот объект, ради которого шаблон и был
выбран. Строчки вида `DataContext="{Binding Login}"` из `MainWindow.xaml`
удаляются, а вместе с ними — целый источник молчаливых ошибок.

Подключаем словарь:

```xml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Views/ViewTemplates.xaml"/>
        </ResourceDictionary.MergedDictionaries>

        <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
        <converters:BooleanToVisibilityHiddenConverter x:Key="BooleanToVisibilityHiddenConverter"/>
    </ResourceDictionary>
</Application.Resources>
```

> **Ловушка, на которую попадаются все.** Как только появился
> `MergedDictionaries`, все остальные ресурсы обязаны лежать **внутри того же
> одного** `<ResourceDictionary>`. Оставите конвертеры снаружи —
> `The property 'Resources' is set more than once`.

### 1.6 `MainWindow.xaml` сжимается до одной строки

```xml
<Window x:Class="MyNewProject.MainWindow"
        ...
        Title="{Binding Title}" Height="560" Width="640">

    <!-- Вся навигация приложения. Content - это ДАННЫЕ (view model),
         вид для них WPF найдёт сам по типу. -->
    <ContentControl Content="{Binding CurrentViewModel}" Margin="20"/>
</Window>
```

И удаляем:

```xml
<!-- этого больше нет: конструктор MainViewModel требует аргументы,
     а XAML умеет вызывать только конструктор без параметров -->
<Window.DataContext>
    <viewModels:MainViewModel/>
</Window.DataContext>
```

`DataContext` окну теперь выдаёт тот, кто окно создал (Часть 4). До неё —
временно в `App.OnStartup`.

Запускаем. Демонстрируем вход и выход. Показываем в Live Visual Tree, что
логин-формы в дереве **физически нет**, пока показан workspace, — в отличие
от старого варианта с `Collapsed`.

### 1.7 Демо-ошибка: явный шаблон побеждает неявный

Дописать на `ContentControl`:

```xml
<ContentControl Content="{Binding CurrentViewModel}"
                ContentTemplate="{StaticResource SomeTemplate}"/>
```

— и подбор по типу выключается навсегда. То же правило, что с `ItemTemplate`
на уроке 7: **явно заданный шаблон отключает неявный подбор**. Смешивать
нельзя, надо выбрать одно.

Второй вариант той же ошибки, менее очевидный:

```xml
<ContentControl Content="{Binding CurrentViewModel.Title}"/>
```

Строка — не наш тип, шаблона для неё нет, WPF рисует `ToString()`. Никакой
ошибки в Output: `ContentControl` всегда «работает», просто иногда показывает
`MyNewProject.ViewModels.LoginViewModel`. Увидели имя класса на экране —
значит, шаблон для типа не нашёлся. Это диагноз номер один.

---

## Часть 2. Вкладки: заголовок и содержимое — разные шаблоны (15 мин)

Раньше вкладки были прибиты в разметке:

```xml
<TabItem Header="Todo List">
    <views:TodoListView DataContext="{Binding TodoList}"/>
</TabItem>
```

Теперь вкладки — это данные:

```xml
<!-- Views/WorkspaceView.xaml -->
<DockPanel>

    <DockPanel DockPanel.Dock="Top" Margin="0,0,0,10">
        <Button DockPanel.Dock="Right" Padding="10,2" Command="{Binding LogoutCommand}">Logout</Button>
        <TextBlock FontSize="18" VerticalAlignment="Center">
            Todo list of <Run Text="{Binding UserName, Mode=OneWay}" FontWeight="Bold"/>
        </TextBlock>
    </DockPanel>

    <TabControl ItemsSource="{Binding Tabs}"
                SelectedItem="{Binding SelectedTab}">

        <!-- ВНИМАНИЕ: ItemTemplate у TabControl - это шаблон ЗАГОЛОВКА вкладки,
             а не её содержимого. Ошибка, на которой все спотыкаются
             ровно один раз в жизни. -->
        <TabControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Title}"/>
            </DataTemplate>
        </TabControl.ItemTemplate>

        <!-- ContentTemplate НЕ задан специально: содержимое выбранной вкладки
             рисуется неявным шаблоном по типу - TodoListViewModel даст
             TodoListView, ImageViewModel даст ImageView. -->
    </TabControl>
</DockPanel>
```

Демонстрация «почему так»: временно задать `ContentTemplate` тем же шаблоном
с `TextBlock` — и внутри вкладки появится текст «Todo List» вместо списка.
Два шаблона, две роли, один `ItemsSource`.

Проверить вживую то, ради чего всё затевалось: **добавить третью вкладку**.

```csharp
Tabs = [todoList, image, settings];
```

плюс `DataTemplate` для `SettingsViewModel`. Разметка `WorkspaceView.xaml`
не меняется вообще. Сравнить с тем, что пришлось бы дописывать в старом
варианте с `TabItem`.

> Побочный эффект, который стоит проговорить: `TabControl` по умолчанию
> **уничтожает** содержимое невыбранной вкладки (у него один
> `ContentPresenter` на всех). Переключились туда-обратно — прокрутка,
> выделение и фокус внутри вкладки сброшены. Состояние при этом цело: оно
> во view model, а не в контролах. Ещё один аргумент за MVVM, которого нет
> в проектах с состоянием в code-behind.

---

## Перерыв 10 мин

---

## Часть 3. Остальное про шаблоны (20 мин)

### 3.1 Где объявлять шаблон

| Где | Видно | Когда так делать |
|---|---|---|
| `UserControl.Resources` | внутри контрола | шаблоны фигур из урока 7 |
| отдельный `ResourceDictionary` + merge в `App.xaml` | во всём приложении | экраны, вкладки |
| `Window.Resources` | в окне | почти никогда |

Правило: шаблон живёт как можно ближе к тому, кто его использует, но не ближе.
`ViewTemplates.xaml` глобален не потому, что «так удобнее», а потому, что
навигация — свойство приложения целиком.

> Шаблон ищется вверх по **логическому дереву**. `ContextMenu`, `ToolTip` и
> `Popup` живут в отдельном дереве — ресурсы `UserControl` им не видны, и
> шаблон надо класть в `App.xaml`. Это тот самый случай «работает везде,
> кроме контекстного меню», на который уходит полдня.

### 3.2 `HierarchicalDataTemplate` — тот же приём для деревьев

Если данные вложенные, шаблон умеет объявить, где лежат дети:

```xml
<HierarchicalDataTemplate DataType="{x:Type viewModels:FolderViewModel}"
                          ItemsSource="{Binding Children}">
    <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
</HierarchicalDataTemplate>

<DataTemplate DataType="{x:Type viewModels:FileViewModel}">
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>
```

`TreeView` с `ItemsSource="{Binding Roots}"` дальше строится сам, на любую
глубину, с разными типами узлов вперемешку. Кода — ноль.

### 3.3 `DataTemplateSelector` — повторение с уточнением

С урока 7: селектор нужен для выбора **по данным**, а не по типу. По типу уже
работает `DataType`. Уточнение, которое стоит добавить сегодня:

* селектор вызывается **при создании контейнера**, а не при каждом изменении
  свойства. Изменилось `Area` — шаблон сам собой не переподберётся;
* если нужен переподбор «на лету» — это `DataTrigger` внутри шаблона, а не
  селектор.

### 3.4 Переиспользование контейнеров

```xml
<ListBox VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling"/>
```

При `Recycling` один и тот же `ListBoxItem` с уже построенным шаблоном
переиспользуется для **другого** элемента данных. Привязки переедут сами, а
вот всё, что вы запомнили в code-behind или в состоянии самого контрола, —
нет. Правило простое и старое: **состояние живёт во view model**.

Полезно тут же вспомнить `x:Shared="False"`: обычный ресурс — один объект на
всех, и если положить в ресурсы не шаблон, а готовый визуальный элемент, он
физически не сможет появиться в двух местах сразу.

### 3.5 Что мы получили в сумме

Перед переходом к DI зафиксировать на доске:

* экран — это объект, а не флаг;
* смена экрана — присваивание;
* соответствие «данные → вид» описано в одном файле;
* `MainWindow.xaml` — одна строка;
* view model по-прежнему ничего не знает про WPF, её можно тестировать.

И оставшийся вопрос, ради которого вторая половина урока:
**`MainViewModel` всё ещё должна откуда-то взять `WorkspaceViewModel`.**

---

## Часть 4. Dependency Injection (30 мин)

### 4.1 Что не так с `new`

```csharp
public ImageViewModel Image { get; } = new();
```

Три конкретные беды, по одной строке:

1. **Тест.** Чтобы проверить логин, тест обязан создать `ImageViewModel`, а
   через него — хранилище сцены, а через него — файловую систему.
   Зависимости заразны.
2. **Замена реализации.** Хотим `ISceneStorage` в файл в продакшене и в память
   в тестах — придётся править `ImageViewModel`.
3. **Время жизни.** `new` в инициализаторе поля означает «один на всё время
   жизни родителя». Это решение, принятое молча и не отражённое нигде.

Формулировка, которую стоит продиктовать:

> Класс должен **просить** свои зависимости, а не **добывать** их.
> Просит — через конструктор. Добывает — через `new`, статику и синглтоны.

### 4.2 Контейнер

```
dotnet add package Microsoft.Extensions.DependencyInjection
```

```xml
<!-- версию берём под TargetFramework проекта (net10.0-windows) -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
```

В `App.xaml` убираем `StartupUri="MainWindow.xaml"` — окно теперь создаём мы.

```csharp
// App.xaml.cs
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MyNewProject.Services;
using MyNewProject.ViewModels;

namespace MyNewProject;

public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        _services = services.BuildServiceProvider(new ServiceProviderOptions
        {
            // Обе проверки - в Debug обязательно. ValidateOnBuild ловит
            // незарегистрированную зависимость при старте, а не через
            // полчаса работы, когда пользователь откроет редкий экран.
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Сервисы: интерфейс -> реализация. Здесь и только здесь приложение
        // решает, ЧЕМ является ISceneStorage.
        services.AddSingleton<ISceneStorage, JsonSceneStorage>();

        // View models. Время жизни - осознанное решение, см. 4.4.
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddTransient<TodoListViewModel>();
        services.AddTransient<ImageViewModel>();

        // Экран с параметром времени выполнения - через фабрику, см. 4.5.
        services.AddSingleton<Func<string, WorkspaceViewModel>>(sp =>
            userName => ActivatorUtilities.CreateInstance<WorkspaceViewModel>(sp, userName));

        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = _services.GetRequiredService<MainWindow>();
        window.DataContext = _services.GetRequiredService<MainViewModel>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();   // здесь освободятся все IDisposable-сервисы
        base.OnExit(e);
    }
}
```

Всё. `MainViewModel` уже написана правильно — она просит зависимости в
конструкторе. Мы не поменяли в ней ни строчки, просто теперь их кто-то даёт.
Это и есть проверка, что архитектура была верной: **DI добавляется, а не
вживляется хирургически**.

### 4.3 Сервис вместо `new` внутри view model

`ImageViewModel` перестаёт быть источником данных и становится их
потребителем:

```csharp
internal partial class ImageViewModel : ViewModelBase
{
    private readonly ISceneStorage _storage;

    public ImageViewModel(ISceneStorage storage) => _storage = storage;

    public override string Title => "Image";

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _storage.SaveAsync(Shapes, ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;   // урок 6: ошибка - это состояние
        }
    }
}
```

```csharp
// Services/ISceneStorage.cs
using MyNewProject.ViewModels.Shapes;

namespace MyNewProject.Services;

internal interface ISceneStorage
{
    Task SaveAsync(IReadOnlyList<ShapeViewModel> shapes, CancellationToken ct);
    Task<IReadOnlyList<ShapeViewModel>> LoadAsync(CancellationToken ct);
}
```

Полиморфизм при сериализации включается двумя атрибутами на базовом типе:

```csharp
[JsonDerivedType(typeof(EllipseViewModel), "ellipse")]
[JsonDerivedType(typeof(RectangleViewModel), "rectangle")]
internal abstract partial class ShapeViewModel : ObservableObject
```

Список объявлен как `IReadOnlyList<ShapeViewModel>`, поэтому каждый элемент
получает `"$type"`, и после загрузки эллипс остаётся эллипсом.

> **Маленькая ловушка, которую видно только на живом коде.** Вычисляемые
> `Kind`, `Area`, `Description` в файле не нужны — вешаем `[JsonIgnore]`.
> На `Description` работает, на `Kind` и `Area` — **нет**: они абстрактные,
> а `System.Text.Json` читает атрибут с **самого производного** объявления.
> Значит, `[JsonIgnore]` надо повторить на каждом `override`. Хороший момент
> сказать вслух: «наследование атрибутов» — не общее правило, каждая
> библиотека решает это по-своему, и проверяется это запуском, а не памятью.

Проверочный вопрос аудитории: сколько классов надо изменить, чтобы в тестах
сцена сохранялась в память? Ответ: ноль — только строчку регистрации.

### 4.4 Времена жизни

| Регистрация | Сколько экземпляров | Для чего в WPF |
|---|---|---|
| `AddSingleton` | один на приложение | сервисы, `MainViewModel`, окно-оболочка |
| `AddTransient` | новый на каждый запрос | экраны и диалоги |
| `AddScoped` | один на область | сеанс пользователя, если заводить область руками |

`Scoped` в десктопе не бесплатен: областей, в отличие от ASP.NET, никто не
создаёт за вас. Либо вы сами делаете `IServiceScopeFactory.CreateScope()` на
время сеанса, либо `Scoped` не используете. Промежуточных вариантов нет.

**Captive dependency** — главная ошибка новичка. Синглтон, которому в
конструктор дали transient, держит его вечно:

```csharp
services.AddSingleton<MainViewModel>();      // живёт всегда
services.AddTransient<TodoListViewModel>();  // "новый каждый раз"
// но если MainViewModel возьмёт TodoListViewModel в конструктор -
// экземпляр будет ровно один, на всё время жизни приложения
```

Именно поэтому `MainViewModel` берёт **фабрику** `Func<string,
WorkspaceViewModel>`, а не сам `WorkspaceViewModel`. `ValidateScopes = true`
ловит такое для `Scoped`, но для `Transient` — не ловит никто, кроме вас.

### 4.5 Фабрики: когда контейнера мало

Контейнер умеет собирать объект из **зарегистрированных** зависимостей. Имя
пользователя не зарегистрировано и не может быть — оно появляется в рантайме.
Три способа, от простого к правильному:

```csharp
// 1. Func<T> - когда параметров нет, нужна только отложенность создания
services.AddSingleton<Func<TodoListViewModel>>(sp => sp.GetRequiredService<TodoListViewModel>);

// 2. ActivatorUtilities - параметры рантайма + зависимости из контейнера
services.AddSingleton<Func<string, WorkspaceViewModel>>(sp =>
    userName => ActivatorUtilities.CreateInstance<WorkspaceViewModel>(sp, userName));

// 3. Своя фабрика с интерфейсом - когда логика создания нетривиальна
internal interface IWorkspaceFactory
{
    WorkspaceViewModel Create(string userName);
}
```

`ActivatorUtilities.CreateInstance<T>(sp, args)` разбирает конструктор:
что нашлось среди `args` — берёт оттуда, остальное запрашивает у контейнера.
Именно так `WorkspaceViewModel(string userName, TodoListViewModel todoList,
ImageViewModel image)` и собирается.

### 4.6 Чего делать нельзя: Service Locator

```csharp
// ТАК НЕ ДЕЛАЕМ
internal class ImageViewModel
{
    public ImageViewModel()
    {
        _storage = App.Services.GetRequiredService<ISceneStorage>();
    }
}
```

Работает, но: зависимость снова спрятана (конструктор врёт, что её нет),
класс привязан к контейнеру, тест обязан поднять контейнер. Формально это
даже не DI — это глобальная переменная с красивым именем.

Тот же приговор относится к `Ioc.Default` из CommunityToolkit: удобно,
популярно, и это ровно тот же Service Locator.

**Единственные два места**, где обращение к контейнеру законно:
composition root (`App`) и явная фабрика.

### 4.7 Дизайнер и DI

Момент, который обязательно всплывёт: конструкторы теперь с параметрами,
а в разметке стоит

```xml
d:DataContext="{d:DesignInstance viewModels:TodoListViewModel}"
```

Всё в порядке. По умолчанию `d:DesignInstance` **не создаёт** объект, а
подсовывает дизайнеру «пустышку» по форме типа — конструктор не вызывается,
DI ничего не ломает. Сломается только при `d:IsDesignTimeCreatable=True` —
там нужен настоящий конструктор без параметров, и лучше этот флаг не ставить.

Кому нужны живые данные в дизайнере — заводят отдельный
`DesignTimeTodoListViewModel` с фиксированными элементами. Это нормальный
приём, но это класс для дизайнера, а не второй конструктор в рабочем классе.

---

## Часть 5. Подводные камни (10 мин)

### DataTemplate

* **Явный `ContentTemplate` / `ItemTemplate` выключает неявный подбор.**
  Диагноз: на экране имя класса.
* **`TabControl.ItemTemplate` — это заголовок вкладки**, содержимое —
  `ContentTemplate` или неявный шаблон.
* **`x:Key` на шаблоне отключает подбор по типу**, даже если `DataType` задан.
* **По интерфейсам шаблоны не ищутся** — базовый тип должен быть классом.
* **Ресурсы и `MergedDictionaries`**: все остальные ресурсы обязаны быть
  внутри того же `<ResourceDictionary>`.
* **`ContextMenu` / `ToolTip` / `Popup`** — другое логическое дерево, шаблоны
  из `UserControl.Resources` там не видны.
* **`DataContext` внутри шаблона задавать не надо** — его ставит хозяин
  шаблона. Написали руками — сломали.
* **`TabControl` уничтожает содержимое невыбранной вкладки.** Состояние
  должно быть во view model.
* **`VirtualizationMode="Recycling"`** переиспользует контейнеры: всё, что
  вы сохранили в визуальном элементе, переедет к чужим данным.
* **Не забывать отписываться от событий** старой view model при смене экрана —
  иначе она останется в памяти вместе со всем деревом.
* **`[JsonIgnore]` на абстрактном свойстве не действует на `override`** —
  повторять на наследнике (см. 4.3).

### DI

* **Service Locator вместо конструктора** — самая частая «оптимизация».
  `IServiceProvider` во view model — красный флаг на ревью.
* **Captive dependency**: singleton, взявший transient, делает его singleton.
  Для параметров рантайма — фабрика.
* **`AddTransient` для `IDisposable`**: контейнер запоминает такие объекты и
  освобождает только в `Dispose()` провайдера. Transient + `IDisposable` +
  долго живущее приложение = утечка. Либо `Scoped` с явной областью, либо
  освобождаем сами.
* **View model как singleton** — состояние переживает logout. Именно от этого
  мы сегодня избавились, не вернуть бы обратно через регистрацию.
* **`Window` как singleton**: после `Close()` окно нельзя показать снова —
  `InvalidOperationException`. Диалоги — только `Transient`.
* **`ValidateOnBuild` / `ValidateScopes`** включить сразу; без них
  незарегистрированный сервис всплывёт у пользователя, а не у вас.
* **Не регистрировать всё подряд «на всякий случай»** — контейнер должен
  описывать реальные связи, а не быть свалкой.
* **DI — не про контейнер.** Внедрение зависимостей — это конструктор.
  Контейнер лишь избавляет от ручной сборки в одном месте.

---

## Домашнее задание (мостик к уроку 9)

1. **Третий экран.** `SettingsViewModel` + `SettingsView` + `DataTemplate`.
   Условие: `WorkspaceView.xaml` при этом не меняется вообще. Если пришлось
   его тронуть — значит, вкладки где-то остались зашитыми в разметку.

2. **`INavigationService`.** Вынести из `MainViewModel` знание о том, какой
   экран следующий:

   ```csharp
   internal interface INavigationService
   {
       ViewModelBase Current { get; }
       event Action? CurrentChanged;
       void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
   }
   ```

   Внедрить как singleton, `MainViewModel` подписывается на `CurrentChanged`.
   Отдельно подумать и записать ответ в двух предложениях: где теперь живёт
   правило «после логина показываем workspace» и стало ли лучше.

3. **`ISceneStorage` через DI.** Довести до конца ДЗ урока 7: `JsonSceneStorage`
   с полиморфной сериализацией (`[JsonDerivedType]`), зарегистрировать,
   `ImageViewModel` получает его в конструктор. Плюс `InMemorySceneStorage` —
   вторая реализация. Проверить, что переключение между ними — одна строка
   в `ConfigureServices`.

4. **Первый юнит-тест.** Проект `MyNewProject.Tests`, тест на `MainViewModel`:
   после `LoginSucceeded` в `CurrentViewModel` лежит `WorkspaceViewModel`,
   после `LogoutRequested` — снова `LoginViewModel`. Никакого WPF, никакого
   контейнера: фабрику подставляем лямбдой прямо в тесте.

   Если тест написался за пять минут и без `[STAThread]` — архитектура урока
   получилась. С этого и начнём урок 9: тесты, диалоги без `MessageBox`
   и валидация.
