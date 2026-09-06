# Урок 7. Графический редактор: полиморфизм и DataTemplate

Ветка: `lesson-07-shapes`. Точка «до» — коммит `6f278ff CommunityToolkit`.

```
git diff 6f278ff..lesson-07-shapes
```

Делаем на вкладке **Image** маленький графический редактор: холст, на который
можно добавлять эллипсы и прямоугольники, выделять их, двигать мышью и удалять.

Цель урока не в редакторе. Цель — показать связку:
**иерархия view model → WPF сам подбирает шаблон по типу.**
Ни одного `if (shape is Ellipse)` в разметке и ни одного `switch` во view model,
кроме единственного разрешённого места — фабрики.

## Тайминг (120 минут)

| Время | Часть |
|---|---|
| 0:00–0:10 | Часть 0. Что делаем, и почему вкладка Image сейчас пустая |
| 0:10–0:30 | Часть 1. Иерархия фигур: абстрактный базовый класс |
| 0:30–0:50 | Часть 2. Коллекция и команды |
| 0:50–1:00 | Перерыв |
| 1:00–1:25 | Часть 3. DataTemplate: как WPF выбирает вид по типу |
| 1:25–1:45 | Часть 4. Перетаскивание мышью |
| 1:45–1:55 | Часть 5. Когда неявного шаблона мало: DataTemplateSelector |
| 1:55–2:00 | Часть 6. Подводные камни + ДЗ |

---

## Часть 0. Стартовая точка (10 мин)

Открыть вкладку **Image** и показать, что она сломана. Три факта из живого кода:

```csharp
// MainViewModel
public ImageViewModel Image { get; }   // никогда не присваивается -> null
```

```xml
<!-- MainWindow.xaml -->
<TabItem Header="Image">
    <views:ImageView/>              <!-- DataContext не задан -->
</TabItem>
```

```xml
<!-- ImageView.xaml -->
<TextBox Text="{Binding Image.ImageUrl}"/>   <!-- биндинг в никуда -->
```

Здесь три ошибки сразу, и WPF **молча** проглотил все три: несуществующий путь
привязки — это не исключение, это запись в Output. Показать окно Output и строку
`System.Windows.Data Error: 40 : BindingExpression path error`.

Чиним:

```csharp
public ImageViewModel Image { get; } = new();
```

```xml
<views:ImageView DataContext="{Binding Image}"/>
```

и в `ImageView.xaml` меняем design-time контекст на свой собственный:

```xml
d:DataContext="{d:DesignInstance viewModels:ImageViewModel}"
```

Вопрос аудитории: почему `MainViewModel` вообще должна знать, как создавать
`ImageViewModel`? Ответ: не должна — это тема следующего урока (DI).
Пока честный `new`, но помним, что это временно.

> Имя `ImageViewModel` оставляем, чтобы не переписывать всё дерево.
> В реальном коде оно бы уже называлось `CanvasEditorViewModel` — назвать вещь
> правильно дешевле в момент создания, чем через три урока.

---

## Часть 1. Иерархия фигур (20 мин)

### 1.1 Что общего у эллипса и прямоугольника

Провести с аудиторией разбор вслух, **до** написания кода:

* общее — положение (`X`, `Y`), размер (`Width`, `Height`), выделенность, умение сдвинуться;
* разное — как это рисуется и как считается площадь;
* у прямоугольника есть своё: скругление углов.

Общее едет в базовый класс, разное — в наследников. Это и есть весь дизайн.

### 1.2 Базовый класс

```csharp
namespace MyNewProject.ViewModels.Shapes;

internal abstract partial class ShapeViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial double X { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial double Y { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Area), nameof(Description))]
    public partial double Width { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Area), nameof(Description))]
    public partial double Height { get; set; } = 60;

    /// <summary>Как называется фигура. Каждый наследник отвечает за себя.</summary>
    public abstract string Kind { get; }

    /// <summary>Площадь считается по-разному - классический полиморфизм.</summary>
    public abstract double Area { get; }

    public string Description => $"{Kind} {Width:0}x{Height:0} at ({X:0}, {Y:0}), S = {Area:0}";

    public void MoveBy(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }
}
```

