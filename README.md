# Sistema de Ferias RH

Sistema para cadastro e acompanhamento de ferias de funcionarios, com automacao de bloqueio/reativacao de contas no Active Directory e desativacao de resposta automatica de e-mail no Microsoft Exchange Online.

O projeto foi pensado para o fluxo de RH: o RH cadastra o periodo de ferias, o worker verifica diariamente/periodicamente as datas e executa as acoes tecnicas no AD e no Exchange.

## Principais recursos

- Cadastro, edicao, listagem e exclusao de periodos de ferias.
- Validacao de login do funcionario no Active Directory.
- Bloqueio automatico da conta AD no inicio das ferias.
- Reativacao automatica da conta AD no retorno das ferias.
- Reativacao manual pela API/front-end para casos excepcionais.
- Consulta e backup da configuracao de resposta automatica do Exchange antes de desativa-la.
- Desativacao automatica do AutoReply do Exchange Online no inicio das ferias.
- Registro de auditoria do backup de AutoReply em banco.
- Modo mock para desenvolvimento local sem AD e sem Exchange.

## Arquitetura

```text
SistemaFerias.Api
  API REST, Swagger e arquivos estaticos do front-end publicado.

SistemaFerias.Worker
  Servico em background que processa entrada e retorno de ferias.

SistemaFerias.Domain
  Entidades, enums e interfaces do dominio.

SistemaFerias.Infrastructure
  Banco de dados, migrations, Active Directory, Exchange Online e repositorios.

SistemaFerias.Frontend
  Front-end React/Vite.
```

## Fluxo de negocio

1. O RH cadastra um periodo de ferias com nome, login AD, data de inicio e data de retorno.
2. O registro nasce com status `Pendente`.
3. O worker roda periodicamente e procura ferias pendentes cuja data de inicio ja chegou.
4. Para cada funcionario que entra em ferias, o worker:
   - consulta a configuracao atual de AutoReply no Exchange;
   - salva um backup em `ExchangeAutoReplyBackups`;
   - desativa o AutoReply no Exchange;
   - bloqueia a conta no AD;
   - muda o status para `EmFerias`.
5. Quando a data de retorno chega, o worker:
   - reativa a conta no AD;
   - marca o backup do Exchange como encerrado;
   - muda o status para `Finalizado`.

Observacao: o sistema nao religa automaticamente o AutoReply no retorno, porque isso poderia reativar uma mensagem de ausencia depois que o funcionario voltou. O backup fica salvo para auditoria.

## Status de ferias

| Valor | Nome | Significado |
| --- | --- | --- |
| 1 | `Pendente` | Ferias cadastradas, ainda nao iniciadas |
| 2 | `EmFerias` | Conta bloqueada no AD e AutoReply desativado |
| 3 | `Finalizado` | Ferias encerradas e conta reativada |

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Worker Service
- Entity Framework Core
- Pomelo EntityFrameworkCore MySQL
- MySQL ou MariaDB
- Active Directory via `System.DirectoryServices.AccountManagement`
- Exchange Online via PowerShell e modulo `ExchangeOnlineManagement`
- React
- Vite

## Pre-requisitos

Para desenvolvimento local:

- .NET SDK 8 instalado.
- Node.js instalado, caso queira rodar o front-end Vite.
- MySQL ou MariaDB, caso queira executar API/Worker com banco real.

Para producao ou ambiente integrado:

- Servidor Windows com acesso ao dominio Active Directory.
- Conta de servico com permissao para localizar, bloquear e reativar usuarios no AD.
- PowerShell com modulo `ExchangeOnlineManagement`.
- Conta administrativa do Exchange Online com permissao para executar:
  - `Get-MailboxAutoReplyConfiguration`
  - `Set-MailboxAutoReplyConfiguration`
- Banco MySQL/MariaDB acessivel pela API e pelo Worker.

## Configuracao

Os arquivos reais `appsettings.json` ficam ignorados pelo Git para evitar vazamento de senhas.

Existem arquivos de exemplo:

