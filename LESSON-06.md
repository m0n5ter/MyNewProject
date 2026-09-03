# Урок 6. Асинхронность по-настоящему + генераторы кода

Ветка: `lesson-06-async-commands`. Точка «до» — коммит `e8d104f Async Load`.

```
git diff e8d104f..lesson-06-async-commands
```

## Часть 1. Почему `async void` — зло (демо)

В состоянии «до» было так:

```csharp
LoadCommand = new RelayCommand(Load, () => !IsLoading);
private async void Load() { ... }
```

Демо на живом коде: поставить галочку **Simulate error** и нажать **Load**.

* Со старым `async void` исключение уходило в `SynchronizationContext` →
  `Application.DispatcherUnhandledException` → падало всё приложение.
  `try/catch` вокруг вызова команды не помогал.
* Второй симптом: команда не знала, когда операция закончилась, поэтому
  `CanExecute` держался на ручном флаге `IsLoading`.

Правило: `async void` допустим только в обработчиках событий UI. Всё остальное — `async Task`.

## Часть 2. `AsyncRelayCommand`

`IAsyncRelayCommand` умеет сам:

* `IsRunning` — вместо ручного `IsLoading` / `IsLoggingIn` (оба свойства удалены);
* `CanExecute == false`, пока задача выполняется — команда не запустится дважды;
* `ExecutionTask` — если нужен доступ к самой задаче.

В XAML это выглядит как `{Binding LoadCommand.IsRunning}` и `{Binding LoginCommand.IsRunning}`.

Обсудить: `AsyncRelayCommandOptions.AllowConcurrentExecutions` — когда параллельные
запуски всё-таки нужны.

## Часть 3. Отмена

```csharp
[RelayCommand(IncludeCancelCommand = true)]
private async Task LoadAsync(CancellationToken token)
{
    await Task.Delay(5000, token);   // без token отмена не сработает
}
```

Генератор создаёт **две** команды: `LoadCommand` и `LoadCancelCommand`.
Кнопка Cancel биндится на вторую и сама включается только на время выполнения.

`OperationCanceledException` ловим отдельным `catch` — это не ошибка, а
результат действия пользователя.

## Часть 4. Ошибка — это состояние view model

`ErrorMessage` (`string?`) + вычисляемое `HasError`, одинаково в
`TodoListViewModel` и `LoginViewModel`. Никаких `MessageBox` из view model.

Попутно починили баг: `Items.Clear()` в начале загрузки — раньше повторный
Load добавлял копии.

## Часть 5. Генераторы

| Было (руками) | Стало |
|---|---|
| поле + свойство + `SetProperty` | `[ObservableProperty]` над полем |
| `OnPropertyChanged(nameof(IsLoginVisible))` в сеттере | `[NotifyPropertyChangedFor(nameof(IsLoginVisible))]` |
| `AddCommand.NotifyCanExecuteChanged()` в сеттере | `[NotifyCanExecuteChangedFor(nameof(AddCommand))]` |
| `LoginCommand = new RelayCommand(...)` в конструкторе | `[RelayCommand(CanExecute = nameof(CanLogin))]` над методом |
| побочный эффект в сеттере | `partial void OnUserNameChanged(string value)` |

Класс обязан быть `partial`. Обязательно показать сгенерированный код:
F12 по `NewTitle` или Solution Explorer → Dependencies → Analyzers →
`CommunityToolkit.Mvvm.SourceGenerators`.

### Подводные камни, которые стоит проговорить

* `[ObservableProperty]` всегда генерирует **публичный** сеттер. У `IsLoggedIn`
  в `MainViewModel` раньше был приватный — это плата за генератор.
  Иногда честнее оставить свойство руками.
* Имя команды получается отбрасыванием суффикса `Async`: `LoadAsync` → `LoadCommand`.
  Если назвать метод `LoadCommandAsync`, получится `LoadCommandCommand`.
* `CanExecute` для команды с параметром принимает тот же параметр:
  `private bool CanRemove(TodoItemViewModel? item)`.
* Вычисляемые свойства (`DoneCount`) генератор не отслеживает — `ClearDoneCommand.NotifyCanExecuteChanged()`
  остался ручным.

## Домашнее задание (мостик к уроку 7)

Вынести фейковый «сервер» из view model:

* `FakeTodoService` с `Task<IReadOnlyList<TodoDto>> GetAllAsync(CancellationToken ct)`;
* `FakeAuthService` с `Task<bool> SignInAsync(string user, string password, CancellationToken ct)`.

Пока просто `new` в конструкторе view model. На уроке 7 заменим на DI.