Два момента, которые стоит подчеркнуть:

* класс `abstract`, но обязан быть `partial` — генератор `[ObservableProperty]`
  дописывает в него код. `abstract partial` — нормальное сочетание;
* свойства объявлены в базовом классе один раз, наследникам ничего дописывать
  не надо. `INotifyPropertyChanged` наследуется вместе с ними.

### 1.3 Наследники

```csharp
internal sealed class EllipseViewModel : ShapeViewModel
{
    public override string Kind => "Ellipse";

    // π * a * b
    public override double Area => Math.PI * (Width / 2) * (Height / 2);
}
```

```csharp
internal sealed partial class RectangleViewModel : ShapeViewModel
{
    [ObservableProperty]
    public partial double CornerRadius { get; set; }

    public override string Kind => "Rectangle";

    public override double Area => Width * Height;
}
```

Обратить внимание: `EllipseViewModel` — не `partial`, потому что в нём нет
ни одного `[ObservableProperty]`. `RectangleViewModel` — `partial`, потому что
у него есть собственное свойство. То есть `partial` нужен там, где работает
генератор, а не «на всякий случай».

### 1.4 Что НЕ кладём во view model

Соблазн: добавить `public Brush Fill { get; }` и вернуть `Brushes.Blue` из эллипса.
Так делать не надо:

* `Brush` — тип из `PresentationCore`, то есть view model становится непригодной
  для тестов и невозможной для переиспользования;
* цвет — это оформление, а оформление живёт в шаблоне. Через месяц дизайнер
  захочет градиент, и менять придётся C#, а не XAML.

Если цвет всё-таки логический (пользователь его выбирает) — храним `string` или
`enum`, а превращаем в кисть уже в разметке (конвертером или `DataTrigger`).

---

## Часть 2. Коллекция и команды (20 мин)

`ImageViewModel` целиком переписывается: `ImageUrl` удаляем.

```csharp
internal partial class ImageViewModel : ObservableObject
{
    private static readonly Random Random = new();

    public ObservableCollection<ShapeViewModel> Shapes { get; } = new();

    // Логический размер холста. Не пиксели контрола: view model
    // не должна знать, как её растянули на экране.
    public double CanvasWidth => 520;
    public double CanvasHeight => 300;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(BringToFrontCommand))]
    public partial ShapeViewModel? SelectedShape { get; set; }

    public string StatusText =>
        SelectedShape?.Description ?? $"Shapes: {Shapes.Count}. Nothing selected.";
}
```

### 2.1 Единственный допустимый `switch`

```csharp
[RelayCommand]
private void AddShape(string kind)
{
    // Это фабрика. Тут мы обязаны знать про конкретные типы -
    // кто-то же должен вызвать new. Дальше по коду - только ShapeViewModel.
    ShapeViewModel shape = kind switch
    {
        "Ellipse"   => new EllipseViewModel(),
        "Rectangle" => new RectangleViewModel { CornerRadius = 8 },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown shape")
    };

    shape.X = Random.Next(0, (int)(CanvasWidth - shape.Width));
    shape.Y = Random.Next(0, (int)(CanvasHeight - shape.Height));

    Shapes.Add(shape);
    SelectedShape = shape;
}
```

Правило урока: **`switch` по типу разрешён ровно в одном месте — там, где объект
создаётся.** Если такой `switch` появился где-то ещё, значит в базовом классе
не хватает виртуального члена.

В XAML две кнопки на одну команду:

```xml
<Button Command="{Binding AddShapeCommand}" CommandParameter="Ellipse">Add ellipse</Button>
<Button Command="{Binding AddShapeCommand}" CommandParameter="Rectangle">Add rectangle</Button>
```