- `SistemaFerias.Api/appsettings.example.json`
- `SistemaFerias.Worker/appsettings.example.json`

Crie os arquivos reais copiando os exemplos:

```powershell
Copy-Item SistemaFerias.Api\appsettings.example.json SistemaFerias.Api\appsettings.json
Copy-Item SistemaFerias.Worker\appsettings.example.json SistemaFerias.Worker\appsettings.json
```

Depois edite as configuracoes.

### Exemplo de configuracao

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=sistema_ferias;User=sistema_ferias;Password=troque-esta-senha;"
  },
  "ActiveDirectory": {
    "UseMock": true,
    "Domain": "empresa.local",
    "Container": "DC=empresa,DC=local",
    "AdminUser": "EMPRESA\\usuario-servico",
    "AdminPassword": "troque-esta-senha"
  },
  "ExchangeOnline": {
    "UseMock": true,
    "AdminUser": "usuario.exchange@empresa.com"
  }
}
```

### Modo mock

Use mock em maquina local sem AD/Exchange:

```json
"ActiveDirectory": {
  "UseMock": true
},
"ExchangeOnline": {
  "UseMock": true
}
```

No servidor real:

```json
"ActiveDirectory": {
  "UseMock": false
},
"ExchangeOnline": {
  "UseMock": false
}
```

## Banco de dados

O sistema usa Entity Framework Core com MySQL/MariaDB.

Entidades principais:

- `Ferias`
  - funcionario, login AD, data de inicio, data de retorno, status e auditoria de entrada/saida.
- `ExchangeAutoReplyBackup`
  - backup da configuracao de AutoReply antes da desativacao.

### Aplicar migrations

Depois de configurar a connection string:

```powershell
dotnet ef database update --project SistemaFerias.Infrastructure --startup-project SistemaFerias.Api
```

Se o comando `dotnet ef` nao estiver disponivel:

```powershell
dotnet tool install --global dotnet-ef
```

## Exchange Online

O sistema usa PowerShell para integrar com o Exchange Online.

Instale o modulo no servidor que executa o Worker:

```powershell
Install-Module ExchangeOnlineManagement
```

O worker executa comandos equivalentes a:

```powershell
Get-MailboxAutoReplyConfiguration -Identity usuario@empresa.com
Set-MailboxAutoReplyConfiguration -Identity usuario@empresa.com -AutoReplyState Disabled
```

O comando real e executado por script temporario `.ps1`, para reduzir problemas com aspas e mensagens HTML do AutoReply.

## Active Directory

O servico de AD usa:

- dominio;
- container/base DN;
- usuario administrativo;
- senha administrativa.

Ele localiza usuarios pelo `SamAccountName`, ou seja, o campo `LoginAd` precisa estar no formato esperado pelo AD. Caso sua empresa use e-mail completo como login, valide se isso corresponde ao identificador usado no dominio.

## Como compilar

O repositorio possui `global.json` fixando o SDK .NET 8.

Compile os projetos principais:

```powershell
dotnet restore SistemaFerias.Api\SistemaFerias.Api.csproj
dotnet restore SistemaFerias.Worker\SistemaFerias.Worker.csproj

