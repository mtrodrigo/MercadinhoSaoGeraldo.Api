# Mercadinho API

API REST para **Mercadinho São Geraldo** com autenticação JWT, papéis (Cliente/Admin), CRUD de produtos com upload de imagens (Supabase Storage), gestão de clientes (Admin) e pedidos.  

---

## 👩‍💻 Tecnologias

- **.NET** (ASP.NET Core Web API)
- **Entity Framework Core** (Npgsql provider)
- **PostgreSQL** (Supabase)
- **Npgsql DataSource** (tuning para pgBouncer/Pooler do Supabase)
- **JWT (Bearer)** — `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Hash de senha**: BCrypt
- **Criptografia**: AES-GCM (CPF reversível)
- **Supabase Storage** (bucket `product-images`)
- **CORS**
- **Serilog** (logs estruturados)
- **DotNetEnv** (carrega `.env`)
- **AspNetCoreRateLimit**

---

## ⚙️ Requisitos

- **.NET SDK 8** (ou 7, se o projeto estiver com TargetFramework 7)
- Conta Supabase com:
  - Projeto Postgres ativo
  - **Service Role Key**
  - Bucket no Storage chamado **product-images** (público ou com regras que permitam leitura)
- Postman/Insomnia (para testes)

---

## 🔐 Variáveis de ambiente (`.env`)

Crie um arquivo `.env` na raiz do projeto:

```ini
# Supabase (DB + Storage)
SUPABASE_URL=https://SEU-PROJETO.supabase.co
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOi...
SUPABASE_DB_CONNECTION=Host=aws-1-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.SEU-PROJETO;Password=SUA-SENHA;SSL Mode=Require

# JWT
JWT_ISSUER=mercadinho-api
JWT_AUDIENCE=mercadinho-clients
JWT_KEY=uma-chave-aleatoria-bem-grande-para-jwt

# Criptografia AES-256 (Base64 de 32 bytes)
AES_KEY_BASE64=GERADO_EM_BASE64_DE_32_BYTES
```

---

## 🚀 Instalação & Execução

```bash
dotnet restore
dotnet build
dotnet run
```

Por padrão sobe em **http://localhost:5000**.

---

## ☁️ Deploy no Render

1. Crie (ou reutilize) um serviço **Web Service** no [Render](https://render.com) apontando para este repositório.
2. Se preferir a automação, utilize o arquivo [`render.yaml`](render.yaml) deste projeto para criar o serviço via **Blueprint** (`render blueprint deploy`). Ele já informa:
   - uso do `Dockerfile` e do `entrypoint.sh` existentes;
   - verificação de saúde em `GET /ping`;
   - variáveis de ambiente esperadas (configure-as como **Secrets** antes do deploy).
3. Configure os secrets listados abaixo com os mesmos valores utilizados no `.env` local:
   - `SUPABASE_URL`
   - `SUPABASE_SERVICE_ROLE_KEY`
   - `SUPABASE_DB_CONNECTION`
   - `JWT_ISSUER`
   - `JWT_AUDIENCE`
   - `JWT_KEY`
   - `AES_KEY_BASE64`
   - (`opcional`) `USE_HTTPS_REDIRECT`
4. No Render, a variável `PORT` é fornecida automaticamente; o container já publica a API em `0.0.0.0:$PORT` através do `entrypoint.sh`.
5. Após o deploy, utilize o endpoint `/ping` para confirmar que a aplicação está saudável (o Render também usará essa rota no health check).

---

## 🧱 Banco de Dados (tabelas)

Tabelas principais (mapeadas no `AppDbContext`):

- `app_users`  
  `id (uuid)`, `email`, `password_hash`, `role` (`Cliente`|`Admin`), `cpf_enc`, `nome`, `created_at`, `updated_at`
- `app_user_details`  
  `user_id (uuid, PK=FK)`, `telefone`, `cep`, `logradouro`, `numero`, `complemento`, `bairro`, `cidade`, `estado`, `created_at`, `updated_at`
- `products`  
  `id (uuid)`, `nome`, `descricao`, `preco (numeric)`, `estoque`, `image_url`, `created_at`, `updated_at`
- `orders`  
  `id (uuid)`, `user_id`, `total (numeric)`, `status`, `created_at`
- `order_items`  
  `id (uuid)`, `order_id`, `product_id`, `quantidade`, `preco_unit (numeric)`

> Você pode criar via **EF Migrations** (`dotnet ef migrations add InitialCreate && dotnet ef database update`) ou aplicar um SQL inicial equivalente no Supabase.

---

## 🔒 Segurança

- **Senha**: BCrypt (hash irreversível)
- **CPF**: AES-GCM (reversível), chave em `AES_KEY_BASE64`
- **JWT**: Bearer, emissor/audience configuráveis, expiração curta para access token
- **CORS**: libere somente seus front-ends
- **Headers de segurança** aplicados no pipeline
- **Supabase Storage**: as imagens são enviadas para o bucket `product-images`

---

## 📚 Endpoints (visão geral)

### Autenticação
- `POST /api/auth/register`  
- `POST /api/auth/login`  

### Usuário (Cliente autenticado)
- `GET /api/users/me`
- `PUT /api/users/me`  
- `PUT /api/users/me/password` 

### Administração de clientes (Admin)
- `GET /api/admin/clientes?search=...`
- `GET /api/admin/clientes/{id}`
- `PATCH /api/admin/clientes/{id}/role`  
- `DELETE /api/admin/clientes/{id}`

### Produtos
- `POST /api/products` *(Admin)*
- `GET /api/products?search=...&pagina=1&tamanho=20`
- `GET /api/products/{id}`
- `PUT /api/products/{id}` *(Admin)*
- `DELETE /api/products/{id}` *(Admin)*
- `POST /api/products/{id}/image` *(Admin, upload imagem)*  

### Pedidos
- `POST /api/orders` *(Cliente)*
- `GET /api/orders/my` *(Cliente)*
- `GET /api/orders/{id}` *(Cliente/Admin)*
- `GET /api/orders` *(Admin)*
- `PATCH /api/orders/{id}/status` *(Admin)*  

---

## 📧 Contato

**Rodrigo** — rodrigour@gmail.com