Обсудить альтернативу: `enum ShapeKind` вместо строки — безопаснее (опечатка
ловится компилятором), но в XAML требует `CommandParameter="{x:Static ...}"`.
Показать оба варианта, выбрать строку ради читаемости разметки и честно назвать
цену выбора.

### 2.2 Остальные команды

```csharp
[RelayCommand(CanExecute = nameof(HasSelection))]
private void Delete()
{
    Shapes.Remove(SelectedShape!);
    SelectedShape = null;
}

// Z-order - это просто позиция в коллекции: кто позже, тот выше.
[RelayCommand(CanExecute = nameof(HasSelection))]
private void BringToFront()
{
    var shape = SelectedShape!;
    Shapes.Move(Shapes.IndexOf(shape), Shapes.Count - 1);
}

[RelayCommand]
private void Clear()
{
    Shapes.Clear();
    SelectedShape = null;
}

private bool HasSelection() => SelectedShape is not null;
```

`ObservableCollection.Move` вызывает `NotifyCollectionChangedAction.Move`, и
`ItemsControl` переставляет контейнер — анимации и состояние не теряются,
в отличие от `Remove` + `Add`.

### 2.3 Счётчик и статусная строка

Знакомая с урока 5 проблема: `ObservableCollection` уведомляет о добавлении
и удалении, но не о том, что у элемента поменялось свойство. `StatusText` зависит
и от коллекции, и от свойств выделенной фигуры. Минимальное решение:

```csharp
public ImageViewModel()
{
    Shapes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StatusText));
}

partial void OnSelectedShapeChanged(ShapeViewModel? oldValue, ShapeViewModel? newValue)
{
    if (oldValue is not null) oldValue.PropertyChanged -= OnShapeChanged;
    if (newValue is not null) newValue.PropertyChanged += OnShapeChanged;

    OnPropertyChanged(nameof(StatusText));
}

private void OnShapeChanged(object? sender, PropertyChangedEventArgs e)
    => OnPropertyChanged(nameof(StatusText));
```

Заодно показать вторую форму генерируемого хука — `OnXxxChanged(old, new)`,
она удобна именно для перевешивания подписок.

---

## Перерыв 10 мин

---

## Часть 3. DataTemplate: WPF выбирает вид по типу (25 мин)

Ключевая часть урока.

### 3.1 Шаблон без ключа

```xml
<UserControl.Resources>

    <!-- Ни одного x:Key. Ключом служит сам тип. -->
    <DataTemplate DataType="{x:Type shapes:EllipseViewModel}">
        <Ellipse Width="{Binding Width}" Height="{Binding Height}"
                 Fill="#7FB3FF" Stroke="#2C3E50" StrokeThickness="1.5"/>
    </DataTemplate>

    <DataTemplate DataType="{x:Type shapes:RectangleViewModel}">
        <Rectangle Width="{Binding Width}" Height="{Binding Height}"
                   RadiusX="{Binding CornerRadius}" RadiusY="{Binding CornerRadius}"
                   Fill="#FFC98B" Stroke="#2C3E50" StrokeThickness="1.5"/>
    </DataTemplate>

</UserControl.Resources>
```

`DataTemplate` без `x:Key` кладётся в словарь ресурсов под ключом
`DataTemplateKey(тип)`. Дальше любой `ContentPresenter`, которому дали объект
этого типа, находит шаблон сам.

### 3.2 Холст

```xml
<ItemsControl ItemsSource="{Binding Shapes}">

    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <Canvas Width="{Binding CanvasWidth}" Height="{Binding CanvasHeight}"
                    Background="White"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>

    <!-- Позиция задаётся КОНТЕЙНЕРУ, а не содержимому шаблона -->
    <ItemsControl.ItemContainerStyle>
        <Style TargetType="ContentPresenter">
            <Setter Property="Canvas.Left" Value="{Binding X}"/>
            <Setter Property="Canvas.Top"  Value="{Binding Y}"/>
        </Style>
    </ItemsControl.ItemContainerStyle>

</ItemsControl>
```