dotnet build SistemaFerias.Api\SistemaFerias.Api.csproj --no-restore
dotnet build SistemaFerias.Worker\SistemaFerias.Worker.csproj --no-restore
```

Observacao: o arquivo `SistemaFerias.slnx` usa um formato de solucao mais novo. Com o SDK .NET 8, prefira compilar por projeto como mostrado acima.

## Como executar localmente

Antes de executar:

1. Crie os `appsettings.json`.
2. Use `UseMock: true` se estiver sem AD/Exchange.
3. Configure um banco real ou ajuste a connection string para o banco disponivel.
4. Aplique as migrations.

### API

```powershell
dotnet run --project SistemaFerias.Api\SistemaFerias.Api.csproj
```

Por padrao, a API sobe em:

```text
http://localhost:5190
https://localhost:7050
```

Swagger:

```text
http://localhost:5190/swagger
```

### Worker

Em outro terminal:

```powershell
dotnet run --project SistemaFerias.Worker\SistemaFerias.Worker.csproj
```

O worker verifica os registros a cada 1 minuto.

### Front-end React/Vite

```powershell
cd SistemaFerias.Frontend
npm install
npm run dev
```

Para gerar build de producao:

```powershell
npm run build
```

## Endpoints principais

Base:

```text
/api/ferias
```

| Metodo | Rota | Descricao |
| --- | --- | --- |
| `GET` | `/api/ferias` | Lista todos os registros |
| `GET` | `/api/ferias/{id}` | Busca registro por ID |
| `GET` | `/api/ferias/status/{status}` | Lista por status |
| `GET` | `/api/ferias/login/{login}` | Lista por login AD |
| `GET` | `/api/ferias/validar-login/{login}` | Verifica se usuario existe no AD |
| `POST` | `/api/ferias` | Cria novo periodo de ferias |
| `PUT` | `/api/ferias/{id}` | Atualiza periodo ainda nao iniciado |
| `POST` | `/api/ferias/{id}/reativar` | Reativa manualmente uma conta em ferias |
| `DELETE` | `/api/ferias/{id}` | Exclui registro que nao esta em andamento |

### Exemplo de cadastro

```json
{
  "nomeFuncionario": "Maria Silva",
  "loginAd": "maria.silva",
  "dataInicio": "2026-07-01T00:00:00",
  "dataRetorno": "2026-07-15T00:00:00"
}
```

## Regras importantes

- Nao e permitido cadastrar ferias com data de retorno menor ou igual a data de inicio.
- Nao e permitido cadastrar periodos conflitantes para o mesmo login AD.
- Nao e permitido editar registro com status `EmFerias`.
- Nao e permitido excluir registro com status `EmFerias`.
- A transicao para `EmFerias` depende de sucesso na desativacao do AutoReply e no bloqueio do AD.
- A transicao para `Finalizado` depende de sucesso na reativacao do AD.

## Operacao em producao

Recomendacoes:

- Rodar `SistemaFerias.Api` como servico/IIS/reverse proxy conforme padrao da infraestrutura.
- Rodar `SistemaFerias.Worker` como Windows Service ou processo supervisionado.
- Usar uma conta de servico dedicada para AD.
- Usar uma conta administrativa dedicada para Exchange Online.
- Guardar senhas em variaveis de ambiente, secret manager, cofre de senhas ou mecanismo equivalente.
- Monitorar logs do Worker, principalmente falhas de AD ou Exchange.

## Problemas comuns

### `AD Domain nao configurado`

O `appsettings.json` nao existe ou esta sem a secao `ActiveDirectory`.

### `ExchangeOnline:AdminUser nao configurado`

Configure:

```json
"ExchangeOnline": {
  "AdminUser": "usuario.exchange@empresa.com"
}
```

### `Import-Module ExchangeOnlineManagement` falhou

Instale o modulo PowerShell no servidor:

```powershell
Install-Module ExchangeOnlineManagement
```

### A maquina local nao tem AD

Use:

```json
"ActiveDirectory": {
  "UseMock": true
}
```

### A maquina local nao tem Exchange Online configurado

Use:

```json
"ExchangeOnline": {
  "UseMock": true
}
```

## Seguranca

- Nao commitar `appsettings.json` com senhas reais.
- Nao commitar `.env` com segredos.
- Usar menor privilegio possivel para contas de servico.
- Revisar permissao da conta Exchange antes de colocar em producao.
- Proteger logs caso mensagens internas/externas de AutoReply sejam registradas no banco.

## Estado atual validado

Validado neste ambiente:

```powershell
dotnet build SistemaFerias.Api\SistemaFerias.Api.csproj --no-restore
dotnet build SistemaFerias.Worker\SistemaFerias.Worker.csproj --no-restore
```

Resultado: compilacao com sucesso, sem erros e sem avisos.

Nao foi executado teste integrado com AD/Exchange reais nesta maquina, porque o ambiente local nao possui esses servicos configurados.
