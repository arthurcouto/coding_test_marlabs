# 🏢 Elevator Challenge

Uma solução robusta, thread-safe e escalável para o controle de um sistema de múltiplos elevadores. 
Implementado em .NET 10 seguindo princípios de Clean Architecture, Clean Code e programação assíncrona.

## 🏗️ Estrutura do Projeto

O projeto é dividido rigidamente em 4 camadas distintas para garantir baixo acoplamento e alta testabilidade:

- **`Elevator.Domain`**: O coração do sistema. Contém apenas entidades puras (`Request`, `ElevatorConfiguration`), Enums de estado, interfaces estritas (`IElevator`, `IScheduler`) e eventos de domínio. **Zero** dependências externas, bibliotecas de log ou primitivas de threading de baixo nível.
- **`Elevator.Core`**: O motor do sistema. Contém a lógica de concorrência (`Task`, `lock`), os loops assíncronos de funcionamento dos elevadores (simulando tempo de viagem e operação de portas via `IClock`), e os algoritmos de agendamento reais (ex: `ClosestElevatorScheduler`, algoritmos baseados em disco rígido como SCAN e LOOK). Contém o orquestrador `ElevatorSystem` e o despachante de requisições `RequestDispatcher` que processa fila `ThreadSafeQueue/BlockingCollection`.
- **`Elevator.App`**: A camada de visualização em formato de Console Interativo (BackgroundService injetado do ASP.NET Core Hosting). Lida exclusivamente com I/O, captura de comandos contínua e injeção de dependência (`ServiceCollectionExtensions`).
- **`Elevator.Tests`**: Conjunto de testes automatizados com xUnit e Moq. Cobre desde o isolamento de cada `Scheduler` e máquina de estados (`Unit`) até a simulação pesada de sobrecarga com 100 requisições simultâneas via Threads/Tasks em um ambiente integrado (`Integration/ConcurrencyTests`).

## 🚀 Como Executar

Você pode executar o projeto de duas maneiras principais: via **Docker** (recomendado se não quiser instalar dependências) ou via **.NET SDK**.

### Via Docker (Recomendado)

Você só precisa ter o `docker` e `docker-compose` instalados:

1. **Subir a aplicação com configurações via Ambiente:**
   Na raiz do diretório `ElevatorChallenge/`, simplesmente execute:
   ```bash
   docker-compose run elevator-app
   ```
   *Nota: O console já abrirá interativamente no seu terminal.*

2. **Alterar as configurações (Andares, Frota, Timeouts):**
   Basta editar o arquivo `docker-compose.yml` e rodar o comando novamente.

### Via .NET Nativo

O SDK do .NET é obrigatório.

1. **Na raiz do diretório da Solução `ElevatorChallenge/`**, construa o projeto:
   ```bash
   dotnet build
   ```

2. **Para rodar os testes unitários e de integração:**
   ```bash
   dotnet test
   ```

3. **Para iniciar a aplicação interativa:**
   ```bash
   cd Elevator.App
   dotnet run
   ```

## 🎮 Comandos do Console

A interface interativa permite o input "em tempo real" via terminal sem bloquear a simulação em background.

- `req <origem> <destino>` - Chama um elevador padrão da origem para o destino (Ex: `req 1 5`)
- `req <origem> <destino> vip` - Adiciona a flag VIP para priorização de algoritmos de frete/expressos (Ex: `req 8 2 vip`)
- `status` - Mostra uma listagem momentânea de cada elevador (andar atual, capacidade e estado)
- `metrics` - Retorna um extrato instantâneo usando o `InMemoryMetricsCollector` (Ex: Tempo Médio de Espera, Utilização dos Elevadores)
- `q` ou `quit` - Encerra o BackgroundService graciosamente.

## 🛠 Escolhas de Design

- **Event-Driven Telemetry:** O `ElevatorSystem` e o `InMemoryMetricsCollector` conversam passivamente com a frota usando os eventos do domínio (`StateChanged`, `RequestCompleted`), o que tira a responsabilidade dos Elevadores de conhecerem as métricas de negócio do prédio.
- **Schedulers Substituíveis:** Pelo uso da interface `IScheduler`, a forma de resolver para onde o elevador vai (FIFO vs Closest First) é puramente injetada no DI.
- **Tempo Abstrato (`IClock`):** O `SystemClock` é injetado no lugar de `Task.Delay()` direto para que os testes do Core pudessem, caso necessário avançar no tempo, simular execuções determinísticas sem ficar segundos parados esperando portas abrirem ou fecharem.
- **Thread-Safety Isolado:** Os controles pesados e exclusões mútuas (`lock`) ficam isolados na alteração das propriedades estritas do `Elevator.cs`, enquanto a recepção das ordens é tratada por fila concorrente nativa de alta performance (`ConcurrentQueue<T>`, `BlockingCollection<T>`).
