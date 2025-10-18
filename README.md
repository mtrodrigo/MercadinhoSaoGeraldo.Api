# Mercadinho São Geraldo API

API RESTful para gerenciamento de usuários, produtos e pedidos do Mercadinho São Geraldo.

## Tecnologias

- **ASP.NET Core 8** para criação da API.
- **Entity Framework Core** com **Npgsql** para acesso ao PostgreSQL.
- **JWT Bearer** para autenticação e autorização.
- **Serilog** para observabilidade e logging estruturado.
- **Supabase Storage** para upload de imagens de produtos.
- **FluentValidation** (via `FluentValidation.AspNetCore`) para validação de DTOs.
- **DotNetEnv** para carregar variáveis de ambiente a partir de arquivos `.env`.

## Configuração de ambiente

A aplicação depende das seguintes variáveis de ambiente:

| Variável | Descrição |
| --- | --- |
| `SUPABASE_DB_CONNECTION` ou `ConnectionStrings__DefaultConnection` | Cadeia de conexão PostgreSQL. URLs `postgres://` também são aceitas. |
| `SUPABASE_URL` | URL do projeto Supabase (utilizado para o Storage). |
| `SUPABASE_SERVICE_ROLE_KEY` | Chave service role do Supabase para manipular o Storage. |
| `JWT_ISSUER` | Emissor a ser validado nos tokens JWT. |
| `JWT_AUDIENCE` | Audiência dos tokens JWT. |
| `JWT_KEY` | Segredo simétrico para assinar os tokens JWT (mínimo 16 bytes UTF-8). |
| `AES_KEY_BASE64` | Chave AES-256 em Base64 (32 bytes) para criptografia de CPFs. |
| `ALLOWED_ORIGINS` (opcional) | Lista separada por vírgulas de origens permitidas no CORS. Padrão: `*`. |
| `USE_HTTPS_REDIRECT` (opcional) | Define se redireciona HTTP→HTTPS. Padrão: `false`. |

Crie um arquivo `.env` na raiz do projeto ou defina as variáveis diretamente no ambiente.

```env
SUPABASE_DB_CONNECTION=Host=localhost;Port=5432;Database=mercadinho;Username=postgres;Password=postgres
SUPABASE_URL=https://xyzcompany.supabase.co
SUPABASE_SERVICE_ROLE_KEY=seu-service-role-key
JWT_ISSUER=https://mercadinho.local
JWT_AUDIENCE=mercadinho-clients
JWT_KEY=sua-chave-super-secreta
AES_KEY_BASE64=MzJieXRlc2RlY2hhdmU0cHJhQVBJIQ==
ALLOWED_ORIGINS=http://localhost:5173
USE_HTTPS_REDIRECT=false
```

## Execução local

1. Instale o .NET SDK 8.0.
2. Garanta que um servidor PostgreSQL esteja disponível e que a cadeia de conexão apontada nas variáveis esteja acessível.
3. Clone o repositório e restaure as dependências:

   ```bash
   dotnet restore
   ```

4. Compile e execute a API:

   ```bash
   dotnet build
   dotnet run
   ```

5. A API ficará disponível em `http://localhost:5000` (ou na porta definida em `ASPNETCORE_URLS`).

> Obs.: aplique as migrações/DDL necessárias no banco antes de iniciar a API.

## Execução com Docker

1. Crie um arquivo `.env` com as variáveis de ambiente.
2. Construa a imagem:

   ```bash
   docker build -t mercadinho-api .
   ```

3. Inicie o container expondo a porta desejada e carregando as variáveis:

   ```bash
   docker run --env-file .env -p 5000:5000 mercadinho-api
   ```

4. Utilize `http://localhost:5000` para acessar a API. O healthcheck está disponível em `/ping`.

## Endpoints principais

| Método | Rota | Autenticação | Descrição |
| --- | --- | --- | --- |
| `GET` | `/ping` | Pública | Verifica o status da API. |
| `POST` | `/api/auth/register` | Pública | Registra um novo usuário cliente. |
| `POST` | `/api/auth/login` | Pública | Autentica usuário e retorna tokens JWT. |
| `GET` | `/api/me` | JWT | Recupera dados do perfil autenticado. |
| `PUT` | `/api/me` | JWT | Atualiza nome e CPF do perfil autenticado. |
| `GET` | `/api/me/contact` | JWT | Obtém endereço e contato do usuário. |
| `PUT` | `/api/me/contact` | JWT | Cria/atualiza contato do usuário. |
| `GET` | `/api/products` | Pública | Lista produtos. |
| `GET` | `/api/products/{id}` | Pública | Obtém um produto por ID. |
| `POST` | `/api/products` | JWT (Admin) | Cria produto. |
| `PUT` | `/api/products/{id}` | JWT (Admin) | Atualiza produto. |
| `DELETE` | `/api/products/{id}` | JWT (Admin) | Remove produto. |
| `POST` | `/api/products/{id}/image` | JWT (Admin) | Faz upload de imagem do produto. |
| `POST` | `/api/orders` | JWT | Cria pedido com itens. |
| `GET` | `/api/orders/mine` | JWT | Lista pedidos do usuário autenticado. |
| `GET` | `/api/orders` | JWT (Admin) | Lista pedidos paginados. |
| `GET` | `/api/admin/users` | JWT (Admin) | Lista usuários paginados. |
| `GET` | `/api/admin/users/{id}/contact` | JWT (Admin) | Detalhes de contato de um usuário. |
| `PUT` | `/api/admin/users/{id}/contact` | JWT (Admin) | Cria/atualiza contato de um usuário. |

## Contato

Dúvidas ou suporte: [rodrigour@gmail.com](mailto:rodrigour@gmail.com)