Обратите внимание: у `ItemsControl` **нет** `ItemTemplate`. Совсем.
Мы нигде не написали, что эллипс рисуется эллипсом — WPF смотрит на рантайм-тип
элемента коллекции и подбирает шаблон. Это и есть полиморфизм, доехавший
до разметки.

### 3.3 Демо-ошибка, которую делают все

Специально перенести позиционирование внутрь шаблона:

```xml
<DataTemplate DataType="{x:Type shapes:EllipseViewModel}">
    <Ellipse Canvas.Left="{Binding X}" Canvas.Top="{Binding Y}" .../>   <!-- не работает -->
</DataTemplate>
```

Все фигуры слипнутся в левом верхнем углу. Причина: непосредственный ребёнок
`Canvas` — это сгенерированный `ContentPresenter`, а `Canvas.Left` читается
именно с прямого ребёнка. Внутри шаблона это свойство никто не смотрит.
Отсюда и `ItemContainerStyle`.

Показать это вживую через Snoop или Live Visual Tree: `Canvas → ContentPresenter → Ellipse`.

### 3.4 Шаблон для базового типа

Добавить третий шаблон:

```xml
<DataTemplate DataType="{x:Type shapes:ShapeViewModel}">
    <Border Width="{Binding Width}" Height="{Binding Height}"
            Background="#EEE" BorderBrush="Red" BorderThickness="1">
        <TextBlock Text="?" HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </Border>
</DataTemplate>
```

и закомментировать шаблон эллипса. Эллипсы превратятся в серые заглушки:
поиск шаблона идёт по точному типу, а если его нет — вверх по цепочке базовых
классов. Практический смысл: новый тип фигуры не ломает приложение, он просто
рисуется заглушкой, пока для него не написали шаблон.

Важное ограничение: по **интерфейсам** неявный шаблон не ищется. Поэтому
базовый абстрактный класс, а не `IShape`.

### 3.5 Выделение: меняем ItemsControl на ListBox

`ItemsControl` ничего не знает про выделение. `ListBox` — это тот же
`ItemsControl` плюс выбор, поэтому меняется только тег:

```xml
<ListBox ItemsSource="{Binding Shapes}"
         SelectedItem="{Binding SelectedShape}"
         Background="White" BorderThickness="0"
         ScrollViewer.HorizontalScrollBarVisibility="Disabled">

    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <Canvas Width="{Binding CanvasWidth}" Height="{Binding CanvasHeight}"/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>

    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="Canvas.Left" Value="{Binding X}"/>
            <Setter Property="Canvas.Top"  Value="{Binding Y}"/>

            <!-- Штатное синее выделение растянуто на всю строку и нам не годится -->
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="ListBoxItem">
                        <Grid>
                            <ContentPresenter/>
                            <Rectangle x:Name="Marker"
                                       Stroke="DodgerBlue" StrokeThickness="1.5"
                                       StrokeDashArray="3 2" Margin="-4"
                                       IsHitTestVisible="False"
                                       Visibility="Collapsed"/>
                        </Grid>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsSelected" Value="True">
                                <Setter TargetName="Marker" Property="Visibility" Value="Visible"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

Здесь важно: `ContentPresenter` внутри `ControlTemplate` продолжает подбирать
`DataTemplate` по типу — механика из 3.1 никуда не делась.

Обсудить цену решения: за `SelectedItem` бесплатно мы заплатили переопределением
`ControlTemplate`. Альтернатива — оставить `ItemsControl`, завести
`bool IsSelected` во `ShapeViewModel` и выделять через `DataTrigger`.
Тогда синхронизацию «выделен ровно один» пишем сами. Оба варианта живые,
выбор зависит от того, нужно ли множественное выделение.

---

## Часть 4. Перетаскивание мышью (20 мин)

### 4.1 Правило движения — во view model

```csharp
public void MoveShape(ShapeViewModel shape, double dx, double dy)
{
    shape.X = Math.Clamp(shape.X + dx, 0, CanvasWidth  - shape.Width);
    shape.Y = Math.Clamp(shape.Y + dy, 0, CanvasHeight - shape.Height);
}
```

Ограничение «не выпускать фигуру за холст» — это правило, а правила живут
во view model. Во view остаётся только жест.

### 4.2 Жест — `Thumb`

`Thumb` — готовый контрол, который сам захватывает мышь, сам отслеживает
её и выдаёт готовую дельту в `DragDelta`. Писать `MouseDown` / `MouseMove` /
`CaptureMouse` руками не нужно.

В `ControlTemplate` элемента вместо `ContentPresenter`:

```xml
<Thumb DragDelta="OnShapeDragDelta">
    <Thumb.Template>
        <ControlTemplate TargetType="Thumb">
            <ContentPresenter Content="{Binding}"/>
        </ControlTemplate>
    </Thumb.Template>
