# TodoMinimalApi

## Descrição

**TodoMinimalApi** é uma aplicação minimalista desenvolvida com **.NET 8** e **Entity Framework Core** para gerenciar tarefas (**Todos**). A aplicação suporta operações CRUD para tarefas, autenticação com JWT e está completamente dockerizada, incluindo integração com um banco de dados **PostgreSQL**.

---

## Funcionalidades

- **Autenticação JWT**: Somente usuários autenticados podem acessar os endpoints.
- **CRUD de Tarefas**: Permite criar, listar, atualizar e excluir tarefas.
- **Banco de Dados PostgreSQL**: Persistência de dados usando Entity Framework Core.
- **Usuário Administrador Padrão**: Criado automaticamente na primeira execução.
- **Swagger UI**: Documentação interativa para testar os endpoints.
- **Dockerização Completa**: Facilita a execução e o deploy.

---

## Pré-requisitos

- [Docker](https://www.docker.com/) instalado.
- [Docker Compose](https://docs.docker.com/compose/) configurado.
- Opcional: [Postman](https://www.postman.com/) ou outro cliente de API para testes.

---

## Como Executar

### 1. Clone o Repositório

```bash
git clone https://github.com/seuusuario/TodoMinimalApi.git
cd TodoMinimalApi
```

### 2. Suba os Contêineres

Execute o comando:

```bash
docker-compose up --build
```

Este comando:
- Constrói e sobe os contêineres da aplicação e do banco de dados.
- Aplica as migrations ao banco de dados.
- Cria um usuário administrador padrão:
  - **E-mail**: `admin@todo.com`
  - **Senha**: `senha123`

### 3. Acesse a Aplicação

- **Swagger UI**: [`http://localhost:8080/swagger`](http://localhost:8080/swagger)
- **API**: [`http://localhost:8080`](http://localhost:8080)

### 4. Banco de Dados (Opcional)

Você pode acessar o banco de dados PostgreSQL diretamente usando um cliente SQL:

- **Host**: `localhost`
- **Porta**: `5432`
- **Usuário**: `postgres`
- **Senha**: `yourpassword`
- **Banco de Dados**: `TodoDb`

Ou acessar via Docker:

```bash
docker exec -it todo_postgres psql -U postgres -d TodoDb
```

---

## Endpoints Principais

### **Autenticação**
- `POST /api/v1/auth/login`: Autenticar e obter o token JWT.

### **Tarefas (Todos)**
- `POST /api/v1/todos`: Criar uma nova tarefa.
- `GET /api/v1/todos`: Listar todas as tarefas do usuário autenticado.
- `GET /api/v1/todos/{id}`: Obter detalhes de uma tarefa específica.
- `PUT /api/v1/todos/{id}`: Atualizar uma tarefa existente.
- `DELETE /api/v1/todos/{id}`: Excluir uma tarefa existente.

---

## Detalhes Técnicos

### Banco de Dados

A aplicação usa **PostgreSQL** e é gerenciada via **Entity Framework Core**. As tabelas são criadas automaticamente através de migrations. Estrutura inicial:

- **Users**:
  - `Id`: Identificador único do usuário.
  - `Name`: Nome do usuário.
  - `Email`: E-mail do usuário.
  - `PasswordHash`: Senha armazenada de forma segura.

- **Todos**:
  - `Id`: Identificador único da tarefa.
  - `Title`: Título da tarefa.
  - `Description`: Descrição da tarefa.
  - `CreatedAt`: Data de criação.
  - `CompletedAt`: Data de conclusão (opcional).
  - `UserId`: Identificador do usuário que criou a tarefa.

### Usuário Administrador Padrão

Na primeira execução, um usuário administrador é criado automaticamente:

- **E-mail**: `admin@todo.com`
- **Senha**: `senha123`

Este usuário pode ser usado para autenticar e testar os endpoints imediatamente.

---

## Customizações

### Alterar o Usuário Administrador Padrão

Para personalizar o e-mail e a senha do administrador, edite o arquivo `Program.cs` nas variáveis:

```csharp
var adminEmail = "admin@todo.com";
var adminPassword = "senha123";
```

### Configuração do Banco de Dados

As credenciais e o nome do banco podem ser ajustados no arquivo `docker-compose.override.yml`:

```yaml
environment:
  POSTGRES_USER: postgres
  POSTGRES_PASSWORD: yourpassword
  POSTGRES_DB: TodoDb
```