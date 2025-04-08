# Modelo ETL para Análise de Estrutura de Banco de Dados

Este projeto foi desenvolvido como um modelo **ETL (Extract, Transform, Load)** para a análise e extração da estrutura de um banco de dados SQL Server. Ele é projetado para coletar informações sobre tabelas, colunas e chaves estrangeiras, transformá-las em um formato JSON estruturado e carregar esse resultado em um arquivo para ser utilizado em análises posteriores.

Este modelo pode ser utilizado como parte de um Trabalho de Conclusão de Curso (TCC) ou em qualquer situação onde seja necessário extrair e documentar a estrutura de um banco de dados.

## Objetivo

O objetivo principal deste projeto é fornecer uma solução simples e eficiente para a **extração, transformação e carga (ETL)** de dados sobre a estrutura de um banco de dados relacional, visando facilitar a documentação, auditoria e análise do esquema de banco de dados. 

## Como Funciona

Este projeto realiza três etapas principais:

1. **Extração (Extract)**: 
   - Conecta-se ao banco de dados SQL Server.
   - Obtém a lista de tabelas e as informações sobre colunas e chaves estrangeiras de cada tabela.
   
2. **Transformação (Transform)**:
   - Processa os dados extraídos, organizando as informações de forma estruturada.
   - Organiza as tabelas, colunas e chaves estrangeiras em um formato adequado para fácil visualização e análise.
   
3. **Carga (Load)**:
   - Gera um arquivo JSON contendo a estrutura do banco de dados.
   - O arquivo pode ser utilizado para gerar documentação, importar para outras ferramentas ou realizar análises de estrutura do banco de dados.

## Requisitos

- **.NET 6 ou superior** para compilar e executar o código.
- **SQL Server** ou outro banco de dados SQL Server compatível.
- **Acesso ao banco de dados**: Para configurar a string de conexão, é necessário ter acesso ao banco de dados, com as permissões necessárias para consultar as tabelas e colunas.

## Estrutura do Projeto

O código é estruturado para ser facilmente entendível, com um processo de ETL que realiza:

- **Conexão com o banco de dados** utilizando `SqlConnection`.
- **Extração de dados** sobre tabelas, colunas e chaves estrangeiras utilizando `SqlCommand` e `SqlDataReader`.
- **Transformação dos dados** em um formato estruturado e adequado, utilizando tipos como `ColumnInfo`, `ForeignKeyInfo` e `TableStructure`.
- **Geração de um arquivo JSON** para armazenar os dados processados, o qual pode ser utilizado posteriormente em análises ou documentações.

## Como Usar

1. **Clone o repositório** ou baixe o código-fonte.
   
2. **Configure a string de conexão**:
   - No código, edite as variáveis abaixo com as informações do seu banco de dados:
     ```csharp
     string server = "localhost";
     string database = "teste";
     string username = "root";
     string password = "xxxxxxxxx";
     ```
     Ajuste para que as informações de servidor, banco de dados, nome de usuário e senha correspondam ao seu ambiente de banco de dados.

3. **Compile e execute o projeto**:
   - Abra o terminal na pasta do projeto.
   - Execute os seguintes comandos para compilar e rodar o projeto:
     ```bash
     dotnet build
     dotnet run
     ```

4. **Resultado**:
   - O programa gerará um arquivo `db_structure.json` na pasta do projeto com a estrutura do banco de dados em formato JSON.

## Exemplo de Arquivo JSON Gerado

O arquivo JSON gerado tem a seguinte estrutura:

```json
{
  "Tabela1": {
    "Columns": [
      { "Name": "Coluna1", "Type": "INT" },
      { "Name": "Coluna2", "Type": "VARCHAR" }
    ],
    "ForeignKeys": [
      {
        "Column": "Coluna1",
        "Constraint": "FK_Tabela1_Coluna1_Tabela2_Coluna2",
        "ReferencingColumn": "Coluna1",
        "ReferencedTable": "Tabela2",
        "ReferencedColumn": "Coluna2"
      }
    ]
  }
}
```


## Logs de Execução
Durante o processo de ETL, o programa gera logs detalhados sobre cada etapa. As mensagens incluem ícones e informações sobre o andamento do processo, como tempo de execução e número de tabelas processadas.

## Exemplo de log:
🔵 [ETAPA 1/4] Iniciando análise da estrutura do banco de dados...
🔵 [ETAPA 2/4] Conectando ao banco de dados...
✅ Conexão estabelecida com sucesso. ⏱️ Tempo: 2.35s
🔵 [ETAPA 3/4] Obtendo lista de tabelas...
📋 Encontradas 10 tabelas. ⏱️ Tempo: 1.10s
🔵 [ETAPA 4/4] Processando 10 tabelas...
ℹ️ Processando tabela 1/10: Tabela1
📝 Obtendo colunas e tipos de dados...
🔗 Buscando chaves estrangeiras...
📋 Tabela Tabela1 processada. ⏱️ Tempo: 0.75s
📄 Arquivo 'db_structure.json' gerado com sucesso. ⏱️ Tempo: 1.50s
✅ Processo concluído com sucesso! ⏱️ Tempo total: 5.35s


## Tipos de Dados Suportados
O modelo é compatível com vários tipos de dados comuns em bancos de dados SQL Server, como:

INT

VARCHAR

DATE

DECIMAL

DATETIME

E outros tipos de dados padrão.

Contribuindo
Se você deseja contribuir para o projeto, siga os seguintes passos:

Faça um fork do repositório.

Crie uma branch para a sua feature (git checkout -b feature/nova-feature).

Faça as alterações necessárias.

Envie as alterações para o seu fork (git push origin feature/nova-feature).

Abra um pull request para o repositório principal.

Licença
Este projeto está licenciado sob a MIT License.

Considerações Finais
Este modelo ETL foi criado para ser uma base para análise e documentação de bancos de dados. Ele pode ser integrado a outras ferramentas de análise de dados ou adaptado para cenários específicos, como migrações ou auditorias.
