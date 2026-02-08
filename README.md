# Query Clipboard

Gerenciador de queries SQL para Windows. Acesse suas queries de qualquer lugar com um atalho global, busque, organize por categorias e copie com um clique.

## Funcionalidades

- **Atalho global configuravel** - Abra o popup de qualquer lugar do Windows (padrao: `Ctrl+Alt+Q`)
- **Copia rapida** - Clique na query, ela vai pro clipboard e o app fecha automaticamente
- **Editor com numeros de linha** - Janela redimensionavel com scroll horizontal e vertical para queries grandes
- **Busca instantanea** - Filtre por nome, conteudo, descricao ou categoria
- **Categorias com cores** - Organize queries por DBA, Dev, Reports, etc.
- **Estatisticas de uso** - Veja quantas vezes usou cada query e quando foi a ultima vez
- **Editor integrado** - Crie e edite queries direto no app
- **Import/Export** - Exporte e importe queries em JSON para backup ou compartilhamento
- **Dois modos de armazenamento**:
  - **JSON Local** - Arquivo salvo em `%APPDATA%\QueryClipboard\`
  - **SQL Server** - Banco de dados centralizado (tabela criada automaticamente)

## Requisitos

- .NET 8 SDK
- Windows 10/11
- Visual Studio 2022 ou VSCode com extensoes C#

## Instalacao

```bash
git clone <repo-url>
cd QueryClipboard
dotnet build
dotnet run --project QueryClipboard
```

Ou abra `QueryClipboard.sln` no Visual Studio e pressione F5.

## Configuracao

### Modo JSON (padrao)

Nenhuma configuracao necessaria. Queries sao salvas em:
```
%APPDATA%\QueryClipboard\queries.json
```

### Modo SQL Server

1. Abra as Configuracoes (icone de engrenagem)
2. Selecione **SQL Server**
3. Informe a connection string:
   ```
   Server=localhost;Database=QueryClipboard;Integrated Security=True;TrustServerCertificate=True;
   ```
4. Salve e reinicie o app

A tabela `Queries` e criada automaticamente na primeira execucao.

### Alterando o atalho

1. Abra as Configuracoes
2. Na secao "Atalho de Teclado", clique em **Alterar**
3. Pressione a combinacao desejada (ex: `Ctrl+Shift+Q`)
4. Clique em **Confirmar** e depois **Salvar**
5. Reinicie o app para aplicar

## Como usar

| Acao | Como |
|------|------|
| Abrir/fechar popup | Atalho global (padrao `Ctrl+Alt+Q`) |
| Fechar popup | `ESC` |
| Copiar query | Clique no card - copia e fecha automaticamente |
| Buscar | Digite na barra de busca (filtra em tempo real) |
| Filtrar por categoria | Clique nas pills de categoria |
| Nova query | Botao "+ Nova Query" no rodape |
| Editar | Icone de lapis no card (abre editor com scroll e numeros de linha) |
| Excluir | Icone X vermelho no card |
| Importar/Exportar | Botoes no rodape |

## Estrutura do projeto

```
QueryClipboard/
  App.xaml(.cs)                        # Estilos globais e paleta de cores
  MainWindow.xaml(.cs)                 # Popup principal
  Models/
    Models.cs                          # QueryItem, Category, AppSettings, StorageMode
  Services/
    IQueryRepository.cs                # Interface do repositorio
    JsonQueryRepository.cs             # Armazenamento em arquivo JSON
    SqlServerQueryRepository.cs        # Armazenamento em SQL Server
    HotkeyManager.cs                   # Registro de atalho global (Win32 API)
    SettingsService.cs                 # Leitura/gravacao de configuracoes
  Views/
    QueryEditorWindow.xaml(.cs)        # Dialog de criacao/edicao de query
    SettingsWindow.xaml(.cs)           # Dialog de configuracoes + CategoryEditorDialog
```

## Tecnologias

| Tecnologia | Uso |
|------------|-----|
| .NET 8 / WPF | Framework e interface |
| Newtonsoft.Json | Serializacao JSON |
| Microsoft.Data.SqlClient | Conexao SQL Server |
| Win32 RegisterHotKey | Atalho global do sistema |

## Criacao manual da tabela SQL

Se preferir criar a tabela manualmente:

```sql
CREATE TABLE Queries (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    SqlQuery NVARCHAR(MAX) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    LastUsed DATETIME2 DEFAULT GETDATE(),
    UsageCount INT DEFAULT 0
);

CREATE INDEX IX_Queries_Category ON Queries(Category);
CREATE INDEX IX_Queries_LastUsed ON Queries(LastUsed DESC);
```

## Backup

- **JSON**: Copie `%APPDATA%\QueryClipboard\queries.json` ou use o botao Exportar
- **SQL Server**: Backup convencional do banco

## Licenca

Projeto pessoal - use a vontade.