</Thumb>
```

Code-behind `ImageView.xaml.cs`:

```csharp
private void OnShapeDragDelta(object sender, DragDeltaEventArgs e)
{
    if (sender is Thumb { DataContext: ShapeViewModel shape }
        && DataContext is ImageViewModel vm)
    {
        vm.MoveShape(shape, e.HorizontalChange, e.VerticalChange);
    }
}
```

### 4.3 Разговор про «а это вообще MVVM?»

Да. Пять строк code-behind не содержат ни одного правила: они переводят событие
UI в вызов метода view model. Логика (границы холста) осталась во view model
и тестируется без окна.

Чего делать было бы **нельзя** — вот этого:

```csharp
// ПЛОХО: правило переехало во view
shape.X = Math.Min(shape.X + e.HorizontalChange, 520 - shape.Width);
```

Если такой обработчик нужен на пяти экранах, его превращают в attached-поведение:

```csharp
public static class DragMove
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(DragMove),
            new PropertyMetadata(null, OnCommandChanged));
    // подписка на Thumb.DragDelta -> Command.Execute(new Vector(dx, dy))
}
```

Тот же код, но переиспользуемый и без code-behind во view. Показать как «куда
это растёт», писать целиком на уроке необязательно.

### 4.4 Клик по фигуре = выделение, и почему он ломается

Идея: выделение мы не программируем вообще — щелчок по `ListBoxItem` ставит
`IsSelected`, `ListBox` пишет объект в `SelectedItem`, привязка кладёт его
в `SelectedShape`, а `[NotifyCanExecuteChangedFor]` включает кнопки Delete
и Bring to front. Вся цепочка декларативная.

Но после добавления `Thumb` выделение перестанет работать, и это надо показать.
`Thumb` захватывает мышь и помечает `MouseLeftButtonDown` как `Handled`, поэтому
до `ListBoxItem` событие не доходит. Лечение — обработать туннельное событие,
которое идёт сверху вниз и до `Thumb` ещё не дошло:

```xml
<Style TargetType="ListBoxItem">
    <EventSetter Event="PreviewMouseLeftButtonDown" Handler="OnItemPreviewMouseDown"/>
```

```csharp
private void OnItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
    => ((ListBoxItem)sender).IsSelected = true;   // e.Handled НЕ трогаем
```

Отличный повод повторить маршрутизацию событий: `Preview*` — туннель от корня
к элементу, обычное событие — всплытие обратно. Тот, кто пометил событие
`Handled` при всплытии, отбирает его у всех родителей.

---

## Часть 5. Когда неявного шаблона мало (10 мин)

Неявный шаблон выбирается **по типу данных**. Если выбирать надо по состоянию,
есть два инструмента.

### 5.1 DataTrigger — если различие косметическое

```xml
<DataTemplate DataType="{x:Type shapes:RectangleViewModel}">
    <Rectangle x:Name="Body" Fill="#FFC98B" .../>
    <DataTemplate.Triggers>
        <DataTrigger Binding="{Binding Area}" Value="0">
            <Setter TargetName="Body" Property="Fill" Value="Transparent"/>
        </DataTrigger>
    </DataTemplate.Triggers>
</DataTemplate>
```

### 5.2 DataTemplateSelector — если различие структурное

```csharp
public class ShapeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Normal { get; set; }
    public DataTemplate? Tiny { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is ShapeViewModel { Area: < 500 } ? Tiny : Normal;
}
```

Подключается через `ItemTemplateSelector`. Важное следствие: как только вы
задали `ItemTemplate` или `ItemTemplateSelector` явно, **неявный подбор по типу
выключается** — селектор обязан вернуть шаблон сам.

Вывод, который стоит зафиксировать: селектор нужен для выбора по данным.
Для выбора по типу он не нужен никогда — там уже работает `DataType`.

---

## Часть 6. Подводные камни (5 мин)

* **`Canvas.Left` внутри `DataTemplate` не работает** — только через
  `ItemContainerStyle`. Ошибка номер один.
* **`x:Key` на `DataTemplate` выключает неявный подбор.** Ключ и `DataType`
  вместе — значит шаблон надо указывать вручную.
* **Явный `ItemTemplate` побеждает неявные шаблоны.** Смешивать нельзя.
* **По интерфейсам неявные шаблоны не ищутся** — только по классам и их базам.
* **Где объявлять шаблоны.** В `UserControl.Resources` — видны только внутри
  этого контрола; в `App.xaml` — во всём приложении. Для фигур логично
  положить рядом с холстом, а не в глобальные ресурсы.
* **`Width`/`Height` в шаблоне против `Stretch`.** Если забыть задать размер
  фигуре, `Canvas` даст ей размер по содержимому, и `Ellipse` схлопнется в точку.
* **Не хранить `Brush`, `Geometry`, `Point` из WPF во view model** — иначе
  тесты потянут за собой `PresentationCore`.
* **`Shapes.Move` вместо `Remove` + `Add`** при смене z-order: иначе контейнер
  пересоздаётся и выделение слетает.
* **`Thumb` съедает клик выделения** — `Handled = true` при всплытии.
  Лечится `PreviewMouseLeftButtonDown` на контейнере (см. 4.4).
* **Приведение типов в командах** (`if (SelectedShape is EllipseViewModel e)`) —
  сигнал, что в базовом классе не хватает виртуального члена.

---

## Домашнее задание (мостик к уроку 8)

1. **Третий тип фигуры** — `TriangleViewModel` (рисуется `Polygon`).
   Условие: добавить его, изменив ровно два места — фабрику в `AddShape`
   и новый `DataTemplate`. Если пришлось править что-то ещё — значит
   в базовом классе не хватает абстракции, разберём на уроке.
2. **Изменение размера**: маркер в правом нижнем углу выделенной фигуры,
   тянем — меняются `Width`/`Height`. Тот же `Thumb`, только метод
   `ResizeShape` во view model, с минимальным размером 10x10.
3. **Заготовка под DI**: сделать `ISceneStorage` с методами
   `Task SaveAsync(IReadOnlyList<ShapeViewModel> shapes, CancellationToken ct)`
   и `Task<IReadOnlyList<ShapeViewModel>> LoadAsync(CancellationToken ct)`,
   реализацию — через `System.Text.Json` в файл. Пока `new` в конструкторе
   `ImageViewModel`.

   Отдельно подумать: как сериализовать **полиморфную** коллекцию, чтобы при
   загрузке эллипс остался эллипсом? Ключевые слова — `[JsonDerivedType]`
   и type discriminator. На уроке 8 заменим `new` на внедрение зависимостей
   и заодно обсудим, почему сохранение сцены — работа сервиса, а не view model.
